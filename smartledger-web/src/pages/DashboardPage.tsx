import { useEffect, useState } from 'react'
import { api, inr, type DashboardSummary } from '../api'

export function DashboardPage() {
  const [data, setData] = useState<DashboardSummary | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    api.summary().then(setData).catch((e) => setError(e.message))
  }, [])

  const max = Math.max(...(data?.categoryBreakdown.map((c) => c.total) ?? [1]), 1)

  return (
    <>
      <div className="page-head">
        <div>
          <h1>{data?.businessName ?? 'Overview'}</h1>
          <p>Spend pulse, anomalies, and category mix for your business this month.</p>
        </div>
      </div>
      {error && <div className="error">{error}</div>}
      <div className="grid stats" style={{ marginBottom: 18 }}>
        <div className="stat">
          <div className="label">Month spend</div>
          <div className="value">{inr(data?.monthSpend ?? 0)}</div>
        </div>
        <div className="stat">
          <div className="label">Invoices</div>
          <div className="value">{data?.invoiceCount ?? '—'}</div>
        </div>
        <div className="stat">
          <div className="label">Anomalies</div>
          <div className="value">{data?.anomalyCount ?? '—'}</div>
        </div>
        <div className="stat">
          <div className="label">Plan usage</div>
          <div className="value" style={{ fontSize: '1.25rem' }}>
            {data ? `${data.invoicesUsedThisMonth}/${data.monthlyInvoiceLimit === 2147483647 ? '∞' : data.monthlyInvoiceLimit}` : '—'}
          </div>
        </div>
      </div>
      <div className="grid two">
        <section className="panel">
          <h2 style={{ marginTop: 0 }}>Category mix</h2>
          <div className="chart-bars">
            {(data?.categoryBreakdown ?? []).slice(0, 6).map((c, i) => (
              <div className="bar-wrap" key={c.category}>
                <div className="bar" style={{ height: `${Math.max(12, (c.total / max) * 100)}%`, animationDelay: `${i * 0.05}s` }} />
                <span>{c.category}</span>
              </div>
            ))}
            {!data?.categoryBreakdown?.length && <p className="muted">Upload invoices to see category trends.</p>}
          </div>
        </section>
        <section className="panel">
          <h2 style={{ marginTop: 0 }}>Plan</h2>
          <p className="muted">Tier: <strong>{data?.tier ?? '—'}</strong></p>
          <p className="muted">SmartLedger flags unusual bills and keeps GST matching ready for month-end.</p>
        </section>
      </div>
    </>
  )
}
