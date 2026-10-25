export interface ApiResponse<T = any> {
  success: boolean
  message?: string
  data?: T
  errorCode?: string
  errors?: string[]
}

export interface PagedResult<T> {
  items: T[]
  totalCount: number
  pageIndex: number
  pageSize: number
  totalPages: number
  hasPreviousPage: boolean
  hasNextPage: boolean
}

export interface User {
  id: string
  phoneNumber: string
  fullName: string
  email?: string
  role: string
  createdAt: string
}

export interface AuthResponse {
  accessToken: string
  expiresIn: number
  user: User
}

export interface UserLookup {
  id: string
  phoneNumber: string
  maskedPhoneNumber: string
  fullName: string
}

export interface Wallet {
  id: string
  userId: string
  phoneNumber: string
  balance: number
  currency: string
  status: number
}

export interface LedgerItem {
  id: string
  previousBalance: number
  newBalance: number
  amount: number
  type: 'Credit' | 'Debit' | string
  transactionCode: string
  description?: string
  createdAt: string
}

export enum TransactionType {
  Transfer = 1,
  Payment = 2,
  Deposit = 3,
  Withdraw = 4,
}

export enum TransactionStatus {
  Pending = 1,
  Success = 2,
  Failed = 3,
  Cancelled = 4,
}

export interface PaymentTransaction {
  id: string
  transactionCode: string
  senderUserId?: string
  senderPhone?: string
  receiverUserId?: string
  receiverPhone?: string
  amount: number
  fee: number
  netAmount: number
  type: TransactionType
  status: TransactionStatus
  failureReason?: string
  description?: string
  createdAt: string
  completedAt?: string
}

export enum NotificationType {
  MoneyReceived = 1,
  MoneySent = 2,
  DepositSuccess = 3,
  WithdrawSuccess = 4,
  PaymentSuccess = 5,
  SystemAlert = 6,
}

export interface AppNotification {
  id: string
  userId: string
  title: string
  content: string
  type: NotificationType
  referenceTransactionCode?: string
  amount?: number
  isRead: boolean
  createdAt: string
  readAt?: string
}
