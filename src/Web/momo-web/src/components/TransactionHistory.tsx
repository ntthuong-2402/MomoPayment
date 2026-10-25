import React, { useState, useEffect } from 'react'
import { ArrowUpRight, ArrowDownLeft, RefreshCw, QrCode } from 'lucide-react'
import { api } from '../services/api'
import { PaymentTransaction, TransactionType, TransactionStatus } from '../types'

interface TransactionHistoryProps {
  currentUserId: string
  refreshTrigger: number
}

export const TransactionHistory: React.FC<TransactionHistoryProps> = ({ currentUserId, refreshTrigger }) => {
  const [transactions, setTransactions] = useState<PaymentTransaction[]>([])
  const [loading, setLoading] = useState(false)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [typeFilter, setTypeFilter] = useState<number | undefined>(undefined)

  const loadHistory = async () => {
    setLoading(true)
    try {
      const res = await api.getHistory(page, 15, typeFilter)
      setTransactions(res.items)
      setTotalPages(res.totalPages)
    } catch (err) {
      console.error('Failed to load transaction history', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadHistory()
  }, [page, typeFilter, refreshTrigger])

  const formatVND = (v: number) => new Intl.NumberFormat('vi-VN').format(v)

  const getStatusBadge = (status: TransactionStatus) => {
    switch (status) {
      case TransactionStatus.Success:
        return <span className="badge badge-success">Thành công</span>
      case TransactionStatus.Failed:
        return <span className="badge badge-danger">Thất bại</span>
      case TransactionStatus.Pending:
        return <span className="badge badge-warning">Đang xử lý</span>
      default:
        return <span className="badge badge-info">Khác</span>
    }
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Header & Filter Controls */}
      <div style={{
        display: 'flex',
        justifyContent: 'space-between',
        alignItems: 'center',
        flexWrap: 'wrap',
        gap: '12px',
      }}>
        <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap' }}>
          {[
            { label: 'Tất cả', value: undefined },
            { label: 'Chuyển tiền', value: TransactionType.Transfer },
            { label: 'Thanh toán', value: TransactionType.Payment },
          ].map((f) => {
            const isSelected = typeFilter === f.value
            return (
              <button
                key={f.label}
                onClick={() => {
                  setTypeFilter(f.value)
                  setPage(1)
                }}
                style={{
                  background: isSelected ? 'var(--momo-pink)' : 'rgba(255, 255, 255, 0.05)',
                  color: isSelected ? 'white' : 'var(--text-secondary)',
                  border: '1px solid var(--border-subtle)',
                  borderRadius: 'var(--radius-full)',
                  padding: '6px 14px',
                  fontSize: '0.82rem',
                  fontWeight: 600,
                  cursor: 'pointer',
                  transition: 'var(--transition)',
                }}
              >
                {f.label}
              </button>
            )
          })}
        </div>

        <button
          onClick={loadHistory}
          className="btn-secondary"
          style={{ padding: '6px 12px', fontSize: '0.8rem' }}
          title="Làm mới lịch sử"
        >
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
          Làm mới
        </button>
      </div>

      {/* Transactions List */}
      {transactions.length === 0 ? (
        <div className="glass-panel" style={{ padding: '48px 24px', textAlign: 'center', color: 'var(--text-muted)' }}>
          <div style={{ fontSize: '1rem', fontWeight: 600 }}>Chưa có giao dịch nào</div>
          <div style={{ fontSize: '0.8rem', marginTop: '6px' }}>
            Hãy thực hiện giao dịch chuyển tiền hoặc thanh toán để xem lịch sử
          </div>
        </div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {transactions.map((t) => {
            const isSender = t.senderUserId === currentUserId
            const isPayment = t.type === TransactionType.Payment

            return (
              <div
                key={t.id}
                className="glass-panel"
                style={{
                  padding: '16px 20px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  gap: '14px',
                  flexWrap: 'wrap',
                }}
              >
                <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
                  <div style={{
                    width: '42px',
                    height: '42px',
                    borderRadius: '12px',
                    background: isPayment
                      ? 'rgba(245, 158, 11, 0.15)'
                      : isSender
                      ? 'rgba(239, 68, 68, 0.15)'
                      : 'rgba(16, 185, 129, 0.15)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexShrink: 0,
                  }}>
                    {isPayment ? (
                      <QrCode size={20} color="var(--warning)" />
                    ) : isSender ? (
                      <ArrowUpRight size={20} color="var(--danger)" />
                    ) : (
                      <ArrowDownLeft size={20} color="var(--success)" />
                    )}
                  </div>

                  <div>
                    <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <span style={{ fontSize: '0.95rem', fontWeight: 700 }}>
                        {isPayment
                          ? 'Thanh toán đơn hàng'
                          : isSender
                          ? `Chuyển tiền đến ${t.receiverPhone}`
                          : `Nhận tiền từ ${t.senderPhone}`}
                      </span>
                      {getStatusBadge(t.status)}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-muted)', marginTop: '2px' }}>
                      {t.description || 'Giao dịch MoMo'} • Mã: <span style={{ fontFamily: 'monospace' }}>{t.transactionCode}</span>
                    </div>
                    <div style={{ fontSize: '0.74rem', color: 'var(--text-muted)' }}>
                      {new Date(t.createdAt).toLocaleString('vi-VN')}
                    </div>
                  </div>
                </div>

                <div style={{ textAlign: 'right' }}>
                  <div style={{
                    fontSize: '1.15rem',
                    fontWeight: 800,
                    color: isPayment || isSender ? 'var(--text-primary)' : 'var(--success)',
                  }}>
                    {isPayment || isSender ? '-' : '+'}
                    {formatVND(t.amount)} đ
                  </div>
                  {t.failureReason && (
                    <div style={{ fontSize: '0.72rem', color: 'var(--danger)', marginTop: '2px' }}>
                      {t.failureReason}
                    </div>
                  )}
                </div>
              </div>
            )
          })}
        </div>
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div style={{ display: 'flex', justifyContent: 'center', gap: '8px', marginTop: '12px' }}>
          <button
            className="btn-secondary"
            disabled={page <= 1}
            onClick={() => setPage((p) => Math.max(1, p - 1))}
            style={{ padding: '6px 14px', fontSize: '0.8rem' }}
          >
            Trang trước
          </button>
          <span style={{ alignSelf: 'center', fontSize: '0.84rem', color: 'var(--text-muted)' }}>
            Trang {page} / {totalPages}
          </span>
          <button
            className="btn-secondary"
            disabled={page >= totalPages}
            onClick={() => setPage((p) => p + 1)}
            style={{ padding: '6px 14px', fontSize: '0.8rem' }}
          >
            Trang sau
          </button>
        </div>
      )}
    </div>
  )
}
