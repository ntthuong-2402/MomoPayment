import React, { useState, useEffect } from 'react'
import { X, CheckCircle2, AlertCircle, Loader2, KeyRound, Shield, ArrowRight, RefreshCw } from 'lucide-react'
import { api } from '../services/api'
import { UserLookup, PaymentTransaction } from '../types'

interface TransferModalProps {
  isOpen: boolean
  onClose: () => void
  onSuccess: () => void
  currentPhone: string
  currentBalance: number
}

export const TransferModal: React.FC<TransferModalProps> = ({
  isOpen,
  onClose,
  onSuccess,
  currentPhone,
  currentBalance,
}) => {
  const [receiverPhone, setReceiverPhone] = useState('')
  const [recipient, setRecipient] = useState<UserLookup | null>(null)
  const [lookingUp, setLookingUp] = useState(false)
  const [lookupError, setLookupError] = useState<string | null>(null)

  const [amount, setAmount] = useState<number>(100000)
  const [description, setDescription] = useState('Chuyển tiền MoMo')
  const [idempotencyKey, setIdempotencyKey] = useState<string>(crypto.randomUUID())

  const [submitting, setSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [receipt, setReceipt] = useState<PaymentTransaction | null>(null)

  // Reset when opened
  useEffect(() => {
    if (isOpen) {
      setReceiverPhone('')
      setRecipient(null)
      setLookupError(null)
      setError(null)
      setReceipt(null)
      setIdempotencyKey(crypto.randomUUID())
    }
  }, [isOpen])

  // Realtime debounce lookup recipient by phone
  useEffect(() => {
    const trimmed = receiverPhone.trim()
    if (trimmed.length < 9) {
      setRecipient(null)
      setLookupError(null)
      return
    }

    if (trimmed === currentPhone) {
      setLookupError('Không thể tự chuyển tiền cho số của chính mình')
      setRecipient(null)
      return
    }

    const timer = setTimeout(async () => {
      setLookingUp(true)
      setLookupError(null)
      try {
        const res = await api.lookupPhone(trimmed)
        setRecipient(res)
      } catch (err: any) {
        setRecipient(null)
        setLookupError(err.message || 'Số điện thoại chưa đăng ký tài khoản MoMo')
      } finally {
        setLookingUp(false)
      }
    }, 400)

    return () => clearTimeout(timer)
  }, [receiverPhone, currentPhone])

  if (!isOpen) return null

  const handleSelectQuickContact = (phone: string) => {
    setReceiverPhone(phone)
  }

  const handleTransfer = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!recipient) return

    if (amount <= 0) {
      setError('Số tiền chuyển phải lớn hơn 0 VND')
      return
    }

    if (amount > currentBalance) {
      setError(`Số dư hiện tại (${currentBalance.toLocaleString()} đ) không đủ để chuyển`)
      return
    }

    setSubmitting(true)
    setError(null)

    try {
      const result = await api.transfer({
        receiverPhone: recipient.phoneNumber,
        amount,
        description: description.trim() || 'Chuyển tiền MoMo',
        idempotencyKey,
      })

      setReceipt(result)
      onSuccess()
    } catch (err: any) {
      setError(err.message || 'Lỗi xử lý giao dịch chuyển tiền')
      setIdempotencyKey(crypto.randomUUID())
    } finally {
      setSubmitting(false)
    }
  }

  const formatVND = (v: number) => new Intl.NumberFormat('vi-VN').format(v)

  const quickAmounts = [50000, 100000, 200000, 500000, 1000000]

  return (
    <div className="modal-overlay">
      <div className="modal-content">
        {/* Header */}
        <div style={{
          padding: '18px 24px',
          borderBottom: '1px solid var(--border-subtle)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          background: 'rgba(255, 255, 255, 0.02)',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div style={{
              width: '10px',
              height: '10px',
              borderRadius: 'var(--radius-full)',
              background: 'var(--momo-pink)',
              boxShadow: '0 0 10px var(--momo-pink)',
            }} />
            <h3 style={{ fontSize: '1.15rem', fontWeight: 800 }}>Chuyển tiền MoMo tức thì</h3>
          </div>
          <button
            onClick={onClose}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--text-muted)',
              cursor: 'pointer',
              display: 'flex',
            }}
          >
            <X size={20} />
          </button>
        </div>

        {/* Modal Body */}
        <div style={{ padding: '24px' }}>
          {receipt ? (
            /* Success Receipt Screen */
            <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', textAlign: 'center', gap: '16px' }}>
              <div style={{
                width: '64px',
                height: '64px',
                borderRadius: 'var(--radius-full)',
                background: 'var(--success-bg)',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'var(--success)',
                border: '1px solid rgba(16, 185, 129, 0.4)',
              }}>
                <CheckCircle2 size={36} />
              </div>

              <div>
                <h4 style={{ fontSize: '1.25rem', fontWeight: 800, color: 'var(--text-primary)', marginBottom: '4px' }}>
                  Chuyển tiền thành công!
                </h4>
                <div style={{ fontSize: '1.8rem', fontWeight: 900, color: 'var(--momo-pink)' }}>
                  -{formatVND(receipt.amount)} đ
                </div>
                <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '4px' }}>
                  Miễn phí giao dịch MoMo
                </div>
              </div>

              <div style={{
                width: '100%',
                background: 'rgba(255, 255, 255, 0.03)',
                border: '1px solid var(--border-subtle)',
                borderRadius: 'var(--radius-md)',
                padding: '16px',
                fontSize: '0.86rem',
                display: 'flex',
                flexDirection: 'column',
                gap: '10px',
                textAlign: 'left',
              }}>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: 'var(--text-muted)' }}>Mã giao dịch:</span>
                  <span style={{ fontWeight: 700, fontFamily: 'monospace' }}>{receipt.transactionCode}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: 'var(--text-muted)' }}>Người nhận:</span>
                  <span style={{ fontWeight: 700 }}>{recipient?.fullName}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: 'var(--text-muted)' }}>Số điện thoại:</span>
                  <span>{receipt.receiverPhone}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: 'var(--text-muted)' }}>Lời nhắn:</span>
                  <span>{receipt.description}</span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between' }}>
                  <span style={{ color: 'var(--text-muted)' }}>Thời gian:</span>
                  <span>{new Date(receipt.completedAt || receipt.createdAt).toLocaleString('vi-VN')}</span>
                </div>
              </div>

              <button className="btn-primary" style={{ width: '100%' }} onClick={onClose}>
                Hoàn tất
              </button>
            </div>
          ) : (
            /* Input Form */
            <form onSubmit={handleTransfer} style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
              {error && (
                <div style={{
                  background: 'var(--danger-bg)',
                  border: '1px solid rgba(239, 68, 68, 0.3)',
                  color: 'var(--danger)',
                  padding: '12px 14px',
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

              {/* Recipient Phone */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Số điện thoại người nhận
                </label>
                <div style={{ position: 'relative' }}>
                  <input
                    type="tel"
                    className="form-input"
                    placeholder="Nhập SĐT người nhận (VD: 0907654321)"
                    value={receiverPhone}
                    onChange={(e) => setReceiverPhone(e.target.value)}
                    required
                    style={{ paddingRight: '40px' }}
                  />
                  {lookingUp && (
                    <div style={{ position: 'absolute', right: '12px', top: '12px', color: 'var(--momo-pink)' }}>
                      <Loader2 size={18} className="animate-spin" />
                    </div>
                  )}
                </div>

                {/* Quick suggestions */}
                <div style={{ display: 'flex', gap: '6px', marginTop: '8px', flexWrap: 'wrap' }}>
                  <span style={{ fontSize: '0.74rem', color: 'var(--text-muted)', alignSelf: 'center' }}>Gợi ý:</span>
                  {['0907654321', '0912345678', '0901234567']
                    .filter((p) => p !== currentPhone)
                    .map((p) => (
                      <button
                        key={p}
                        type="button"
                        onClick={() => handleSelectQuickContact(p)}
                        style={{
                          background: 'rgba(255, 255, 255, 0.05)',
                          border: '1px solid var(--border-subtle)',
                          borderRadius: 'var(--radius-sm)',
                          padding: '2px 8px',
                          fontSize: '0.74rem',
                          color: 'var(--text-secondary)',
                          cursor: 'pointer',
                        }}
                      >
                        {p === '0907654321' ? 'Bob' : p === '0912345678' ? 'Charlie' : 'Alice'} ({p})
                      </button>
                    ))}
                </div>

                {/* Lookup Result Indicator */}
                {recipient && (
                  <div style={{
                    marginTop: '10px',
                    padding: '10px 14px',
                    background: 'rgba(16, 185, 129, 0.1)',
                    border: '1px solid rgba(16, 185, 129, 0.3)',
                    borderRadius: 'var(--radius-md)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'space-between',
                  }}>
                    <div>
                      <div style={{ fontSize: '0.9rem', fontWeight: 800, color: 'var(--text-primary)' }}>
                        {recipient.fullName}
                      </div>
                      <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>
                        SĐT: {recipient.maskedPhoneNumber}
                      </div>
                    </div>
                    <span className="badge badge-success">
                      <CheckCircle2 size={12} /> Đã kích hoạt ví
                    </span>
                  </div>
                )}

                {lookupError && (
                  <div style={{ fontSize: '0.8rem', color: 'var(--danger)', marginTop: '6px' }}>
                    {lookupError}
                  </div>
                )}
              </div>

              {/* Amount */}
              <div>
                <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: '8px' }}>
                  <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)' }}>
                    Số tiền chuyển (VND)
                  </label>
                  <span style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                    Khả dụng: {formatVND(currentBalance)} đ
                  </span>
                </div>
                <input
                  type="number"
                  className="form-input"
                  min="1000"
                  max="50000000"
                  step="1000"
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

                {/* Quick amount chips */}
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
                        transition: 'var(--transition)',
                      }}
                    >
                      {formatVND(q)} đ
                    </button>
                  ))}
                </div>
              </div>

              {/* Description */}
              <div>
                <label style={{ fontSize: '0.85rem', fontWeight: 600, color: 'var(--text-secondary)', display: 'block', marginBottom: '8px' }}>
                  Lời nhắn
                </label>
                <input
                  type="text"
                  className="form-input"
                  placeholder="Nhập lời nhắn chuyển tiền"
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  maxLength={100}
                />
              </div>

              {/* Invariant Financial Protection badge */}
              <div style={{
                background: 'rgba(216, 45, 139, 0.06)',
                border: '1px solid rgba(216, 45, 139, 0.2)',
                borderRadius: 'var(--radius-md)',
                padding: '10px 14px',
                display: 'flex',
                flexDirection: 'column',
                gap: '4px',
                fontSize: '0.76rem',
                color: 'var(--text-muted)',
              }}>
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px', color: 'var(--momo-pink)' }}>
                  <Shield size={14} />
                  <span style={{ fontWeight: 700 }}>Kiểm soát toàn vẹn giao dịch tài chính</span>
                </div>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px' }}>
                    <KeyRound size={12} />
                    <span>Idempotency Key: <code style={{ color: 'var(--text-secondary)' }}>{idempotencyKey.slice(0, 16)}...</code></span>
                  </div>
                  <button
                    type="button"
                    onClick={() => setIdempotencyKey(crypto.randomUUID())}
                    style={{
                      background: 'none',
                      border: 'none',
                      color: 'var(--momo-pink)',
                      cursor: 'pointer',
                      fontSize: '0.74rem',
                      fontWeight: 600,
                      display: 'flex',
                      alignItems: 'center',
                      gap: '4px',
                    }}
                    title="Đổi mã khóa mới"
                  >
                    <RefreshCw size={11} /> Đổi key
                  </button>
                </div>
              </div>

              {/* Submit Button */}
              <button
                type="submit"
                className="btn-primary"
                disabled={submitting || !recipient || amount <= 0 || lookingUp}
                style={{ width: '100%', padding: '14px' }}
              >
                {submitting ? (
                  <>
                    <Loader2 size={18} className="animate-spin" />
                    Đang khóa phân tán & xử lý gRPC...
                  </>
                ) : (
                  <>
                    Chuyển {formatVND(amount)} đ <ArrowRight size={18} />
                  </>
                )}
              </button>
            </form>
          )}
        </div>
      </div>
    </div>
  )
}
