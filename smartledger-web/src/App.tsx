import { Navigate, Outlet, Route, Routes } from 'react-router-dom'
import { AppLayout } from './AppLayout'
import { isAuthenticated } from './api'
import { AskPage } from './pages/AskPage'
import { CashFlowPage } from './pages/CashFlowPage'
import { DashboardPage } from './pages/DashboardPage'
import { GstPage } from './pages/GstPage'
import { InvoicesPage } from './pages/InvoicesPage'
import { LoginPage } from './pages/LoginPage'
import { RegisterPage } from './pages/RegisterPage'

function RequireAuth() {
  if (!isAuthenticated()) return <Navigate to="/login" replace />
  return <Outlet />
}

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route path="/register" element={<RegisterPage />} />
      <Route element={<RequireAuth />}>
        <Route element={<AppLayout />}>
          <Route index element={<DashboardPage />} />
          <Route path="invoices" element={<InvoicesPage />} />
          <Route path="ask" element={<AskPage />} />
          <Route path="gst" element={<GstPage />} />
          <Route path="cashflow" element={<CashFlowPage />} />
        </Route>
      </Route>
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
