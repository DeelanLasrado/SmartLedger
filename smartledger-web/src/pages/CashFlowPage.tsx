import { useEffect, useState } from 'react'
import { api, inr, type CashFlowResult } from '../api'

export function CashFlowPage() {
  const [horizon, setHorizon] = useState(30)
  const [data, setData] = useState<CashFlowResult | null>(null)
  const [error, setError] = useState('')

  useEffect(() => {
    setError('')
    api
      .cashflow(horizon)
      .then(setData)
      .catch((e) => setError(e.message))
  }, [horizon])

  const sample = data?.points.filter((_, i) => i % Math.ceil((data.points.length || 1) / 10) === 0) ?? []
  const min = Math.min(...(sample.map((p) => p.predictedBalance) ?? [0]), 0)
  const max = Math.max(...(sample.map((p) => p.predictedBalance) ?? [1]), 1)
  const span = Math.max(max - min, 1)

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Cash flow forecast</h1>
          <p>30 / 60 / 90 day projection with confidence-style bounds from recent invoice outflow.</p>
        </div>
        <select value={horizon} onChange={(e) => setHorizon(Number(e.target.value))} style={{ borderRadius: 999, padding: '10px 14px' }}>
          <option value={30}>30 days</option>
          <option value={60}>60 days</option>
          <option value={90}>90 days</option>
        </select>
      </div>
      {error && <div className="error">{error}</div>}
      <section className="panel">
        <div className="chart-bars" style={{ height: 220 }}>
          {sample.map((p, i) => (
            <div className="bar-wrap" key={p.date}>
              <div
                className="bar"
                style={{
                  height: `${Math.max(10, ((p.predictedBalance - min) / span) * 100)}%`,
                  animationDelay: `${i * 0.04}s`,
                }}
              />
              <span>{new Date(p.date).getDate()}</span>
            </div>
          ))}
        </div>
        {data?.points?.length ? (
          <p className="muted" style={{ marginBottom: 0 }}>
            Day {horizon} balance estimate: <strong>{inr(data.points[data.points.length - 1].predictedBalance)}</strong>
            {' '}(band {inr(data.points[data.points.length - 1].lowerBound)} – {inr(data.points[data.points.length - 1].upperBound)})
          </p>
        ) : (
          <p className="muted">Loading forecast…</p>
        )}
      </section>
    </>
  )
}
