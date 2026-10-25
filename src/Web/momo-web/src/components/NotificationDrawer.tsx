import React, { useState, useEffect } from 'react'
import { X, Bell, CheckCheck, RefreshCw, ArrowDownLeft, ArrowUpRight, Plus, Landmark, ShoppingBag } from 'lucide-react'
import { api } from '../services/api'
import { AppNotification, NotificationType } from '../types'

interface NotificationDrawerProps {
  isOpen: boolean
  onClose: () => void
  onRefreshCount: () => void
}

export const NotificationDrawer: React.FC<NotificationDrawerProps> = ({
  isOpen,
  onClose,
  onRefreshCount,
}) => {
  const [notifications, setNotifications] = useState<AppNotification[]>([])
  const [loading, setLoading] = useState(false)

  const loadNotifications = async () => {
    setLoading(true)
    try {
      const res = await api.getNotifications(1, 30)
      setNotifications(res.items)
      onRefreshCount()
    } catch (err) {
      console.error('Failed to load notifications', err)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    if (isOpen) {
      loadNotifications()
    }
  }, [isOpen])

  if (!isOpen) return null

  const handleMarkAsRead = async (id: string) => {
    try {
      await api.markAsRead(id)
      setNotifications((prev) =>
        prev.map((n) => (n.id === id ? { ...n, isRead: true } : n))
      )
      onRefreshCount()
    } catch (err) {
      console.error(err)
    }
  }

  const handleMarkAllRead = async () => {
    try {
      await api.markAllAsRead()
      setNotifications((prev) => prev.map((n) => ({ ...n, isRead: true })))
      onRefreshCount()
    } catch (err) {
      console.error(err)
    }
  }

  const getNotifIcon = (type: NotificationType) => {
    switch (type) {
      case NotificationType.MoneyReceived:
        return <ArrowDownLeft size={18} color="var(--success)" />
      case NotificationType.MoneySent:
        return <ArrowUpRight size={18} color="var(--momo-pink)" />
      case NotificationType.DepositSuccess:
        return <Plus size={18} color="var(--success)" />
      case NotificationType.WithdrawSuccess:
        return <Landmark size={18} color="var(--info)" />
      case NotificationType.PaymentSuccess:
        return <ShoppingBag size={18} color="var(--warning)" />
      default:
        return <Bell size={18} color="var(--momo-pink)" />
    }
  }

  const formatVND = (v: number) => new Intl.NumberFormat('vi-VN').format(v)

  return (
    <div className="modal-overlay" style={{ justifyContent: 'flex-end', padding: 0 }}>
      <div
        className="glass-panel"
        style={{
          width: '100%',
          maxWidth: '440px',
          height: '100vh',
          borderRadius: '0',
          borderLeft: '1px solid var(--border-subtle)',
          display: 'flex',
          flexDirection: 'column',
          background: '#0d131f',
          animation: 'slideLeft 0.25s cubic-bezier(0.16, 1, 0.3, 1)',
        }}
      >
        {/* Drawer Header */}
        <div style={{
          padding: '18px 20px',
          borderBottom: '1px solid var(--border-subtle)',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
        }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Bell size={20} color="var(--momo-pink)" />
            <h3 style={{ fontSize: '1.1rem', fontWeight: 800 }}>Hộp thư thông báo</h3>
          </div>
          <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
            <button
              onClick={loadNotifications}
              style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
              title="Làm mới"
            >
              <RefreshCw size={16} className={loading ? 'animate-spin' : ''} />
            </button>
            <button
              onClick={onClose}
              style={{ background: 'none', border: 'none', color: 'var(--text-muted)', cursor: 'pointer' }}
            >
              <X size={20} />
            </button>
          </div>
        </div>

        {/* Action bar */}
        <div style={{
          padding: '10px 20px',
          borderBottom: '1px solid var(--border-subtle)',
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'center',
          background: 'rgba(255, 255, 255, 0.02)',
        }}>
          <span style={{ fontSize: '0.78rem', color: 'var(--text-muted)' }}>
            Lắng nghe sự kiện RabbitMQ thời gian thực
          </span>
          <button
            onClick={handleMarkAllRead}
            style={{
              background: 'none',
              border: 'none',
              color: 'var(--momo-pink)',
              fontSize: '0.78rem',
              fontWeight: 700,
              cursor: 'pointer',
              display: 'flex',
              alignItems: 'center',
              gap: '4px',
            }}
          >
            <CheckCheck size={14} /> Đọc tất cả
          </button>
        </div>

        {/* Notifications List */}
        <div style={{ flex: 1, overflowY: 'auto', padding: '14px' }}>
          {notifications.length === 0 ? (
            <div style={{ padding: '60px 20px', textAlign: 'center', color: 'var(--text-muted)' }}>
              <Bell size={36} style={{ margin: '0 auto 12px auto', opacity: 0.3 }} />
              <div style={{ fontSize: '0.9rem', fontWeight: 600 }}>Chưa có thông báo nào</div>
              <div style={{ fontSize: '0.78rem', marginTop: '4px' }}>
                Các giao dịch chuyển/nhận tiền sẽ được tự động bắn thông báo qua RabbitMQ
              </div>
            </div>
          ) : (
            <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
              {notifications.map((n) => (
                <div
                  key={n.id}
                  onClick={() => !n.isRead && handleMarkAsRead(n.id)}
                  style={{
                    padding: '14px',
                    borderRadius: 'var(--radius-md)',
                    background: n.isRead ? 'rgba(255, 255, 255, 0.02)' : 'rgba(216, 45, 139, 0.08)',
                    border: n.isRead ? '1px solid var(--border-subtle)' : '1px solid rgba(216, 45, 139, 0.3)',
                    cursor: n.isRead ? 'default' : 'pointer',
                    display: 'flex',
                    gap: '12px',
                    transition: 'var(--transition)',
                  }}
                >
                  <div style={{
                    width: '36px',
                    height: '36px',
                    borderRadius: '10px',
                    background: 'rgba(255, 255, 255, 0.05)',
                    display: 'flex',
                    alignItems: 'center',
                    justifyContent: 'center',
                    flexShrink: 0,
                  }}>
                    {getNotifIcon(n.type)}
                  </div>

                  <div style={{ flex: 1, minWidth: 0 }}>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', marginBottom: '3px' }}>
                      <div style={{ fontSize: '0.86rem', fontWeight: n.isRead ? 600 : 800, color: 'var(--text-primary)' }}>
                        {n.title}
                      </div>
                      {!n.isRead && (
                        <div style={{
                          width: '8px',
                          height: '8px',
                          borderRadius: 'var(--radius-full)',
                          background: 'var(--momo-pink)',
                          boxShadow: '0 0 6px var(--momo-pink)',
                          flexShrink: 0,
                          marginTop: '4px',
                        }} />
                      )}
                    </div>
                    <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', lineHeight: 1.4, marginBottom: '6px' }}>
                      {n.content}
                    </div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.72rem', color: 'var(--text-muted)' }}>
                      <span>{new Date(n.createdAt).toLocaleTimeString('vi-VN', { hour: '2-digit', minute: '2-digit', second: '2-digit' })} • {new Date(n.createdAt).toLocaleDateString('vi-VN')}</span>
                      {n.amount && <span style={{ fontWeight: 700, color: 'var(--text-primary)' }}>{formatVND(n.amount)} đ</span>}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>
    </div>
  )
}
