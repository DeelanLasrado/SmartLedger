import { useEffect, useRef, useState } from 'react'
import { api, inr, type Invoice } from '../api'

export function InvoicesPage() {
  const [rows, setRows] = useState<Invoice[]>([])
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const inputRef = useRef<HTMLInputElement>(null)

  async function load() {
    try {
      setRows(await api.invoices())
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load invoices')
    }
  }

  useEffect(() => {
    void load()
  }, [])

  async function onUpload(file: File) {
    setLoading(true)
    setError('')
    setMessage('')
    try {
      const result = await api.uploadInvoice(file)
      setMessage(
        `Parsed ${result.extracted.vendorName} for ${inr(result.extracted.totalAmount)}` +
          (result.anomaly.isAnomaly ? ` — flagged: ${result.anomaly.reason}` : ''),
      )
      await load()
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Upload failed')
    } finally {
      setLoading(false)
      if (inputRef.current) inputRef.current.value = ''
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Invoices</h1>
          <p>Upload a PDF or image. Azure Document Intelligence extracts fields (mock mode works without keys).</p>
        </div>
        <div>
          <input
            ref={inputRef}
            type="file"
            accept=".pdf,.png,.jpg,.jpeg,.tiff,.bmp"
            hidden
            onChange={(e) => {
              const f = e.target.files?.[0]
              if (f) void onUpload(f)
            }}
          />
          <button className="btn" disabled={loading} onClick={() => inputRef.current?.click()}>
            {loading ? 'Parsing…' : 'Upload invoice'}
          </button>
        </div>
      </div>
      {error && <div className="error">{error}</div>}
      {message && <div className="success">{message}</div>}
      <section className="panel">
        <table className="table">
          <thead>
            <tr>
              <th>Date</th>
              <th>Vendor</th>
              <th>Category</th>
              <th>Amount</th>
              <th>Status</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((r) => (
              <tr key={r.id}>
                <td>{r.invoiceDate ? new Date(r.invoiceDate).toLocaleDateString('en-IN') : '—'}</td>
                <td>{r.vendorName}</td>
                <td>{r.category}</td>
                <td>{inr(r.totalAmount)}</td>
                <td>
                  {r.isAnomaly ? <span className="badge warn">Anomaly</span> : <span className="badge">Parsed</span>}
                </td>
              </tr>
            ))}
            {!rows.length && (
              <tr>
                <td colSpan={5} className="muted">
                  No invoices yet.
                </td>
              </tr>
            )}
          </tbody>
        </table>
      </section>
    </>
  )
}
