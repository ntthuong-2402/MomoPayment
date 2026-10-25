import React, { useState } from 'react'
import { X, CheckCircle2, AlertCircle, Loader2, PlusCircle, CreditCard, Building } from 'lucide-react'
import { api } from '../services/api'

interface DepositModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
}

export const DepositModal: React.FC<DepositModalProps> = ({ isOpen, onClose, onSuccess }) => {
  const [amount, setAmount] = useState<number>(500000)
  const [source, setSource] = useState('Techcombank')
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

  const quickAmounts = [100000, 200000, 500000, 1000000, 2000000]
  const sources = [
    { name: 'Techcombank', sub: 'Thẻ liên kết ****1234', icon: Building },
    { name: 'Vietcombank', sub: 'Thẻ liên kết ****5678', icon: Building },
    { name: 'Napas Thẻ nội địa', sub: 'Thẻ ATM Napas các ngân hàng', icon: CreditCard },
  ]

  const handleDeposit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (amount <= 0) return

    setSubmitting(true)
    setError(null)

    try {
      await api.deposit(amount, source, `Nạp tiền từ ${source}`)
      setSuccess(true)
      onSuccess()
    } catch (err: any) {
      setError(err.message || 'Lỗi nạp tiền vào ví')
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
            <PlusCircle size={20} color="var(--momo-pink)" />
            <h3 style={{ fontSize: '1.15rem', fontWeight: 800 }}>Nạp tiền vào ví MoMo</h3>
          </div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        <div style={{ padding: '24px' }}>
          {success ? (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center', gap: '16px' }}>
              <CheckCircle2 size={48} color="var(--success)" />
              <h4 style={{ fontSize: '1.2rem', fontWeight: 800 }}>Nạp tiền thành công!</h4>
              <div style={{ fontSize: '1.8rem', fontWeight: 800, color: 'var(--momo-pink)' }}>
                +{formatVND(amount)} đ
              </div>
              <p style={{ fontSize: '0.86rem', color: 'var(--text-muted)' }}>
                Tiền đã được cộng vào số dư ví của bạn và ghi sổ cái Ledger.
              </p>
              <button className="btn-primary" style={{ width: '100%' }} onClick={onClose}>
                Đóng
              </button>
            </div>
          ) : (
            <form onSubmit={handleDeposit} style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
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

              {/* Source selection */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Nguồn tiền nạp
                </label>
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {sources.map((s) => {
                    const isSelected = source === s.name
                    const Icon = s.icon
                    return (
                      <div
                        key={s.name}
                        onClick={() => setSource(s.name)}
                        style={{
                          padding: '12px 14px',
                          borderRadius: 'var(--radius-md)',
                          border: isSelected ? '1px solid var(--momo-pink)' : '1px solid var(--border-subtle)',
                          background: isSelected ? 'rgba(216, 45, 139, 0.12)' : 'rgba(255, 255, 255, 0.03)',
                          display: 'flex',
                          alignItems: 'center',
                          gap: '12px',
                          cursor: 'pointer',
                          transition: 'var(--transition)',
                        }}
                      >
                        <Icon size={20} color={isSelected ? 'var(--momo-pink)' : 'var(--text-muted)'} />
                        <div>
                          <div style={{ fontSize: '0.9rem', fontWeight: 700 }}>{s.name}</div>
                          <div style={{ fontSize: '0.76rem', color: 'var(--text-muted)' }}>{s.sub}</div>
                        </div>
                      </div>
                    )
                  })}
                </div>
              </div>

              {/* Amount */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Số tiền nạp (VND)
                </label>
                <input
                  type="number"
                  className="form-input"
                  min="10000"
                  max="50000000"
                  step="10000"
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
                    Tối thiểu 10.000 đ • Tối đa 50.000.000 đ
                  </span>
                </div>

                <div style={{ display: 'flex', gap: '6px', marginTop: '8px', flexWrap: 'wrap' }}>
                  {quickAmounts.map((q) => (
                    <button
                      key={q}
                      type="button"
                      onClick={() => setAmount(q)}
                      style={{
                        background: amount === q ? 'var(--momo-pink)' : 'rgba(255, 255, 255, 0.05)',
                        color: amount === q ? 'white' : 'var(--text-secondary)',
                        border: '1px solid var(--border-subtle)',
                        borderRadius: 'var(--radius-full)',
                        padding: '4px 12px',
                        fontSize: '0.78rem',
                        fontWeight: 600,
                        cursor: 'pointer',
                      }}
                    >
                      {formatVND(q)} đ
                    </button>
                  ))}
                </div>
              </div>

              <button
                type="submit"
                className="btn-primary"
                disabled={submitting || amount <= 0}
                style={{ width: '100%', padding: '14px' }}
              >
                {submitting ? (
                  <>
                    <Loader2 size={18} className="animate-spin" />
                    Đang nạp tiền...
                  </>
                ) : (
                  `Nạp ${formatVND(amount)} đ`
                )}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
