import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import { clearSession, getStoredEmail } from './api'

const links = [
  { to: '/', label: 'Overview', end: true },
  { to: '/invoices', label: 'Invoices' },
  { to: '/ask', label: 'Ask AI' },
  { to: '/gst', label: 'GST' },
  { to: '/cashflow', label: 'Cash flow' },
]

export function AppLayout() {
  const navigate = useNavigate()
  const email = getStoredEmail()

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="brand">
          <strong>SmartLedger</strong>
          <span>Expense intelligence for SMBs</span>
        </div>
        <nav className="nav">
          {links.map((l) => (
            <NavLink key={l.to} to={l.to} end={l.end} className={({ isActive }) => (isActive ? 'active' : undefined)}>
              {l.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-foot">
          <div className="user-chip">{email}</div>
          <button
            className="btn ghost"
            style={{ color: '#e8f5f1', borderColor: 'rgba(255,255,255,0.2)' }}
            onClick={() => {
              clearSession()
              navigate('/login')
            }}
          >
            Sign out
          </button>
        </div>
      </aside>
      <main className="main">
        <Outlet />
      </main>
    </div>
  )
}
