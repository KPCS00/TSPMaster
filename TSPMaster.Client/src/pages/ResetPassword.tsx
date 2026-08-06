import { useState } from 'react'
import { Link, useNavigate, useSearchParams } from 'react-router-dom'
import { authApi } from '../api/client'
import { Lock, CheckCircle2, AlertCircle } from 'lucide-react'

export default function ResetPassword() {
  const [searchParams] = useSearchParams()
  const navigate = useNavigate()
  const email = searchParams.get('email') || ''
  const token = searchParams.get('token') || ''

  const [newPassword, setNewPassword] = useState('')
  const [confirmPassword, setConfirmPassword] = useState('')
  const [error, setError] = useState('')
  const [success, setSuccess] = useState(false)
  const [loading, setLoading] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!token || !email) {
      setError('Invalid or missing password reset token/email in URL.')
      return
    }
    if (!newPassword || !confirmPassword) {
      setError('Please fill in all fields.')
      return
    }
    if (newPassword.length < 6) {
      setError('Password must be at least 6 characters long.')
      return
    }
    if (newPassword !== confirmPassword) {
      setError('Passwords do not match.')
      return
    }

    setError('')
    setLoading(true)

    try {
      await authApi.resetPassword({ email, token, newPassword })
      setSuccess(true)
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } }; message?: string }
      setError(axiosErr.response?.data?.message ?? 'Failed to reset password. Token may be expired or invalid.')
    } finally {
      setLoading(false)
    }
  }

  if (!token || !email) {
    return (
      <div className="auth-page">
        <div className="auth-card fade-in">
          <div className="auth-logo">
            <div className="auth-logo-icon">🏦</div>
            <div>
              <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 700, fontSize: 20 }}>TSP Master</div>
              <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>Federal Investment Intelligence</div>
            </div>
          </div>
          <h1 className="auth-title">Invalid Link</h1>
          <div className="alert alert-error" style={{ marginTop: 16 }}>
            <AlertCircle size={20} /> Invalid or expired password reset link. Please request a new link.
          </div>
          <div className="auth-switch" style={{ marginTop: 24 }}>
            <Link to="/forgot-password" className="btn btn-primary btn-lg" style={{ width: '100%', justifyContent: 'center', display: 'flex', textDecoration: 'none' }}>
              Request New Reset Link
            </Link>
          </div>
        </div>
      </div>
    )
  }

  return (
    <div className="auth-page">
      <div className="auth-card fade-in">
        <div className="auth-logo">
          <div className="auth-logo-icon">🏦</div>
          <div>
            <div style={{ fontFamily: 'var(--font-heading)', fontWeight: 700, fontSize: 20 }}>TSP Master</div>
            <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', textTransform: 'uppercase', letterSpacing: '0.08em' }}>Federal Investment Intelligence</div>
          </div>
        </div>

        <h1 className="auth-title">Set new password</h1>
        <p className="auth-subtitle">Enter your new password for <strong>{email}</strong></p>

        {error && (
          <div className="alert alert-error" id="reset-password-error">
            <span>⚠️</span> {error}
          </div>
        )}

        {success ? (
          <div className="fade-in" style={{ marginTop: 16 }}>
            <div style={{ display: 'flex', alignItems: 'center', gap: 12, color: '#10b981', background: 'rgba(16, 185, 129, 0.1)', padding: '16px', borderRadius: '8px', border: '1px solid rgba(16, 185, 129, 0.2)', marginBottom: 20 }}>
              <CheckCircle2 size={24} style={{ flexShrink: 0 }} />
              <div>
                <div style={{ fontWeight: 600, color: 'var(--clr-text-main)' }}>Password Reset Successful!</div>
                <div style={{ fontSize: 13, color: 'var(--clr-text-muted)', marginTop: 4 }}>You can now sign in with your new password.</div>
              </div>
            </div>
            <button
              onClick={() => navigate('/login')}
              className="btn btn-primary btn-lg"
              style={{ width: '100%', justifyContent: 'center' }}
            >
              Sign In Now
            </button>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label" htmlFor="reset-new-password">New Password</label>
              <input
                id="reset-new-password"
                className="form-input"
                type="password"
                name="newPassword"
                value={newPassword}
                onChange={e => { setNewPassword(e.target.value); setError('') }}
                placeholder="••••••••"
                autoComplete="new-password"
                required
              />
            </div>

            <div className="form-group">
              <label className="form-label" htmlFor="reset-confirm-password">Confirm New Password</label>
              <input
                id="reset-confirm-password"
                className="form-input"
                type="password"
                name="confirmPassword"
                value={confirmPassword}
                onChange={e => { setConfirmPassword(e.target.value); setError('') }}
                placeholder="••••••••"
                autoComplete="new-password"
                required
              />
            </div>

            <button
              id="reset-submit"
              type="submit"
              className="btn btn-primary btn-lg"
              disabled={loading}
              style={{ width: '100%', marginTop: 8, justifyContent: 'center' }}
            >
              {loading ? (
                <span className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} />
              ) : (
                <>
                  <Lock size={18} /> Update Password
                </>
              )}
            </button>
          </form>
        )}

        <div className="auth-switch">
          Back to <Link to="/login">Sign in</Link>
        </div>
      </div>
    </div>
  )
}
