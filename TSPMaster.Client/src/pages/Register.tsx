import { useState } from 'react'
import { Link, useNavigate } from 'react-router-dom'
import { GoogleLogin, CredentialResponse } from '@react-oauth/google'
import { useAuth } from '../context/AuthContext'
import { authApi } from '../api/client'
import { UserPlus } from 'lucide-react'

export default function Register() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', password: '' })
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(false)

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setForm(prev => ({ ...prev, [e.target.name]: e.target.value }))
    setError('')
  }

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault()
    const hasMinLen = form.password.length >= 8
    const hasUpper = /[A-Z]/.test(form.password)
    const hasDigit = /[0-9]/.test(form.password)

    if (!hasMinLen || !hasUpper || !hasDigit) {
      setError('Password must be at least 8 characters with a number and uppercase letter.')
      return
    }
    setLoading(true)
    try {
      const data = await authApi.register(form)
      login(data)
      navigate('/')
    } catch (err: unknown) {
      const respData = (err as { response?: { data?: any } })?.response?.data
      let msgs = 'Registration failed. Please try again.'
      if (respData) {
        if (respData.errors && typeof respData.errors === 'object') {
          const joined = Object.values(respData.errors).flat().join(' ')
          if (joined) msgs = joined
        } else if (typeof respData.message === 'string') {
          msgs = respData.message
        } else if (typeof respData.detail === 'string') {
          msgs = respData.detail
        } else if (typeof respData.title === 'string') {
          msgs = respData.title
        } else if (typeof respData === 'string') {
          msgs = respData
        }
      }
      setError(msgs)
    } finally {
      setLoading(false)
    }
  }

  const handleGoogleSuccess = async (credentialResponse: CredentialResponse) => {
    if (!credentialResponse.credential) {
      setError('Google sign-up failed: no credential received.')
      return
    }
    setLoading(true)
    setError('')
    try {
      const data = await authApi.googleLogin(credentialResponse.credential)
      login(data)
      navigate('/')
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Google sign-up failed. Please verify server setup.')
    } finally {
      setLoading(false)
    }
  }

  const handleGoogleError = () => {
    setError('Google sign-up was canceled or failed.')
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

        <h1 className="auth-title">Create your account</h1>
        <p className="auth-subtitle">Start tracking your TSP investments with AI insights</p>

        {error && (
          <div className="alert alert-error">
            <span>⚠️</span> {error}
          </div>
        )}

        <div className="google-btn-wrapper">
          <GoogleLogin
            onSuccess={handleGoogleSuccess}
            onError={handleGoogleError}
            useOneTap
            theme="filled_blue"
            shape="rectangular"
            text="signup_with"
            size="large"
          />
        </div>

        <div className="auth-divider">
          <span>or sign up with email</span>
        </div>

        <form onSubmit={handleSubmit}>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 'var(--space-md)' }}>
            <div className="form-group">
              <label className="form-label" htmlFor="reg-firstName">First Name</label>
              <input id="reg-firstName" className="form-input" type="text" name="firstName" value={form.firstName}
                onChange={handleChange} placeholder="John" required />
            </div>
            <div className="form-group">
              <label className="form-label" htmlFor="reg-lastName">Last Name</label>
              <input id="reg-lastName" className="form-input" type="text" name="lastName" value={form.lastName}
                onChange={handleChange} placeholder="Smith" required />
            </div>
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="reg-email">Email address</label>
            <input id="reg-email" className="form-input" type="email" name="email" value={form.email}
              onChange={handleChange} placeholder="you@agency.gov" required />
          </div>

          <div className="form-group">
            <label className="form-label" htmlFor="reg-password">Password</label>
            <input id="reg-password" className="form-input" type="password" name="password" value={form.password}
              onChange={handleChange} placeholder="Min 8 chars, 1 uppercase, 1 number" required />
          </div>

          <button
            id="register-submit"
            type="submit"
            className="btn btn-primary btn-lg"
            disabled={loading}
            style={{ width: '100%', marginTop: 8, justifyContent: 'center' }}
          >
            {loading
              ? <span className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} />
              : <><UserPlus size={18} /> Create Account</>
            }
          </button>
        </form>

        <div className="auth-switch">
          Already have an account? <Link to="/login">Sign in</Link>
        </div>
      </div>
    </div>
  )
}

