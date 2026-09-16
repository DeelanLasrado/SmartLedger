import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, saveSession } from '../api'

export function LoginPage() {
  const navigate = useNavigate()
  const [email, setEmail] = useState('demo@smartledger.local')
  const [password, setPassword] = useState('Demo@12345')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      const session = await api.login(email, password)
      saveSession(session)
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="panel auth-card" onSubmit={onSubmit}>
        <h1 className="brand-mark">SmartLedger</h1>
        <p className="lead">CFO-level clarity for Indian small businesses — invoices, GST, and cash flow in plain English.</p>
        {error && <div className="error">{error}</div>}
        <div className="field">
          <label htmlFor="email">Email</label>
          <input id="email" value={email} onChange={(e) => setEmail(e.target.value)} autoComplete="username" />
        </div>
        <div className="field">
          <label htmlFor="password">Password</label>
          <input id="password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} autoComplete="current-password" />
        </div>
        <button className="btn" type="submit" disabled={loading} style={{ width: '100%' }}>
          {loading ? 'Signing in…' : 'Enter workspace'}
        </button>
        <p className="muted" style={{ marginTop: 16 }}>
          Demo seeded account works out of the box. Need a new tenant? <Link to="/register">Register</Link>
        </p>
      </form>
    </div>
  )
}
