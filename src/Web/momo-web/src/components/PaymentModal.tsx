import React, { useState } from 'react'
import { X, CheckCircle2, AlertCircle, Loader2, QrCode, ShoppingBag } from 'lucide-react'
import { api } from '../services/api'

interface PaymentModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
  currentBalance: number
}

export const PaymentModal: React.FC<PaymentModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  currentBalance,
}) => {
  const [merchant, setMerchant] = useState('Shopee')
  const [amount, setAmount] = useState<number>(150000)
  const [orderId, setOrderId] = useState(`ORD-${Math.floor(100000 + Math.random() * 900000)}`)
  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)

  React.useEffect(() => {
    if (isOpen) {
      setSuccess(false)
      setError(null)
      setSubmitting(false)
    }
  }, [isOpen])

  if (!isOpen) return null

  const merchants = [
    { id: 'Shopee', name: 'Shopee Việt Nam', defaultAmount: 250000 },
    { id: 'Highlands', name: 'Highlands Coffee', defaultAmount: 65000 },
    { id: 'CircleK', name: 'Circle K Cửa hàng tiện lợi', defaultAmount: 45000 },
    { id: 'EVN', name: 'Điện Lực EVN HCMC', defaultAmount: 420000 },
  ]

  const handlePayment = async (e: React.FormEvent) => {
    e.preventDefault()
    if (amount <= 0) return

    if (amount > currentBalance) {
      setError(`Số dư hiện tại (${currentBalance.toLocaleString()} đ) không đủ thanh toán`)
      return
    }

    setSubmitting(true)
    setError(null)

    try {
      await api.payment({
        merchantId: merchant,
        orderId,
        amount,
        description: `Thanh toán ${merchant} đơn ${orderId}`,
      })
      setSuccess(true)
      onSuccess()
    } catch (err: any) {
      setError(err.message || 'Lỗi thanh toán đơn hàng')
    } finally {
      setSubmitting(false)
    }
  }

  const formatVND = (v: number) => new Intl.NumberFormat('vi-VN').format(v)

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        <div style={{
          padding: '18px 24px',
          borderBottom: '1px solid var(--border-subtle)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <QrCode size={20} color="var(--warning)" />
            <h3 style={{ fontSize: '1.15rem', fontWeight: 800 }}>Thanh toán hóa đơn / Đối tác</h3>
          </div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        <div style={{ padding: '24px' }}>
          {success ? (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center', gap: '16px' }}>
              <CheckCircle2 size={48} color="var(--success)" />
              <h4 style={{ fontSize: '1.2rem', fontWeight: 800 }}>Thanh toán thành công!</h4>
              <div style={{ fontSize: '1.8rem', fontWeight: 800, color: 'var(--momo-pink)' }}>
                -{formatVND(amount)} đ
              </div>
              <p style={{ fontSize: '0.86rem', color: 'var(--text-muted)' }}>
                Đã thanh toán đơn hàng {orderId} cho {merchant}.
              </p>
              <button className="btn-primary" style={{ width: '100%' }} onClick={onClose}>
                Đóng
              </button>
            </div>
          ) : (
            <form onSubmit={handlePayment} style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
              {error && (
                <div style={{
                  background: 'var(--danger-bg)',
                  border: '1px solid rgba(239, 68, 68, 0.3)',
                  color: 'var(--danger)',
                  padding: '10px 14px',
                  borderRadius: 'var(--radius-md)',
                  fontSize: '0.86rem',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                }}>
                  <AlertCircle size={18} />
                  <span>{error}</span>
                </div>
              )}

              {/* Merchant Selection */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Đối tác thanh toán
                </label>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(2, 1fr)', gap: '8px' }}>
                  {merchants.map((m) => {
                    const isSelected = merchant === m.id
                    return (
                      <div
                        key={m.id}
                        onClick={() => {
                          setMerchant(m.id)
                          setAmount(m.defaultAmount)
                        }}
                        style={{
                          padding: '12px',
                          borderRadius: 'var(--radius-md)',
                          border: isSelected ? '1px solid var(--momo-pink)' : '1px solid var(--border-subtle)',
                          background: isSelected ? 'rgba(216, 45, 139, 0.12)' : 'rgba(255, 255, 255, 0.03)',
                          cursor: 'pointer',
                          display: 'flex',
                          alignItems: 'center',
                          gap: '10px',
                          transition: 'var(--transition)',
                        }}
                      >
                        <ShoppingBag size={18} color={isSelected ? 'var(--momo-pink)' : 'var(--text-muted)'} />
                        <div>
                          <div style={{ fontSize: '0.84rem', fontWeight: 700 }}>{m.name}</div>
                          <div style={{ fontSize: '0.72rem', color: 'var(--text-muted)' }}>Mã: {m.id}</div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>

              {/* Order ID */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Mã đơn hàng
                </label>
                <input
                  type="text"
                  className="form-input"
                  value={orderId}
                  onChange={(e) => setOrderId(e.target.value)}
                  required
                />
              </div>

              {/* Amount */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Số tiền thanh toán (VND)
                </label>
                <input
                  type="number"
                  className="form-input"
                  min="1000"
                  max={Math.max(1000, currentBalance)}
                  value={amount || ''}
                  onChange={(e) => setAmount(Number(e.target.value))}
                  required
                  style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--momo-pink)' }}
                />

                <div style={{ display: 'flex', justifyContent: 'space-between', marginTop: '6px', fontSize: '0.82rem' }}>
                  <span style={{ color: 'var(--momo-pink-light)', fontWeight: 700 }}>
                    {formatVND(amount || 0)} ₫
                  </span>
                  <span style={{ color: 'var(--text-muted)' }}>
                    Khả dụng: {formatVND(currentBalance)} đ
                  </span>
                </div>
              </div>

              <button
                type="submit"
                className="btn-primary"
                disabled={submitting || amount <= 0 || amount > currentBalance}
                style={{ width: '100%', padding: '14px' }}
              >
                {submitting ? (
                  <>
                    <Loader2 size={18} className="animate-spin" />
                    Đang xử lý thanh toán...
                  </>
                ) : (
                  `Thanh toán ${formatVND(amount)} đ`
                )}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
