import React, { useState, useEffect } from 'react'
import { RefreshCw, ShieldCheck, ArrowDownLeft, ArrowUpRight, Copy, Check } from 'lucide-react'
import { api } from '../services/api'
import { LedgerItem } from '../types'

interface LedgerViewProps {
  refreshTrigger: number
}

export const LedgerView: React.FC<LedgerViewProps> = ({ refreshTrigger }) => {
  const [ledgers, setLedgers] = useState<LedgerItem[]>([])
  const [loading, setLoading] = useState(false)
  const [page, setPage] = useState(1)
  const [totalPages, setTotalPages] = useState(1)
  const [filterType, setFilterType] = useState<'all' | 'credit' | 'debit'>('all')
  const [copiedCode, setCopiedCode] = useState<string | null>(null)

  const loadLedger = async () => {
    setLoading(true)
    try {
      const res = await api.getLedger(page, 20)
      setLedgers(res.items)
      setTotalPages(res.totalPages)
    } catch (err) {
      console.error('Failed to load ledger', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadLedger()
  }, [page, refreshTrigger])

  const formatVND = (v: number) => new Intl.NumberFormat('vi-VN').format(v)

  const handleCopy = (code: string) => {
    navigator.clipboard.writeText(code)
    setCopiedCode(code)
    setTimeout(() => setCopiedCode(null), 2000)
  }

  const filteredLedgers = ledgers.filter((l) => {
    if (filterType === 'credit') return l.type.toLowerCase() === 'credit'
    if (filterType === 'debit') return l.type.toLowerCase() === 'debit'
    return true
  })

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
      {/* Explanation Banner */}
      <div style={{
        background: 'rgba(59, 130, 246, 0.08)',
        border: '1px solid rgba(59, 130, 246, 0.25)',
        borderRadius: 'var(--radius-md)',
        padding: '12px 16px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '10px',
      }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <ShieldCheck size={20} color="var(--info)" />
          <div style={{ fontSize: '0.84rem', color: 'var(--text-secondary)' }}>
            <strong>Sổ cái tài chính bất biến (Append-Only Ledger):</strong> Mỗi biến động tăng/giảm số dư đều tạo một dòng kiểm toán ghi nhận số dư trước và số dư sau trong cùng 1 Database Transaction.
          </div>
        </div>

        <button
          onClick={loadLedger}
          className="btn-secondary"
          style={{ padding: '6px 12px', fontSize: '0.8rem' }}
          title="Làm mới sổ cái"
        >
          <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
          Làm mới
        </button>
      </div>

      {/* Filter Tabs */}
      <div style={{ display: 'flex', gap: '8px' }}>
        {[
          { label: 'Tất cả', value: 'all' as const },
          { label: 'Tiền vào (+Credit)', value: 'credit' as const },
          { label: 'Tiền ra (-Debit)', value: 'debit' as const },
        ].map((f) => {
          const isSelected = filterType === f.value
          return (
            <button
              key={f.value}
              onClick={() => setFilterType(f.value)}
              style={{
                background: isSelected ? 'var(--momo-pink)' : 'rgba(255, 255, 255, 0.05)',
                color: isSelected ? 'white' : 'var(--text-secondary)',
                border: '1px solid var(--border-subtle)',
                borderRadius: 'var(--radius-full)',
                padding: '5px 14px',
                fontSize: '0.8rem',
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

      {/* Ledger Table */}
      {filteredLedgers.length === 0 ? (
        <div className="glass-panel" style={{ padding: '48px 24px', textAlign: 'center', color: 'var(--text-muted)' }}>
          <div style={{ fontSize: '1rem', fontWeight: 600 }}>Chưa có bản ghi sổ cái nào</div>
        </div>
      ) : (
        <div className="glass-panel" style={{ overflowX: 'auto' }}>
          <table style={{ width: '100%', borderCollapse: 'collapse', textAlign: 'left', fontSize: '0.86rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-subtle)', background: 'rgba(255, 255, 255, 0.02)' }}>
                <th style={{ padding: '14px 18px', color: 'var(--text-muted)', fontWeight: 600 }}>Loại</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-muted)', fontWeight: 600 }}>Số tiền</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-muted)', fontWeight: 600 }}>Số dư trước</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-muted)', fontWeight: 600 }}>Số dư sau</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-muted)', fontWeight: 600 }}>Mã giao dịch</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-muted)', fontWeight: 600 }}>Lời nhắn / Nguồn</th>
                <th style={{ padding: '14px 18px', color: 'var(--text-muted)', fontWeight: 600 }}>Thời gian</th>
              </tr>
            </thead>
            <tbody>
              {filteredLedgers.map((l) => {
                const isCredit = l.type.toLowerCase() === 'credit'
                const isCopied = copiedCode === l.transactionCode
                return (
                  <tr
                    key={l.id}
                    style={{
                      borderBottom: '1px solid var(--border-subtle)',
                      transition: 'background 0.15s ease',
                    }}
                  >
                    <td style={{ padding: '14px 18px' }}>
                      <span className={isCredit ? 'badge badge-success' : 'badge badge-danger'}>
                        {isCredit ? <ArrowDownLeft size={12} /> : <ArrowUpRight size={12} />}
                        {l.type}
                      </span>
                    </td>
                    <td style={{ padding: '14px 18px', fontWeight: 800, color: isCredit ? 'var(--success)' : 'var(--danger)' }}>
                      {isCredit ? '+' : '-'}
                      {formatVND(l.amount)} đ
                    </td>
                    <td style={{ padding: '14px 18px', color: 'var(--text-muted)', fontFamily: 'monospace' }}>
                      {formatVND(l.previousBalance)} đ
                    </td>
                    <td style={{ padding: '14px 18px', fontWeight: 700, color: 'var(--text-primary)', fontFamily: 'monospace' }}>
                      {formatVND(l.newBalance)} đ
                    </td>
                    <td style={{ padding: '14px 18px', fontFamily: 'monospace', fontSize: '0.78rem' }}>
                      <span
                        onClick={() => handleCopy(l.transactionCode)}
                        style={{
                          cursor: 'pointer',
                          color: isCopied ? 'var(--success)' : 'var(--momo-pink-light)',
                          display: 'inline-flex',
                          alignItems: 'center',
                          gap: '4px',
                        }}
                        title="Bấm để sao chép mã"
                      >
                        {l.transactionCode}
                        {isCopied ? <Check size={12} /> : <Copy size={12} />}
                      </span>
                    </td>
                    <td style={{ padding: '14px 18px', color: 'var(--text-secondary)' }}>
                      {l.description || 'Ghi nhận biến động số dư ví'}
                    </td>
                    <td style={{ padding: '14px 18px', color: 'var(--text-muted)', fontSize: '0.78rem' }}>
                      {new Date(l.createdAt).toLocaleString('vi-VN')}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
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
