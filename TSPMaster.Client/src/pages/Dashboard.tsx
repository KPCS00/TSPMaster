import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import { fundsApi, analysisApi } from '../api/client'
import { useAuth } from '../context/AuthContext'
import { TrendingUp, TrendingDown, Sparkles, ArrowRight } from 'lucide-react'
import { SparklineChart } from '../components/SparklineChart'

interface FundLatest {
  fundName: string
  price: number
  date: string
  changeAmount: number | null
  changePercent: number | null
  description: string | null
}

interface AnalysisResult {
  topRecommendation: string
  recommendationText: string
  generatedAt: string
}

const FUND_COLORS: Record<string, string> = {
  'G Fund': '#10b981', 'F Fund': '#8b5cf6', 'C Fund': '#3b82f6',
  'S Fund': '#f59e0b', 'I Fund': '#ef4444',
}

const CORE_FUNDS = ['G Fund', 'F Fund', 'C Fund', 'S Fund', 'I Fund']

export default function Dashboard() {
  const { user } = useAuth()
  const [funds, setFunds] = useState<FundLatest[]>([])
  const [analysis, setAnalysis] = useState<AnalysisResult | null>(null)
  const [history, setHistory] = useState<Record<string, { date: string; price: number }[]>>({})
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const load = async () => {
      try {
        const [fundsData, analysisData] = await Promise.allSettled([
          fundsApi.getLatest(),
          analysisApi.getRecommendation(),
        ])

        if (fundsData.status === 'fulfilled') {
          setFunds(fundsData.value)
        }
        if (analysisData.status === 'fulfilled' && analysisData.value) {
          setAnalysis(analysisData.value)
        }

        // Load 30-day history for sparklines
        const to = new Date()
        const from = new Date()
        from.setDate(from.getDate() - 30)
        const hist = await fundsApi.getAllHistory(from.toISOString().split('T')[0], to.toISOString().split('T')[0])
        const simplified: Record<string, { date: string; price: number }[]> = {}
        for (const [k, v] of Object.entries(hist as Record<string, { date: string; price: number }[]>)) {
          simplified[k] = v
        }
        setHistory(simplified)
      } catch (e) {
        console.error(e)
      } finally {
        setLoading(false)
      }
    }
    load()
  }, [])

  const coreFunds = funds.filter(f => CORE_FUNDS.includes(f.fundName))

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">
          Welcome back, {user?.firstName} 👋
        </h1>
        <p className="page-subtitle">Here's your TSP portfolio overview for today</p>
      </div>

      {/* AI Recommendation Banner */}
      {analysis && (
        <div className="recommendation-banner">
          <div className="recommendation-badge">🤖 AI Pick</div>
          <div className="recommendation-text">
            <div className="recommendation-title">
              Top Recommendation: <span style={{ color: 'var(--clr-accent)' }}>{analysis.topRecommendation}</span>
            </div>
            <div className="recommendation-summary">
              Updated {new Date(analysis.generatedAt).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
            </div>
          </div>
          <Link to="/analysis" className="btn btn-secondary btn-sm">
            Full Analysis <ArrowRight size={14} />
          </Link>
        </div>
      )}

      {/* Core Fund Stat Cards */}
      <div className="stat-grid">
        {loading
          ? Array.from({ length: 5 }).map((_, i) => (
              <div key={i} className="stat-card" style={{ minHeight: 120, opacity: 0.4 }} />
            ))
          : coreFunds.map(fund => {
              const color = FUND_COLORS[fund.fundName] ?? 'var(--clr-primary)'
              const positive = (fund.changePercent ?? 0) >= 0
              const sparkData = history[fund.fundName] ?? []

              return (
                <div
                  key={fund.fundName}
                  className="stat-card"
                  style={{ '--fund-color': color } as React.CSSProperties}
                >
                  <div className="stat-label" style={{ display: 'flex', justifyContent: 'space-between' }}>
                    <span>{fund.fundName}</span>
                    <span style={{ color }}>●</span>
                  </div>

                  <div className="stat-value">${fund.price.toFixed(4)}</div>

                  {fund.changePercent !== null && (
                    <div className={`stat-change ${positive ? 'positive' : 'negative'}`}>
                      {positive ? <TrendingUp size={12} /> : <TrendingDown size={12} />}
                      {positive ? '+' : ''}{fund.changePercent.toFixed(2)}%
                    </div>
                  )}

                  {sparkData.length > 0 && (
                    <div style={{ marginTop: 12, height: 40 }}>
                      <SparklineChart data={sparkData} color={color} positive={positive} />
                    </div>
                  )}
                </div>
              )
            })
        }
      </div>

      {/* Quick Links */}
      <div className="grid-2">
        <div className="card">
          <div className="card-header">
            <div>
              <div className="card-title">📊 Fund Performance</div>
              <div className="card-subtitle">Historical price charts for all TSP funds</div>
            </div>
            <Link to="/funds" className="btn btn-secondary btn-sm">
              View <ArrowRight size={14} />
            </Link>
          </div>
          <p style={{ color: 'var(--clr-text-muted)', fontSize: 13 }}>
            Track daily closing prices for G, F, C, S, I, and all Lifecycle funds.
            Data sourced nightly from <strong>tsp.gov</strong>.
          </p>
        </div>

        <div className="card">
          <div className="card-header">
            <div>
              <div className="card-title"><Sparkles size={16} style={{ display: 'inline', marginRight: 6 }} />AI Analysis</div>
              <div className="card-subtitle">Gemini-powered investment recommendations</div>
            </div>
            <Link to="/analysis" className="btn btn-secondary btn-sm">
              View <ArrowRight size={14} />
            </Link>
          </div>
          <p style={{ color: 'var(--clr-text-muted)', fontSize: 13 }}>
            AI analysis based on 6-month price trends, momentum, volatility, and market context.
            {!analysis && (
              <span style={{ color: 'var(--clr-warning)' }}>
                {' '}No analysis yet — <Link to="/analysis">generate one</Link>.
              </span>
            )}
          </p>
        </div>
      </div>

      {/* L Fund quick view */}
      {!loading && funds.filter(f => f.fundName.startsWith('L')).length > 0 && (
        <div className="card" style={{ marginTop: 'var(--space-lg)' }}>
          <div className="card-header">
            <div className="card-title">Lifecycle (L) Funds</div>
            <Link to="/funds" className="btn btn-secondary btn-sm">All Prices</Link>
          </div>
          <table className="data-table">
            <thead>
              <tr>
                <th>Fund</th>
                <th>Latest Price</th>
                <th>Change</th>
                <th>Change %</th>
              </tr>
            </thead>
            <tbody>
              {funds.filter(f => f.fundName.startsWith('L')).map(f => {
                const pos = (f.changePercent ?? 0) >= 0
                return (
                  <tr key={f.fundName}>
                    <td><strong>{f.fundName}</strong></td>
                    <td style={{ fontFamily: 'var(--font-heading)', fontWeight: 600 }}>${f.price.toFixed(4)}</td>
                    <td className={pos ? 'text-success' : 'text-danger'}>
                      {f.changeAmount !== null ? `${pos ? '+' : ''}$${f.changeAmount.toFixed(4)}` : '—'}
                    </td>
                    <td className={pos ? 'text-success' : 'text-danger'}>
                      {f.changePercent !== null ? `${pos ? '+' : ''}${f.changePercent.toFixed(2)}%` : '—'}
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
