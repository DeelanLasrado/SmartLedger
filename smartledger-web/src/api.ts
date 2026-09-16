const TOKEN_KEY = 'sl_access'
const REFRESH_KEY = 'sl_refresh'
const EMAIL_KEY = 'sl_email'

export type AuthSession = {
  accessToken: string
  refreshToken: string
  tenantId: string
  email: string
}

function getToken() {
  return localStorage.getItem(TOKEN_KEY)
}

export function getStoredEmail() {
  return localStorage.getItem(EMAIL_KEY) ?? ''
}

export function saveSession(session: AuthSession) {
  localStorage.setItem(TOKEN_KEY, session.accessToken)
  localStorage.setItem(REFRESH_KEY, session.refreshToken)
  localStorage.setItem(EMAIL_KEY, session.email)
}

export function clearSession() {
  localStorage.removeItem(TOKEN_KEY)
  localStorage.removeItem(REFRESH_KEY)
  localStorage.removeItem(EMAIL_KEY)
}

export function isAuthenticated() {
  return Boolean(getToken())
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const headers = new Headers(init.headers)
  if (!(init.body instanceof FormData) && !headers.has('Content-Type')) {
    headers.set('Content-Type', 'application/json')
  }
  const token = getToken()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const res = await fetch(path, { ...init, headers })
  if (!res.ok) {
    let message = `Request failed (${res.status})`
    try {
      const body = await res.json()
      message = body.title || body.error || body.detail || message
      if (body.errors) message = Object.values(body.errors).flat().join(' ')
    } catch {
      /* ignore */
    }
    throw new Error(message)
  }
  if (res.status === 204) return undefined as T
  return res.json()
}

export const api = {
  login: (email: string, password: string) =>
    request<AuthSession>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    }),
  register: (payload: { businessName: string; ownerEmail: string; password: string; gstin?: string }) =>
    request<AuthSession & { tenantId: string }>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify(payload),
    }),
  summary: () => request<DashboardSummary>('/api/dashboard/summary'),
  invoices: () => request<Invoice[]>('/api/invoices'),
  uploadInvoice: (file: File) => {
    const form = new FormData()
    form.append('file', file)
    return request<InvoiceParseResult>('/api/invoices/upload', { method: 'POST', body: form })
  },
  ask: (question: string) =>
    request<{ answer: string }>('/api/financial/ask', {
      method: 'POST',
      body: JSON.stringify({ question }),
    }),
  reconcileGst: (period: string) =>
    request<GstResult>('/api/gst/reconcile', {
      method: 'POST',
      body: JSON.stringify({ period }),
    }),
  cashflow: (horizonDays: number) =>
    request<CashFlowResult>(`/api/cashflow/forecast?horizonDays=${horizonDays}`),
}

export type DashboardSummary = {
  businessName?: string
  tier?: string
  invoicesUsedThisMonth: number
  monthlyInvoiceLimit?: number
  invoiceCount: number
  monthSpend: number
  anomalyCount: number
  categoryBreakdown: { category: string; total: number; count: number }[]
}

export type Invoice = {
  id: string
  vendorName: string
  vendorGstin?: string
  invoiceNumber?: string
  invoiceDate?: string
  totalAmount: number
  category: string
  status: number
  isAnomaly: boolean
  anomalyReason?: string
}

export type InvoiceParseResult = {
  invoiceId: string
  extracted: { vendorName: string; totalAmount: number; category: string }
  anomaly: { isAnomaly: boolean; reason: string }
}

export type GstResult = {
  period: string
  matchedCount: number
  mismatchCount: number
  netTaxLiability: number
  mismatches: { invoiceNumber: string; reason: string; gstr2AAmount: number; gstr3BAmount: number }[]
}

export type CashFlowResult = {
  horizonDays: number
  points: { date: string; predictedBalance: number; lowerBound: number; upperBound: number }[]
}

export function inr(n: number) {
  return new Intl.NumberFormat('en-IN', {
    style: 'currency',
    currency: 'INR',
    maximumFractionDigits: 0,
  }).format(n)
}
