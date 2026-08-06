import { useState } from 'react'
import { Link } from 'react-router-dom'
import { authApi } from '../api/client'
import { KeyRound, ArrowLeft, CheckCircle2 } from 'lucide-react'

export default function ForgotPassword() {
  const [email, setEmail] = useState('')
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [loading, setLoading] = useState(false)
  const [submitted, setSubmitted] = useState(false)

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    if (!email) {
      setError('Please enter your email address.')
      return
    }
    setError('')
    setMessage('')
    setLoading(true)

    try {
      const res = await authApi.forgotPassword(email)
      setMessage(res.message || 'If an account with that email exists, a password reset link has been sent.')
      setSubmitted(true)
    } catch (err: unknown) {
      const axiosErr = err as { response?: { data?: { message?: string } }; message?: string }
      setError(axiosErr.response?.data?.message ?? 'Failed to process request. Please try again.')
    } finally {
      setLoading(false)
    }
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

        <h1 className="auth-title">Reset password</h1>
        <p className="auth-subtitle">Enter your registered email address and we'll send you instructions to reset your password.</p>

        {error && (
          <div className="alert alert-error" id="forgot-password-error">
            <span>⚠️</span> {error}
          </div>
        )}

        {submitted ? (
          <div className="fade-in" style={{ textAlign: 'center', marginTop: 16 }}>

            <div style={{ display: 'flex', alignItems: 'center', gap: 8, color: '#10b981', background: 'rgba(16, 185, 129, 0.1)', padding: '16px', borderRadius: '8px', border: '1px solid rgba(16, 185, 129, 0.2)', marginBottom: 20 }}>
              <CheckCircle2 size={24} style={{ flexShrink: 0 }} />
              <div style={{ fontSize: 14, color: 'var(--clr-text-main)' }}>
                {message}
              </div>
            </div>
            <p style={{ fontSize: 13, color: 'var(--clr-text-muted)', marginBottom: 20 }}>
              Please check your inbox (and spam folder) for the reset link.
            </p>
            <Link to="/login" className="btn btn-primary btn-lg" style={{ width: '100%', justifyContent: 'center', display: 'flex', textDecoration: 'none' }}>
              <ArrowLeft size={18} /> Back to Sign In
            </Link>
          </div>
        ) : (
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label className="form-label" htmlFor="forgot-email">Email address</label>
              <input
                id="forgot-email"
                className="form-input"
                type="email"
                name="email"
                value={email}
                onChange={e => { setEmail(e.target.value); setError('') }}
                placeholder="you@agency.gov"
                autoComplete="email"
                required
              />
            </div>

            <button
              id="forgot-submit"
              type="submit"
              className="btn btn-primary btn-lg"
              disabled={loading}
              style={{ width: '100%', marginTop: 8, justifyContent: 'center' }}
            >
              {loading ? (
                <span className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} />
              ) : (
                <>
                  <KeyRound size={18} /> Send Reset Link
                </>
              )}
            </button>
          </form>
        )}

        <div className="auth-switch">
          Remember your password? <Link to="/login">Sign in</Link>
        </div>
      </div>
    </div>
  )
}
