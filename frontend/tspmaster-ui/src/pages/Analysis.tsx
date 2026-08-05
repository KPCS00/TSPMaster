import { useEffect, useState } from 'react'
import { analysisApi } from '../api/client'
import ReactMarkdown from 'react-markdown'
import { Sparkles, RefreshCw, TrendingUp, TrendingDown, Minus } from 'lucide-react'

interface FundScore {
  fundName: string
  score: number
  trend: string
  momentumScore: number
  volatilityScore: number
  recommendation: string
}

interface AnalysisResult {
  id: number
  generatedAt: string
  periodDescription: string
  topRecommendation: string
  recommendationText: string
  fundScores: FundScore[]
  marketContext: string
}

const FUND_COLORS: Record<string, string> = {
  'G Fund': '#10b981', 'F Fund': '#8b5cf6', 'C Fund': '#3b82f6', 'S Fund': '#f59e0b', 'I Fund': '#ef4444',
}

const TREND_ICON: Record<string, React.ReactNode> = {
  'Uptrend': <TrendingUp size={14} style={{ color: '#10b981' }} />,
  'Downtrend': <TrendingDown size={14} style={{ color: '#ef4444' }} />,
  'Sideways': <Minus size={14} style={{ color: '#94a3b8' }} />,
}

export default function Analysis() {
  const [result, setResult] = useState<AnalysisResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [refreshing, setRefreshing] = useState(false)
  const [error, setError] = useState('')
  const [cooldownMsg, setCooldownMsg] = useState('')

  useEffect(() => {
    const load = async () => {
      try {
        const data = await analysisApi.getRecommendation()
        if (data) setResult(data)
      } catch (e) { console.error(e) }
      finally { setLoading(false) }
    }
    load()
  }, [])

  const handleRefresh = async () => {
    setRefreshing(true)
    setError('')
    setCooldownMsg('')
    try {
      const data = await analysisApi.refresh()
      setResult(data)
    } catch (err: unknown) {
      const status = (err as { response?: { status?: number; data?: { message?: string } } })?.response
      if (status?.status === 429) {
        setCooldownMsg(status.data?.message ?? 'Analysis throttled. Try again later.')
      } else {
        setError('Failed to generate analysis. Check your Gemini API key in appsettings.json.')
      }
    } finally { setRefreshing(false) }
  }

  return (
    <div className="fade-in">
      <div className="page-header" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div>
          <h1 className="page-title"><Sparkles size={26} style={{ display: 'inline', marginRight: 10, color: 'var(--clr-primary)' }} />AI Analysis</h1>
          <p className="page-subtitle">Powered by Google Gemini + 6-month statistical fund analysis</p>
        </div>
        <button
          id="refresh-analysis"
          className="btn btn-primary"
          onClick={handleRefresh}
          disabled={refreshing}
        >
          {refreshing
            ? <><span className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> Analyzing…</>
            : <><RefreshCw size={16} /> Refresh Analysis</>}
        </button>
      </div>

      {error && <div className="alert alert-error mb-lg">⚠️ {error}</div>}
      {cooldownMsg && <div className="alert alert-warning mb-lg">⏳ {cooldownMsg}</div>}

      {loading ? (
        <div className="loading-container">
          <div className="spinner" />
          <span style={{ color: 'var(--clr-text-muted)' }}>Loading analysis…</span>
        </div>
      ) : !result ? (
        <div className="card" style={{ textAlign: 'center', padding: 'var(--space-3xl)' }}>
          <div style={{ fontSize: 48, marginBottom: 'var(--space-md)' }}>🤖</div>
          <div className="card-title mb-md">No Analysis Yet</div>
          <p style={{ color: 'var(--clr-text-muted)', marginBottom: 'var(--space-lg)' }}>
            Click "Refresh Analysis" to generate your first AI-powered investment recommendation.
            Requires a Gemini API key in <code>appsettings.json</code>.
          </p>
          <button className="btn btn-primary" onClick={handleRefresh} disabled={refreshing}>
            <Sparkles size={16} /> Generate Analysis
          </button>
        </div>
      ) : (
        <>
          {/* Top Recommendation Banner */}
          <div className="recommendation-banner" style={{ marginBottom: 'var(--space-xl)' }}>
            <div style={{ fontSize: 40 }}>🏆</div>
            <div className="recommendation-text">
              <div className="recommendation-title" style={{ fontSize: 22 }}>
                Top Pick: <span style={{ color: FUND_COLORS[result.topRecommendation] ?? 'var(--clr-accent)' }}>
                  {result.topRecommendation}
                </span>
              </div>
              <div className="recommendation-summary">
                Analysis period: {result.periodDescription} &nbsp;·&nbsp;
                Generated {new Date(result.generatedAt).toLocaleString()}
              </div>
            </div>
            <div className="recommendation-badge pulse-glow">AI PICK</div>
          </div>

          {/* Fund Scores */}
          {result.fundScores.length > 0 && (
            <div className="card mb-lg">
              <div className="card-title mb-md">Fund Scores</div>
              <div className="fund-score-grid">
                {result.fundScores.map(fs => {
                  const isTop = fs.fundName === result.topRecommendation
                  const color = FUND_COLORS[fs.fundName] ?? 'var(--clr-primary)'
                  const positive = fs.momentumScore >= 0
                  return (
                    <div key={fs.fundName} className={`fund-score-card${isTop ? ' top-pick' : ''}`}>
                      {isTop && <div style={{ fontSize: 11, color: 'var(--clr-primary)', fontWeight: 700, marginBottom: 4 }}>⭐ TOP PICK</div>}
                      <div className="fund-score-name" style={{ color }}>{fs.fundName}</div>
                      <div className="fund-score-trend">
                        {TREND_ICON[fs.trend]} {fs.trend}
                      </div>
                      <div className="fund-score-value" style={{ color: positive ? '#10b981' : '#ef4444' }}>
                        {positive ? '+' : ''}{fs.momentumScore.toFixed(1)}%
                      </div>
                      <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', marginTop: 4 }}>
                        6-mo return
                      </div>
                      <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', marginTop: 2 }}>
                        Vol: {fs.volatilityScore.toFixed(2)}%
                      </div>
                      {fs.recommendation && (
                        <div style={{
                          marginTop: 8, padding: '2px 8px', borderRadius: 'var(--radius-full)',
                          fontSize: 10, fontWeight: 700,
                          background: isTop ? 'rgba(59,130,246,0.2)' : 'rgba(255,255,255,0.05)',
                          color: isTop ? 'var(--clr-primary)' : 'var(--clr-text-muted)'
                        }}>
                          {fs.recommendation}
                        </div>
                      )}
                    </div>
                  )
                })}
              </div>
            </div>
          )}

          {/* Full Recommendation Markdown */}
          <div className="card">
            <div className="card-title mb-md">📋 Full Recommendation</div>
            <div className="markdown-body">
              <ReactMarkdown>{result.recommendationText}</ReactMarkdown>
            </div>
          </div>
        </>
      )}
    </div>
  )
}
