import React, { useState } from 'react'
import { Eye, EyeOff, Send, PlusCircle, Landmark, QrCode, RefreshCw } from 'lucide-react'
import { Wallet } from '../types'

interface WalletCardProps {
  wallet: Wallet | null
  loading: boolean
  onRefresh: () => void
  onOpenTransfer: () => void
  onOpenDeposit: () => void
  onOpenWithdraw: () => void
  onOpenPayment: () => void
}

export const WalletCard: React.FC<WalletCardProps> = ({
  wallet,
  loading,
  onRefresh,
  onOpenTransfer,
  onOpenDeposit,
  onOpenWithdraw,
  onOpenPayment,
}) => {
  const [showBalance, setShowBalance] = useState(true)

  const formatCurrency = (val: number) => {
    return new Intl.NumberFormat('vi-VN').format(val)
  }

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* MoMo Premium Wallet Card */}
      <div style={{
        background: 'var(--momo-gradient)',
        borderRadius: 'var(--radius-xl)',
        padding: '28px',
        position: 'relative',
        overflow: 'hidden',
        boxShadow: 'var(--momo-glow-lg)',
        color: 'white',
      }}>
        {/* Background decorative watermark */}
        <div style={{
          position: 'absolute',
          right: '-20px',
          bottom: '-30px',
          fontSize: '11rem',
          fontWeight: 900,
          color: 'rgba(255, 255, 255, 0.08)',
          userSelect: 'none',
          pointerEvents: 'none',
          lineHeight: 1,
        }}>
          M
        </div>

        {/* Card Header */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: '20px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div style={{
              width: '32px',
              height: '24px',
              borderRadius: '6px',
              background: 'linear-gradient(135deg, #ffd700 0%, #ffa500 100%)',
              boxShadow: '0 2px 6px rgba(0,0,0,0.2)',
            }} />
            <span style={{ fontSize: '0.82rem', fontWeight: 700, letterSpacing: '1px', opacity: 0.9 }}>
              VÍ ĐIỆN TỬ MOMO
            </span>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <button
              onClick={onRefresh}
              style={{
                background: 'rgba(255, 255, 255, 0.2)',
                border: 'none',
                borderRadius: 'var(--radius-full)',
                width: '32px',
                height: '32px',
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: 'white',
                cursor: 'pointer',
                transition: 'var(--transition)',
              }}
              title="Làm mới số dư"
            >
              <RefreshCw size={14} className={loading ? 'animate-spin' : ''} />
            </button>
            <span style={{
              background: 'rgba(255, 255, 255, 0.25)',
              padding: '3px 10px',
              borderRadius: 'var(--radius-full)',
              fontSize: '0.72rem',
              fontWeight: 800,
            }}>
              ACTIVE
            </span>
          </div>
        </div>

        {/* Balance Display */}
        <div style={{ marginBottom: '24px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '6px' }}>
            <span style={{ fontSize: '0.85rem', opacity: 0.85 }}>Số dư khả dụng</span>
            <button
              onClick={() => setShowBalance(!showBalance)}
              style={{
                background: 'none',
                border: 'none',
                color: 'white',
                cursor: 'pointer',
                display: 'flex',
                alignItems: 'center',
                opacity: 0.85,
              }}
              title={showBalance ? 'Ẩn số dư' : 'Hiện số dư'}
            >
              {showBalance ? <Eye size={16} /> : <EyeOff size={16} />}
            </button>
          </div>
          <div style={{ fontSize: '2.4rem', fontWeight: 800, letterSpacing: '-0.5px', textShadow: '0 2px 10px rgba(0,0,0,0.2)' }}>
            {wallet ? (
              showBalance ? `${formatCurrency(wallet.balance)} đ` : '•••••••• đ'
            ) : (
              '0 đ'
            )}
          </div>
        </div>

        {/* Card Footer: Phone & Currency */}
        <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', opacity: 0.9 }}>
          <div>
            <div style={{ fontSize: '0.72rem', textTransform: 'uppercase', letterSpacing: '0.5px' }}>Số ví MoMo</div>
            <div style={{ fontSize: '1rem', fontWeight: 700, letterSpacing: '1px' }}>
              {wallet ? wallet.phoneNumber : '---'}
            </div>
          </div>
          <div style={{ fontSize: '0.82rem', fontWeight: 700 }}>
            {wallet?.currency || 'VND'}
          </div>
        </div>
      </div>

      {/* 4 Main Action Quick Buttons */}
      <div style={{
        display: 'grid',
        gridTemplateColumns: 'repeat(4, 1fr)',
        gap: '12px',
      }}>
        <button
          onClick={onOpenTransfer}
          className="glass-panel"
          style={{
            padding: '16px 8px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: '10px',
            cursor: 'pointer',
            color: 'var(--text-primary)',
            textAlign: 'center',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <div style={{
            width: '46px',
            height: '46px',
            borderRadius: '14px',
            background: 'linear-gradient(135deg, #10b981 0%, #059669 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'white',
            boxShadow: '0 4px 14px rgba(16, 185, 129, 0.4)',
          }}>
            <Send size={22} />
          </div>
          <span style={{ fontSize: '0.84rem', fontWeight: 700 }}>Chuyển tiền</span>
        </button>

        <button
          onClick={onOpenDeposit}
          className="glass-panel"
          style={{
            padding: '16px 8px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: '10px',
            cursor: 'pointer',
            color: 'var(--text-primary)',
            textAlign: 'center',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <div style={{
            width: '46px',
            height: '46px',
            borderRadius: '14px',
            background: 'var(--momo-gradient)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'white',
            boxShadow: 'var(--momo-glow)',
          }}>
            <PlusCircle size={22} />
          </div>
          <span style={{ fontSize: '0.84rem', fontWeight: 700 }}>Nạp tiền</span>
        </button>

        <button
          onClick={onOpenWithdraw}
          className="glass-panel"
          style={{
            padding: '16px 8px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: '10px',
            cursor: 'pointer',
            color: 'var(--text-primary)',
            textAlign: 'center',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <div style={{
            width: '46px',
            height: '46px',
            borderRadius: '14px',
            background: 'linear-gradient(135deg, #3b82f6 0%, #2563eb 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'white',
            boxShadow: '0 4px 14px rgba(59, 130, 246, 0.4)',
          }}>
            <Landmark size={22} />
          </div>
          <span style={{ fontSize: '0.84rem', fontWeight: 700 }}>Rút tiền</span>
        </button>

        <button
          onClick={onOpenPayment}
          className="glass-panel"
          style={{
            padding: '16px 8px',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: '10px',
            cursor: 'pointer',
            color: 'var(--text-primary)',
            textAlign: 'center',
            border: '1px solid var(--border-subtle)',
          }}
        >
          <div style={{
            width: '46px',
            height: '46px',
            borderRadius: '14px',
            background: 'linear-gradient(135deg, #f59e0b 0%, #d97706 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            color: 'white',
            boxShadow: '0 4px 14px rgba(245, 158, 11, 0.4)',
          }}>
            <QrCode size={22} />
          </div>
          <span style={{ fontSize: '0.84rem', fontWeight: 700 }}>Thanh toán</span>
        </button>
      </div>
    </div>
  )
}
