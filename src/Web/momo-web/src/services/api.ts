import {
  ApiResponse,
  AuthResponse,
  PagedResult,
  User,
  UserLookup,
  Wallet,
  LedgerItem,
  PaymentTransaction,
  AppNotification
} from '../types'

// Default to relative proxy on port 3000 (Vite dev or Nginx) or direct gateway:5000
const getApiBase = () => {
  if (import.meta.env.VITE_API_URL) return import.meta.env.VITE_API_URL
  if (typeof window !== 'undefined' && window.location.port === '3000') return ''
  return 'http://localhost:5000'
}

const API_BASE = getApiBase()

class ApiClient {
  private token: string | null = null

  constructor() {
    this.token = localStorage.getItem('momo_access_token')
  }

  setToken(token: string | null) {
    this.token = token
    if (token) {
      localStorage.setItem('momo_access_token', token)
    } else {
      localStorage.removeItem('momo_access_token')
    }
  }

  getToken(): string | null {
    return this.token
  }

  private async request<T>(endpoint: string, options: RequestInit = {}): Promise<T> {
    const url = `${API_BASE}${endpoint}`
    const headers: Record<string, string> = {
      'Content-Type': 'application/json',
      Accept: 'application/json',
      ...(options.headers as Record<string, string>),
    }

    if (this.token) {
      headers['Authorization'] = `Bearer ${this.token}`
    }

    try {
      const response = await fetch(url, {
        ...options,
        headers,
      })

      if (response.status === 401) {
        this.setToken(null)
        if (typeof window !== 'undefined') {
          window.dispatchEvent(new CustomEvent('momo_auth_expired'))
        }
      }

      const data: ApiResponse<T> = await response.json()

      if (!response.ok || !data.success) {
        throw new Error(data.message || `Lỗi yêu cầu: ${response.status}`)
      }

      return data.data as T
    } catch (err: any) {
      console.error(`API Error on [${options.method || 'GET'}] ${endpoint}:`, err)
      throw err
    }
  }

  // Auth & User API
  async login(phoneNumber: string, password: string): Promise<AuthResponse> {
    const res = await this.request<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, password }),
    })
    this.setToken(res.accessToken)
    return res
  }

  async register(phoneNumber: string, fullName: string, password: string, email?: string): Promise<AuthResponse> {
    const res = await this.request<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ phoneNumber, fullName, password, email }),
    })
    this.setToken(res.accessToken)
    return res
  }

  async getCurrentUser(): Promise<User> {
    return this.request<User>('/api/users/me')
  }

  async lookupPhone(phone: string): Promise<UserLookup> {
    return this.request<UserLookup>(`/api/users/lookup?phone=${encodeURIComponent(phone)}`)
  }

  // Wallet API
  async getBalance(): Promise<Wallet> {
    return this.request<Wallet>('/api/wallet/balance')
  }

  async deposit(amount: number, paymentSource: string = 'Techcombank Linked Card', description?: string, idempotencyKey?: string): Promise<Wallet> {
    const key = idempotencyKey || crypto.randomUUID()
    return this.request<Wallet>('/api/wallet/deposit', {
      method: 'POST',
      headers: {
        'X-Idempotency-Key': key,
      },
      body: JSON.stringify({ amount, paymentSource, description }),
    })
  }

  async withdraw(amount: number, bankCode: string = 'VCB', bankAccountNumber: string = '9876543210', description?: string, idempotencyKey?: string): Promise<Wallet> {
    const key = idempotencyKey || crypto.randomUUID()
    return this.request<Wallet>('/api/wallet/withdraw', {
      method: 'POST',
      headers: {
        'X-Idempotency-Key': key,
      },
      body: JSON.stringify({ amount, bankCode, bankAccountNumber, description }),
    })
  }

  async getLedger(page: number = 1, pageSize: number = 20): Promise<PagedResult<LedgerItem>> {
    return this.request<PagedResult<LedgerItem>>(`/api/wallet/ledger?page=${page}&pageSize=${pageSize}`)
  }

  // Transaction API
  async transfer(params: {
    receiverPhone: string
    amount: number
    description: string
    idempotencyKey?: string
  }): Promise<PaymentTransaction> {
    const idempotencyKey = params.idempotencyKey || crypto.randomUUID()
    return this.request<PaymentTransaction>('/api/transactions/transfer', {
      method: 'POST',
      headers: {
        'X-Idempotency-Key': idempotencyKey,
      },
      body: JSON.stringify({
        receiverPhone: params.receiverPhone,
        amount: params.amount,
        description: params.description,
        idempotencyKey,
      }),
    })
  }

  async payment(params: {
    merchantId: string
    orderId: string
    amount: number
    description: string
    idempotencyKey?: string
  }): Promise<PaymentTransaction> {
    const idempotencyKey = params.idempotencyKey || crypto.randomUUID()
    return this.request<PaymentTransaction>('/api/transactions/payment', {
      method: 'POST',
      headers: {
        'X-Idempotency-Key': idempotencyKey,
      },
      body: JSON.stringify({
        merchantId: params.merchantId,
        orderId: params.orderId,
        amount: params.amount,
        description: params.description,
        idempotencyKey,
      }),
    })
  }

  async getHistory(page: number = 1, pageSize: number = 20, type?: number, status?: number): Promise<PagedResult<PaymentTransaction>> {
    const params = new URLSearchParams({ page: page.toString(), pageSize: pageSize.toString() })
    if (type !== undefined) params.append('type', type.toString())
    if (status !== undefined) params.append('status', status.toString())
    return this.request<PagedResult<PaymentTransaction>>(`/api/transactions/history?${params.toString()}`)
  }

  async getTransactionByCode(code: string): Promise<PaymentTransaction> {
    return this.request<PaymentTransaction>(`/api/transactions/code/${code}`)
  }

  // Notification API
  async getNotifications(page: number = 1, pageSize: number = 20): Promise<PagedResult<AppNotification>> {
    return this.request<PagedResult<AppNotification>>(`/api/notifications?page=${page}&pageSize=${pageSize}`)
  }

  async getUnreadCount(): Promise<number> {
    return this.request<number>('/api/notifications/unread-count')
  }

  async markAsRead(id: string): Promise<void> {
    return this.request<void>(`/api/notifications/${id}/read`, { method: 'PUT' })
  }

  async markAllAsRead(): Promise<void> {
    return this.request<void>('/api/notifications/read-all', { method: 'PUT' })
  }
}

export const api = new ApiClient()
