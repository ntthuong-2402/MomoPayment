import { useState, useEffect } from 'react'
import { Navbar } from './components/Navbar'
import { WalletCard } from './components/WalletCard'
import { TransferModal } from './components/TransferModal'
import { DepositModal } from './components/DepositModal'
import { WithdrawModal } from './components/WithdrawModal'
import { PaymentModal } from './components/PaymentModal'
import { NotificationDrawer } from './components/NotificationDrawer'
import { TransactionHistory } from './components/TransactionHistory'
import { LedgerView } from './components/LedgerView'
import { AuthModal } from './components/AuthModal'
import { api } from './services/api'
import { User, Wallet } from './types'
import { History, FileSpreadsheet, Server, Activity } from 'lucide-react'

export function App() {
  const [currentUser, setCurrentUser] = useState<User | null>(null)
  const [wallet, setWallet] = useState<Wallet | null>(null)
  const [unreadCount, setUnreadCount] = useState<number>(0)
  const [loading, setLoading] = useState<boolean>(true)
  const [refreshTrigger, setRefreshTrigger] = useState<number>(0)
  const [activeTab, setActiveTab] = useState<'transactions' | 'ledger'>('transactions')

  // Modals state
  const [showTransfer, setShowTransfer] = useState(false)
  const [showDeposit, setShowDeposit] = useState(false)
  const [showWithdraw, setShowWithdraw] = useState(false)
  const [showPayment, setShowPayment] = useState(false)
  const [showNotifications, setShowNotifications] = useState(false)
  const [showAuth, setShowAuth] = useState(false)

  const [serviceStatus, setServiceStatus] = useState({
    gateway: true,
    identity: true,
    wallet: true,
    transaction: true,
    notification: true,
  })

  // Check API Gateway health periodically
  const checkGatewayHealth = async () => {
    try {
      const res = await fetch('/api/health', { signal: AbortSignal.timeout(3000) })
      setServiceStatus((prev) => ({ ...prev, gateway: res.ok }))
    } catch {
      setServiceStatus((prev) => ({ ...prev, gateway: false }))
    }
  }

  useEffect(() => {
    checkGatewayHealth()
    const timer = setInterval(checkGatewayHealth, 15000)
    return () => clearInterval(timer)
  }, [])

  // Listen for 401 token expiration from api interceptor
  useEffect(() => {
    const handleAuthExpired = () => {
      setCurrentUser(null)
      setWallet(null)
      setUnreadCount(0)
      setShowAuth(true)
    }
    window.addEventListener('momo_auth_expired', handleAuthExpired)
    return () => window.removeEventListener('momo_auth_expired', handleAuthExpired)
  }, [])

  // Load user data & balance
  const loadUserData = async () => {
    setLoading(true)
    try {
      const user = await api.getCurrentUser()
      setCurrentUser(user)
      setServiceStatus((s) => ({ ...s, identity: true }))

      const wal = await api.getBalance()
      setWallet(wal)
      setServiceStatus((s) => ({ ...s, wallet: true }))

      const count = await api.getUnreadCount()
      setUnreadCount(count)
      setServiceStatus((s) => ({ ...s, notification: true }))
    } catch (err) {
      console.warn('No active session, auto-logging in default demo user Alice...', err)
      // Auto login as default demo user Alice
      try {
        await api.login('0901234567', 'Password123@')
        const user = await api.getCurrentUser()
        setCurrentUser(user)
        setServiceStatus((s) => ({ ...s, identity: true }))

        const wal = await api.getBalance()
        setWallet(wal)
        setServiceStatus((s) => ({ ...s, wallet: true }))

        const count = await api.getUnreadCount()
        setUnreadCount(count)
        setServiceStatus((s) => ({ ...s, notification: true }))
      } catch (loginErr) {
        console.error('Auto login failed:', loginErr)
        setCurrentUser(null)
        setWallet(null)
      }
    } finally {
      setLoading(false)
    }
  }

  // Initial load
  useEffect(() => {
    loadUserData()
  }, [refreshTrigger])

  // Periodic polling for unread notifications and auto-refreshing balance in real-time
  useEffect(() => {
    if (!currentUser) return
    const interval = setInterval(async () => {
      try {
        const count = await api.getUnreadCount()
        setUnreadCount((prev) => {
          if (count !== prev) {
            // New transaction / notification arrived, automatically sync wallet balance
            api.getBalance().then((wal) => {
              setWallet(wal)
              setServiceStatus((s) => ({ ...s, wallet: true }))
            }).catch(() => {})
            setRefreshTrigger((r) => r + 1)
          }
          return count
        })
        setServiceStatus((s) => ({ ...s, notification: true }))
      } catch {
        // Ignore background polling errors
      }
    }, 5000)
    return () => clearInterval(interval)
  }, [currentUser])

  // Quick switch user (Alice, Bob, Charlie)
  const handleQuickSwitchUser = async (phone: string) => {
    setLoading(true)
    try {
      await api.login(phone, 'Password123@')
      setRefreshTrigger((prev) => prev + 1)
    } catch (err: any) {
      alert(`Không thể chuyển sang tài khoản ${phone}: ${err.message}`)
    } finally {
      setLoading(false)
    }
  }

  const handleLogout = () => {
    api.setToken(null)
    setCurrentUser(null)
    setWallet(null)
    setUnreadCount(0)
    setShowAuth(true)
  }

  const handleActionSuccess = () => {
    setRefreshTrigger((prev) => prev + 1)
  }

  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      {/* Top Sticky Navbar */}
      <Navbar
        currentUser={currentUser}
        unreadCount={unreadCount}
        onOpenNotifications={() => setShowNotifications(true)}
        onQuickSwitchUser={handleQuickSwitchUser}
        onOpenAuth={() => setShowAuth(true)}
        onLogout={handleLogout}
      />

      {/* Main Content Area */}
      <main style={{
        maxWidth: '1200px',
        margin: '0 auto',
        padding: '24px 20px 48px 20px',
        width: '100%',
        flex: 1,
      }}>
        <div className="dashboard-grid" style={{
          display: 'grid',
          gridTemplateColumns: 'minmax(320px, 380px) 1fr',
          gap: '24px',
          alignItems: 'start',
        }}>
          {/* Left Column: Wallet Card & Quick Stats */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
            <WalletCard
              wallet={wallet}
              loading={loading}
              onRefresh={() => setRefreshTrigger((p) => p + 1)}
              onOpenTransfer={() => currentUser ? setShowTransfer(true) : setShowAuth(true)}
              onOpenDeposit={() => currentUser ? setShowDeposit(true) : setShowAuth(true)}
              onOpenWithdraw={() => currentUser ? setShowWithdraw(true) : setShowAuth(true)}
              onOpenPayment={() => currentUser ? setShowPayment(true) : setShowAuth(true)}
            />

            {/* Microservice Health Indicators */}
            <div className="glass-panel" style={{ padding: '20px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '8px', marginBottom: '14px' }}>
                <Server size={18} color="var(--momo-pink)" />
                <h4 style={{ fontSize: '0.92rem', fontWeight: 800 }}>Trạng thái Microservices</h4>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: '10px', fontSize: '0.8rem' }}>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>API Gateway (YARP)</span>
                  <span className={`badge ${serviceStatus.gateway ? 'badge-success' : 'badge-danger'}`} style={{ fontSize: '0.68rem', padding: '2px 8px' }}>
                    <Activity size={10} /> :5000 {serviceStatus.gateway ? 'Online' : 'Offline'}
                  </span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Identity Service</span>
                  <span className={`badge ${serviceStatus.identity ? 'badge-success' : 'badge-danger'}`} style={{ fontSize: '0.68rem', padding: '2px 8px' }}>
                    :5001 {serviceStatus.identity ? 'Online' : 'Offline'}
                  </span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Wallet (REST + gRPC)</span>
                  <span className={`badge ${serviceStatus.wallet ? 'badge-success' : 'badge-danger'}`} style={{ fontSize: '0.68rem', padding: '2px 8px' }}>
                    :5002 / :50052 {serviceStatus.wallet ? 'Online' : 'Offline'}
                  </span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Transaction Service</span>
                  <span className={`badge ${serviceStatus.transaction ? 'badge-success' : 'badge-danger'}`} style={{ fontSize: '0.68rem', padding: '2px 8px' }}>
                    :5003 {serviceStatus.transaction ? 'Online' : 'Offline'}
                  </span>
                </div>
                <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <span style={{ color: 'var(--text-secondary)' }}>Notification & RabbitMQ</span>
                  <span className={`badge ${serviceStatus.notification ? 'badge-success' : 'badge-danger'}`} style={{ fontSize: '0.68rem', padding: '2px 8px' }}>
                    :5004 {serviceStatus.notification ? 'Online' : 'Offline'}
                  </span>
                </div>
              </div>
            </div>
          </div>

          {/* Right Column: Tabbed Sections (Transactions & Ledger) */}
          <div style={{ display: 'flex', flexDirection: 'column', gap: '18px' }}>
            {/* Tabs Header */}
            <div style={{
              display: 'flex',
              gap: '8px',
              borderBottom: '1px solid var(--border-subtle)',
              paddingBottom: '8px',
            }}>
              <button
                onClick={() => setActiveTab('transactions')}
                style={{
                  background: activeTab === 'transactions' ? 'var(--momo-pink)' : 'transparent',
                  color: activeTab === 'transactions' ? 'white' : 'var(--text-muted)',
                  border: 'none',
                  borderRadius: 'var(--radius-md)',
                  padding: '10px 18px',
                  fontWeight: 700,
                  fontSize: '0.92rem',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  transition: 'var(--transition)',
                }}
              >
                <History size={18} /> Lịch sử giao dịch
              </button>

              <button
                onClick={() => setActiveTab('ledger')}
                style={{
                  background: activeTab === 'ledger' ? 'var(--momo-pink)' : 'transparent',
                  color: activeTab === 'ledger' ? 'white' : 'var(--text-muted)',
                  border: 'none',
                  borderRadius: 'var(--radius-md)',
                  padding: '10px 18px',
                  fontWeight: 700,
                  fontSize: '0.92rem',
                  cursor: 'pointer',
                  display: 'flex',
                  alignItems: 'center',
                  gap: '8px',
                  transition: 'var(--transition)',
                }}
              >
                <FileSpreadsheet size={18} /> Sổ cái kiểm toán (Ledger)
              </button>
            </div>

            {/* Tab Body */}
            {currentUser ? (
              <>
                {activeTab === 'transactions' ? (
                  <TransactionHistory
                    currentUserId={currentUser.id}
                    refreshTrigger={refreshTrigger}
                  />
                ) : (
                  <LedgerView refreshTrigger={refreshTrigger} />
                )}
              </>
            ) : (
              <div className="glass-panel" style={{ padding: '60px 24px', textAlign: 'center' }}>
                <div style={{ fontSize: '1.25rem', fontWeight: 800, marginBottom: '8px' }}>
                  Chào mừng bạn đến với Ví điện tử MoMo
                </div>
                <p style={{ color: 'var(--text-muted)', fontSize: '0.88rem', marginBottom: '24px' }}>
                  Vui lòng đăng nhập hoặc chọn nhanh một tài khoản ở thanh điều hướng trên cùng để trải nghiệm hệ sinh thái thanh toán Microservices.
                </p>
                <button className="btn-primary" onClick={() => setShowAuth(true)}>
                  Đăng nhập / Đăng ký ngay
                </button>
              </div>
            )}
          </div>
        </div>
      </main>

      {/* Footer */}
      <footer style={{
        borderTop: '1px solid var(--border-subtle)',
        padding: '20px 24px',
        textAlign: 'center',
        fontSize: '0.8rem',
        color: 'var(--text-muted)',
        background: 'rgba(10, 14, 23, 0.9)',
      }}>
        MomoPayment Microservices Payment Ecosystem • C# 13 / .NET 10 • Redlock Concurrency Control • RabbitMQ Event Bus
      </footer>

      {/* Modals */}
      {currentUser && wallet && (
        <>
          <TransferModal
            isOpen={showTransfer}
            onClose={() => setShowTransfer(false)}
            onSuccess={handleActionSuccess}
            currentPhone={currentUser.phoneNumber}
            currentBalance={wallet.balance}
          />

          <DepositModal
            isOpen={showDeposit}
            onClose={() => setShowDeposit(false)}
            onSuccess={handleActionSuccess}
          />

          <WithdrawModal
            isOpen={showWithdraw}
            onClose={() => setShowWithdraw(false)}
            onSuccess={handleActionSuccess}
            currentBalance={wallet.balance}
          />

          <PaymentModal
            isOpen={showPayment}
            onClose={() => setShowPayment(false)}
            onSuccess={handleActionSuccess}
            currentBalance={wallet.balance}
          />
        </>
      )}

      <NotificationDrawer
        isOpen={showNotifications}
        onClose={() => setShowNotifications(false)}
        onRefreshCount={() => setRefreshTrigger((p) => p + 1)}
      />

      <AuthModal
        isOpen={showAuth}
        onClose={() => setShowAuth(false)}
        onSuccess={() => setRefreshTrigger((p) => p + 1)}
      />
    </div>
  )
}
export default App
