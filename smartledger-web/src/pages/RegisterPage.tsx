import { useState, type FormEvent } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { api, saveSession } from '../api'

export function RegisterPage() {
  const navigate = useNavigate()
  const [businessName, setBusinessName] = useState('')
  const [ownerEmail, setOwnerEmail] = useState('')
  const [password, setPassword] = useState('')
  const [gstin, setGstin] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      const session = await api.register({
        businessName,
        ownerEmail,
        password,
        gstin: gstin || undefined,
      })
      saveSession({
        accessToken: session.accessToken,
        refreshToken: session.refreshToken,
        tenantId: session.tenantId,
        email: ownerEmail,
      })
      navigate('/')
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registration failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <div className="auth-page">
      <form className="panel auth-card" onSubmit={onSubmit}>
        <h1 className="brand-mark">SmartLedger</h1>
        <p className="lead">Create your business workspace in under a minute.</p>
        {error && <div className="error">{error}</div>}
        <div className="field">
          <label>Business name</label>
          <input value={businessName} onChange={(e) => setBusinessName(e.target.value)} required />
        </div>
        <div className="field">
          <label>Owner email</label>
          <input type="email" value={ownerEmail} onChange={(e) => setOwnerEmail(e.target.value)} required />
        </div>
        <div className="field">
          <label>Password</label>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} minLength={8} required />
        </div>
        <div className="field">
          <label>GSTIN (optional)</label>
          <input value={gstin} onChange={(e) => setGstin(e.target.value)} placeholder="29AABCD1234G1Z7" />
        </div>
        <button className="btn" type="submit" disabled={loading} style={{ width: '100%' }}>
          {loading ? 'Creating…' : 'Create tenant'}
        </button>
        <p className="muted" style={{ marginTop: 16 }}>
          Already have an account? <Link to="/login">Sign in</Link>
        </p>
      </form>
    </div>
  )
}
