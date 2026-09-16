import { useState, type FormEvent } from 'react'
import { api, inr, type GstResult } from '../api'

export function GstPage() {
  const now = new Date()
  const defaultPeriod = `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}`
  const [period, setPeriod] = useState(defaultPeriod)
  const [result, setResult] = useState<GstResult | null>(null)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      setResult(await api.reconcileGst(period))
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Reconcile failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h1>GST reconciliation</h1>
          <p>Match GSTR-2A vs GSTR-3B entries and surface mismatches for the selected period.</p>
        </div>
      </div>
      <form className="panel" onSubmit={onSubmit} style={{ marginBottom: 16 }}>
        {error && <div className="error">{error}</div>}
        <div className="field" style={{ maxWidth: 220 }}>
          <label>Period (YYYY-MM)</label>
          <input value={period} onChange={(e) => setPeriod(e.target.value)} pattern="\d{4}-(0[1-9]|1[0-2])" required />
        </div>
        <button className="btn" type="submit" disabled={loading}>
          {loading ? 'Reconciling…' : 'Run reconciliation'}
        </button>
      </form>
      {result && (
        <div className="grid two">
          <section className="panel">
            <h2 style={{ marginTop: 0 }}>{result.period}</h2>
            <p>Matched: <strong>{result.matchedCount}</strong></p>
            <p>Mismatches: <strong>{result.mismatchCount}</strong></p>
            <p>Net tax liability: <strong>{inr(result.netTaxLiability)}</strong></p>
          </section>
          <section className="panel">
            <h2 style={{ marginTop: 0 }}>Mismatch detail</h2>
            {!result.mismatches.length && <p className="muted">No mismatches for this period (or no GST entries imported yet).</p>}
            <ul>
              {result.mismatches.map((m, i) => (
                <li key={`${m.invoiceNumber}-${i}`}>
                  {m.invoiceNumber}: {m.reason} ({inr(m.gstr2AAmount)} vs {inr(m.gstr3BAmount)})
                </li>
              ))}
            </ul>
          </section>
        </div>
      )}
    </>
  )
}
