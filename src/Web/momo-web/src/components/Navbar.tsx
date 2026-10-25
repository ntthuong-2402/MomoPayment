import React from 'react'
import { Bell, LogOut, UserCircle2, ArrowRightLeft, ShieldCheck } from 'lucide-react'
import { User } from '../types'

interface NavbarProps {
  currentUser: User | null
  unreadCount: number
  onOpenNotifications: () => void
  onQuickSwitchUser: (phone: string) => void
  onOpenAuth: () => void
  onLogout: () => void
}

export const Navbar: React.FC<NavbarProps> = ({
  currentUser,
  unreadCount,
  onOpenNotifications,
  onQuickSwitchUser,
  onOpenAuth,
  onLogout,
}) => {
  const seedAccounts = [
    { name: 'Alice', phone: '0901234567', label: 'Alice (0901234567)' },
    { name: 'Bob', phone: '0907654321', label: 'Bob (0907654321)' },
    { name: 'Charlie', phone: '0912345678', label: 'Charlie (0912345678)' },
  ]

  return (
    <header style={{
      borderBottom: '1px solid var(--border-subtle)',
      background: 'rgba(10, 14, 23, 0.85)',
      backdropFilter: 'blur(16px)',
      position: 'sticky',
      top: 0,
      zIndex: 100,
    }}>
      <div style={{
        maxWidth: '1200px',
        margin: '0 auto',
        padding: '14px 24px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        flexWrap: 'wrap',
        gap: '16px',
      }}>
        {/* Brand logo & title */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
          <div style={{
            width: '42px',
            height: '42px',
            borderRadius: '12px',
            background: 'var(--momo-gradient)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: 'var(--momo-glow)',
            fontWeight: 900,
            fontSize: '1.4rem',
            color: 'white',
          }}>
            M
          </div>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span style={{ fontSize: '1.25rem', fontWeight: 800, letterSpacing: '-0.5px' }}>
                momo <span style={{ color: 'var(--momo-pink)' }}>Payment</span>
              </span>
              <span className="badge badge-info" style={{ fontSize: '0.68rem', padding: '2px 8px' }}>
                <ShieldCheck size={12} /> .NET 10 Microservices
              </span>
            </div>
            <div style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>
              YARP Gateway:5000 • SQL Server • Redis 7 • RabbitMQ • gRPC
            </div>
          </div>
        </div>

        {/* Quick User Switcher & User Actions */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', flexWrap: 'wrap' }}>
          {currentUser ? (
            <>
              {/* Switch quick pill */}
              <div style={{
                display: 'flex',
                alignItems: 'center',
                gap: '6px',
                background: 'rgba(255, 255, 255, 0.04)',
                border: '1px solid var(--border-subtle)',
                borderRadius: 'var(--radius-full)',
                padding: '4px 6px',
              }}>
                <span style={{ fontSize: '0.75rem', color: 'var(--text-muted)', paddingLeft: '8px', display: 'flex', alignItems: 'center', gap: '4px' }}>
                  <ArrowRightLeft size={12} /> Chuyển nhanh:
                </span>
                {seedAccounts.map((acc) => {
                  const isActive = currentUser.phoneNumber === acc.phone
                  return (
                    <button
                      key={acc.phone}
                      onClick={() => onQuickSwitchUser(acc.phone)}
                      style={{
                        background: isActive ? 'var(--momo-gradient)' : 'transparent',
                        color: isActive ? 'white' : 'var(--text-secondary)',
                        border: 'none',
                        borderRadius: 'var(--radius-full)',
                        padding: '4px 12px',
                        fontSize: '0.78rem',
                        fontWeight: isActive ? 700 : 500,
                        cursor: 'pointer',
                        transition: 'var(--transition)',
                      }}
                    >
                      {acc.name}
                    </button>
                  )
                })}
              </div>

              {/* Notification Bell */}
              <button
                onClick={onOpenNotifications}
                style={{
                  position: 'relative',
                  width: '42px',
                  height: '42px',
                  borderRadius: '12px',
                  background: 'rgba(255, 255, 255, 0.06)',
                  border: '1px solid var(--border-subtle)',
                  color: 'var(--text-primary)',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'center',
                  cursor: 'pointer',
                  transition: 'var(--transition)',
                }}
                title="Thông báo biến động số dư"
              >
                <Bell size={20} />
                {unreadCount > 0 && (
                  <span style={{
                    position: 'absolute',
                    top: '-4px',
                    right: '-4px',
                    background: 'var(--momo-pink)',
                    color: 'white',
                    fontSize: '0.68rem',
                    fontWeight: 800,
                    minWidth: '18px',
                    height: '18px',
                    borderRadius: 'var(--radius-full)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    padding: '0 4px',
                    boxShadow: '0 0 10px rgba(216, 45, 139, 0.8)',
                    animation: 'pulseGlow 2s infinite',
                  }}>
                    {unreadCount > 99 ? '99+' : unreadCount}
                  </span>
                )}
              </button>

              {/* Current user badge & Logout */}
              <div style={{
                display: 'flex',
                alignItems: 'center',
                gap: '10px',
                background: 'rgba(255, 255, 255, 0.05)',
                border: '1px solid var(--border-subtle)',
                borderRadius: 'var(--radius-md)',
                padding: '6px 12px',
              }}>
                <UserCircle2 size={24} color="var(--momo-pink-light)" />
                <div style={{ lineHeight: 1.2 }}>
                  <div style={{ fontSize: '0.88rem', fontWeight: 700 }}>{currentUser.fullName}</div>
                  <div style={{ fontSize: '0.74rem', color: 'var(--text-muted)' }}>{currentUser.phoneNumber}</div>
                </div>
                <button
                  onClick={onLogout}
                  style={{
                    background: 'transparent',
                    border: 'none',
                    color: 'var(--text-muted)',
                    cursor: 'pointer',
                    marginLeft: '6px',
                    display: 'flex',
                    alignItems: 'center',
                    transition: 'var(--transition)',
                  }}
                  title="Đăng xuất"
                >
                  <LogOut size={16} />
                </button>
              </div>
            </>
          ) : (
            <button className="btn-primary" onClick={onOpenAuth}>
              Đăng nhập / Đăng ký
            </button>
          )}
        </div>
      </div>
    </header>
  )
}
