import { useState, type FormEvent } from 'react'
import { api } from '../api'

const prompts = [
  'What was my highest expense category last quarter?',
  'Am I eligible for GST input credit this month?',
  'Which vendor cost me the most recently?',
]

export function AskPage() {
  const [question, setQuestion] = useState(prompts[0])
  const [answer, setAnswer] = useState('')
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  async function onSubmit(e: FormEvent) {
    e.preventDefault()
    setLoading(true)
    setError('')
    try {
      const res = await api.ask(question)
      setAnswer(res.answer)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Ask failed')
    } finally {
      setLoading(false)
    }
  }

  return (
    <>
      <div className="page-head">
        <div>
          <h1>Ask AI</h1>
          <p>Plain-English answers over your transaction history using RAG.</p>
        </div>
      </div>
      <form className="panel" onSubmit={onSubmit}>
        {error && <div className="error">{error}</div>}
        <div className="field">
          <label>Question</label>
          <textarea value={question} onChange={(e) => setQuestion(e.target.value)} required />
        </div>
        <div style={{ display: 'flex', flexWrap: 'wrap', gap: 8, marginBottom: 16 }}>
          {prompts.map((p) => (
            <button key={p} type="button" className="btn ghost" onClick={() => setQuestion(p)}>
              {p}
            </button>
          ))}
        </div>
        <button className="btn" type="submit" disabled={loading}>
          {loading ? 'Thinking…' : 'Ask SmartLedger'}
        </button>
      </form>
      {answer && (
        <section className="panel" style={{ marginTop: 16 }}>
          <h2 style={{ marginTop: 0 }}>Answer</h2>
          <p style={{ whiteSpace: 'pre-wrap', marginBottom: 0 }}>{answer}</p>
        </section>
      )}
    </>
  )
}
