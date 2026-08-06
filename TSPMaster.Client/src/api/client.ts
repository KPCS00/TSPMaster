import axios from 'axios'

const api = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL || '/api',
  headers: { 'Content-Type': 'application/json' },
})

// Inject JWT token on every request
api.interceptors.request.use(config => {
  const token = localStorage.getItem('tsp_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

// Auto-logout on 401
api.interceptors.response.use(
  res => res,
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('tsp_token')
      localStorage.removeItem('tsp_user')
      if (window.location.pathname !== '/login' && window.location.pathname !== '/register') {
        window.location.href = '/login'
      }
    }
    return Promise.reject(err)
  }
)

// ─── Auth ────────────────────────────────────────────────────────
export const authApi = {
  register: (data: { firstName: string; lastName: string; email: string; password: string }) =>
    api.post('/auth/register', data).then(r => r.data),

  login: (data: { email: string; password: string }) =>
    api.post('/auth/login', data).then(r => r.data),

  forgotPassword: (email: string) =>
    api.post('/auth/forgot-password', { email }).then(r => r.data),

  resetPassword: (data: { email: string; token: string; newPassword: string }) =>
    api.post('/auth/reset-password', data).then(r => r.data),
}


// ─── Funds ───────────────────────────────────────────────────────
export const fundsApi = {
  getLatest: () => api.get('/funds/latest').then(r => r.data),
  getNames: () => api.get('/funds/names').then(r => r.data),
  getFundHistory: (fundName: string, from?: string, to?: string) =>
    api.get(`/funds/${encodeURIComponent(fundName)}/history`, { params: { from, to } }).then(r => r.data),
  getAllHistory: (from?: string, to?: string) =>
    api.get('/funds/history', { params: { from, to } }).then(r => r.data),
  sync: () => api.post('/funds/sync').then(r => r.data),
}

// ─── Allocations ─────────────────────────────────────────────────
export const allocationsApi = {
  get: () => api.get('/allocations').then(r => r.data),
  getStatus: () => api.get('/allocations/status').then(r => r.data),
  getOverview: () => api.get('/allocations/overview').then(r => r.data),
  setInitialBalance: (balance: number, effectiveDate?: string) =>
    api.post('/allocations/initial-balance', { balance, effectiveDate }).then(r => r.data),
  recordMove: (data: { effectiveDate: string; description?: string; allocations: { fundName: string; percentage: number }[]; updatedBalance?: number }) =>
    api.post('/allocations/move', data).then(r => r.data),
  getHistory: () => api.get('/allocations/history').then(r => r.data),
  deleteMove: (id: number) => api.delete(`/allocations/move/${id}`).then(r => r.data),
  set: (allocations: { fundName: string; percentage: number }[]) =>
    api.put('/allocations', { allocations }).then(r => r.data),
}

// ─── Users ───────────────────────────────────────────────────────
export const usersApi = {
  getProfile: () => api.get('/users/me').then(r => r.data),
  getPerformance: (days?: number) =>
    api.get('/users/performance', { params: { days } }).then(r => r.data),
}

// ─── Analysis ────────────────────────────────────────────────────
export const analysisApi = {
  getRecommendation: () => api.get('/analysis/recommendation').then(r => r.data),
  refresh: () => api.post('/analysis/refresh').then(r => r.data),
}

export default api
