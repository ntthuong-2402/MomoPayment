import React, { useState } from 'react'
import { X, CheckCircle2, AlertCircle, Loader2, Landmark } from 'lucide-react'
import { api } from '../services/api'

interface WithdrawModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
  currentBalance: number
}

export const WithdrawModal: React.FC<WithdrawModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  currentBalance,
}) => {
  const [amount, setAmount] = useState<number>(200000)
  const [bankCode, setBankCode] = useState('VCB')
  const [accountNumber, setAccountNumber] = useState('0123456789')
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

  const banks = [
    { code: 'VCB', name: 'Vietcombank - NHTMCP Ngoại Thương' },
    { code: 'TCB', name: 'Techcombank - NHTMCP Kỹ Thương' },
    { code: 'MB', name: 'MBBank - Ngân Hàng Quân Đội' },
    { code: 'ACB', name: 'ACB - NHTMCP Á Châu' },
  ]

  const handleWithdraw = async (e: React.FormEvent) => {
    e.preventDefault()
    if (amount <= 0) return

    if (amount > currentBalance) {
      setError(`Số dư hiện tại (${currentBalance.toLocaleString()} đ) không đủ để rút`)
      return
    }

    setSubmitting(true)
    setError(null)

    try {
      await api.withdraw(amount, bankCode, accountNumber, `Rút tiền về ${bankCode} - STK ${accountNumber}`)
      setSuccess(true)
      onSuccess()
    } catch (err: any) {
      setError(err.message || 'Lỗi rút tiền về ngân hàng')
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
            <Landmark size={20} color="var(--info)" />
            <h3 style={{ fontSize: '1.15rem', fontWeight: 800 }}>Rút tiền về tài khoản ngân hàng</h3>
          </div>
          <button onClick={onClose} style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}>
            <X size={20} />
          </button>
        </div>

        <div style={{ padding: '24px' }}>
          {success ? (
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center', gap: '16px' }}>
              <CheckCircle2 size={48} color="var(--success)" />
              <h4 style={{ fontSize: '1.2rem', fontWeight: 800 }}>Rút tiền thành công!</h4>
              <div style={{ fontSize: '1.8rem', fontWeight: 800, color: 'var(--momo-pink)' }}>
                -{formatVND(amount)} đ
              </div>
              <p style={{ fontSize: '0.86rem', color: 'var(--text-muted)' }}>
                Tiền đã được trừ từ ví MoMo và chuyển đến tài khoản ngân hàng {bankCode} ({accountNumber}).
              </p>
              <button className="btn-primary" style={{ width: '100%' }} onClick={onClose}>
                Đóng
              </button>
            </div>
          ) : (
            <form onSubmit={handleWithdraw} style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
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

              {/* Bank Selection */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Ngân hàng thụ hưởng
                </label>
                <select
                  className="form-input"
                  value={bankCode}
                  onChange={(e) => setBankCode(e.target.value)}
                  style={{ background: 'var(--bg-input)' }}
                >
                  {banks.map((b) => (
                    <option key={b.code} value={b.code} style={{ background: '#111827' }}>
                      {b.name}
                    </option>
                  ))}
                </select>
              </div>

              {/* Account Number */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Số tài khoản ngân hàng
                </label>
                <input
                  type="text"
                  className="form-input"
                  value={accountNumber}
                  onChange={(e) => setAccountNumber(e.target.value)}
                  placeholder="Nhập STK ngân hàng"
                  required
                />
              </div>

              {/* Amount */}
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                  <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)' }}>
                    Số tiền rút (VND)
                  </label>
                  <button
                    type="button"
                    onClick={() => setAmount(currentBalance)}
                    style={{ background: 'none', border: 'none', color: 'var(--momo-pink)', fontSize: '0.8rem', fontWeight: 700, cursor: 'pointer' }}
                  >
                    Rút tất cả ({formatVND(currentBalance)} đ)
                  </button>
                </div>
                <input
                  type="number"
                  className="form-input"
                  min="10000"
                  max={Math.max(10000, currentBalance)}
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
                    Đang rút tiền...
                  </>
                ) : (
                  `Rút ${formatVND(amount)} đ`
                )}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
