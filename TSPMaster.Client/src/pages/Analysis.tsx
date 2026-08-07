import { useEffect, useState } from 'react'
import { analysisApi, allocationsApi } from '../api/client'
import ReactMarkdown from 'react-markdown'
import { Sparkles, RefreshCw, TrendingUp, TrendingDown, Minus, ShieldAlert, ArrowRight, CheckCircle2 } from 'lucide-react'
import { useNavigate } from 'react-router-dom'

interface FundScore {
  fundName: string
  score: number
  trend: string
  momentumScore: number
  volatilityScore: number
  recommendation: string
}

interface MovePlan {
  moveNumber: number
  title: string
  triggerCondition: string
  targetAllocation: Record<string, number>
  rationale: string
}

interface ScheduledMove {
  moveNumber: number
  dateString: string
  tradingDay: number
  targetAllocation: Record<string, number>
  seasonalRationale: string
  aiStatusBadge: string
}

interface DailyCalendarEntry {
  dateString: string
  dayOfMonth: number
  tradingDay: number
  recommendedFund: string
  isMoveDay: boolean
  moveNumber?: number
}

interface AnalysisResult {
  id: number
  generatedAt: string
  periodDescription: string
  topRecommendation: string
  recommendationText: string
  fundScores: FundScore[]
  marketContext: string
  targetMonth: string
  move1Plan: MovePlan
  move2Plan: MovePlan
  move3Plan: MovePlan
  macroNewsSummary: string
  scheduledMoves: ScheduledMove[]
  dailyCalendar: DailyCalendarEntry[]
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
  const [applySuccess, setApplySuccess] = useState('')
  const [applying, setApplying] = useState(false)
  const navigate = useNavigate()

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
    setApplySuccess('')
    try {
      const data = await analysisApi.refresh()
      setResult(data)
    } catch (err: unknown) {
      const resp = (err as { response?: { status?: number; data?: { message?: string; detail?: string; error?: string } } })?.response
      if (resp?.status === 429) {
        setCooldownMsg(resp.data?.message ?? 'Analysis throttled. Try again later.')
      } else {
        const msg = resp?.data?.message || resp?.data?.detail || resp?.data?.error || 'Failed to generate analysis. Check API configuration.'
        setError(msg)
      }
    } finally { setRefreshing(false) }
  }

  const handleApplyAllocation = async (targetAllocation: Record<string, number>) => {
    setApplying(true)
    setError('')
    setApplySuccess('')
    try {
      const items = Object.entries(targetAllocation).map(([fundName, percentage]) => ({ fundName, percentage }))
      await allocationsApi.set(items)
      setApplySuccess('Successfully applied strategy allocation to your account!')
      setTimeout(() => navigate('/allocations'), 1500)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Failed to apply allocation.')
    } finally { setApplying(false) }
  }

  return (
    <div className="fade-in">
      <div className="page-header" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div>
          <h1 className="page-title"><Sparkles size={26} style={{ display: 'inline', marginRight: 10, color: 'var(--clr-primary)' }} />Monthly AI Strategy</h1>
          <p className="page-subtitle">TSPCalc Seasonal Strategy Dates + Macro Financial News + TSP 3-Move Rules</p>
        </div>
        <button
          id="refresh-analysis"
          className="btn btn-primary"
          onClick={handleRefresh}
          disabled={refreshing}
        >
          {refreshing
            ? <><span className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> Analyzing…</>
            : <><RefreshCw size={16} /> Refresh Monthly Strategy</>}
        </button>
      </div>

      {error && <div className="alert alert-error mb-lg">⚠️ {error}</div>}
      {cooldownMsg && <div className="alert alert-warning mb-lg">⏳ {cooldownMsg}</div>}
      {applySuccess && <div className="alert alert-success mb-lg"><CheckCircle2 size={16} style={{ marginRight: 6 }} /> {applySuccess}</div>}

      {loading ? (
        <div className="loading-container">
          <div className="spinner" />
          <span style={{ color: 'var(--clr-text-muted)' }}>Generating monthly projection…</span>
        </div>
      ) : !result ? (
        <div className="card" style={{ textAlign: 'center', padding: 'var(--space-3xl)' }}>
          <div style={{ fontSize: 48, marginBottom: 'var(--space-md)' }}>🤖</div>
          <div className="card-title mb-md">No Monthly Strategy Generated</div>
          <p style={{ color: 'var(--clr-text-muted)', marginBottom: 'var(--space-lg)' }}>
            Click "Refresh Monthly Strategy" to project the best performing funds and specific transfer dates for this month.
          </p>
          <button className="btn btn-primary" onClick={handleRefresh} disabled={refreshing}>
            <Sparkles size={16} /> Generate Monthly Strategy
          </button>
        </div>
      ) : (
        <>
          {/* Top Target Month & Recommendation Banner */}
          <div className="recommendation-banner" style={{ marginBottom: 'var(--space-xl)' }}>
            <div style={{ fontSize: 40 }}>🎯</div>
            <div className="recommendation-text">
              <div className="recommendation-title" style={{ fontSize: 22 }}>
                {result.targetMonth || 'Current Month'} Strategy Pick: <span style={{ color: FUND_COLORS[result.topRecommendation] ?? 'var(--clr-accent)' }}>
                  {result.topRecommendation}
                </span>
              </div>
              <div className="recommendation-summary">
                {result.periodDescription} &nbsp;·&nbsp;
                Generated {new Date(result.generatedAt).toLocaleString()}
              </div>
            </div>
            <div className="recommendation-badge pulse-glow">SEASONAL AI PLAN</div>
          </div>

          {/* Scheduled Transfer Dates Timeline Card */}
          <div className="card mb-lg" style={{ borderLeft: '4px solid #3b82f6' }}>
            <div className="card-title mb-xs" style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 18 }}>
              📅 Exact Scheduled Transfer Dates for {result.targetMonth} (TSPCalc Model)
            </div>
            <p style={{ color: 'var(--clr-text-muted)', fontSize: 13, marginBottom: 'var(--space-lg)' }}>
              Specific calendar dates for your 3 allowed monthly Interfund Transfers (IFT), confirmed against current macro news events.
            </p>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 'var(--space-md)' }}>
              {result.scheduledMoves?.map(move => (
                <div key={move.moveNumber} style={{
                  background: 'var(--clr-surface-2)',
                  border: move.moveNumber === 3 ? '1px solid #10b981' : '1px solid var(--clr-border)',
                  borderRadius: 'var(--radius-lg)',
                  padding: 'var(--space-md)',
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'space-between'
                }}>
                  <div>
                    <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 6 }}>
                      <span style={{
                        background: move.moveNumber === 1 ? '#3b82f6' : move.moveNumber === 2 ? '#f59e0b' : '#10b981',
                        color: '#fff', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700
                      }}>
                        MOVE {move.moveNumber}
                      </span>
                      <span style={{ fontSize: 11, background: 'rgba(255,255,255,0.08)', padding: '2px 6px', borderRadius: 4, color: 'var(--clr-primary)', fontWeight: 700 }}>
                        {move.aiStatusBadge}
                      </span>
                    </div>

                    <div style={{ fontSize: 18, fontWeight: 800, color: 'var(--clr-text)', margin: '4px 0' }}>
                      📆 {move.dateString} <span style={{ fontSize: 12, color: 'var(--clr-text-muted)', fontWeight: 400 }}>(Trading Day {move.tradingDay})</span>
                    </div>

                    <div style={{ fontSize: 13, color: 'var(--clr-text-muted)', marginBottom: 8 }}>
                      {move.seasonalRationale}
                    </div>

                    <div style={{ background: 'rgba(0,0,0,0.2)', padding: '8px 10px', borderRadius: 6, marginBottom: 8 }}>
                      <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', fontWeight: 700, marginBottom: 2 }}>Target Position:</div>
                      <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap' }}>
                        {Object.entries(move.targetAllocation).map(([fund, pct]) => (
                          <span key={fund} style={{ fontSize: 13, fontWeight: 700, color: FUND_COLORS[fund] ?? '#fff' }}>
                            {fund}: {pct}%
                          </span>
                        ))}
                      </div>
                    </div>
                  </div>

                  <button
                    className={`btn ${move.moveNumber === 1 ? 'btn-primary' : 'btn-secondary'}`}
                    style={{ width: '100%', marginTop: 6 }}
                    onClick={() => handleApplyAllocation(move.targetAllocation)}
                    disabled={applying}
                  >
                    Apply Move {move.moveNumber} Allocation <ArrowRight size={14} />
                  </button>
                </div>
              ))}
            </div>
          </div>

          {/* Monthly Day-by-Day Fund Calendar Grid */}
          <div className="card mb-lg">
            <div className="card-title mb-xs">🗓️ Monthly Day-by-Day Fund Calendar ({result.targetMonth})</div>
            <p style={{ color: 'var(--clr-text-muted)', fontSize: 13, marginBottom: 'var(--space-lg)' }}>
              TSPCalc-style daily strategy map detailing which fund to hold for every calendar day of the month.
            </p>

            <div style={{
              display: 'grid',
              gridTemplateColumns: 'repeat(auto-fill, minmax(130px, 1fr))',
              gap: 8,
              maxHeight: 400,
              overflowY: 'auto',
              paddingRight: 4
            }}>
              {result.dailyCalendar?.map(entry => (
                <div key={entry.dateString} style={{
                  background: entry.isMoveDay ? 'rgba(59, 130, 246, 0.15)' : 'var(--clr-surface-2)',
                  border: entry.isMoveDay ? '2px solid var(--clr-primary)' : '1px solid var(--clr-border)',
                  borderRadius: 8,
                  padding: 8,
                  textAlign: 'center',
                  position: 'relative'
                }}>
                  {entry.isMoveDay && (
                    <span style={{
                      position: 'absolute', top: 2, right: 4, fontSize: 9, fontWeight: 800,
                      color: entry.moveNumber === 3 ? '#10b981' : 'var(--clr-primary)'
                    }}>
                      IFT #{entry.moveNumber}
                    </span>
                  )}
                  <div style={{ fontSize: 12, fontWeight: 700, color: 'var(--clr-text)' }}>
                    Day {entry.dayOfMonth}
                  </div>
                  <div style={{ fontSize: 10, color: 'var(--clr-text-muted)', marginBottom: 4 }}>
                    {entry.tradingDay > 0 ? `TD ${entry.tradingDay}` : 'Weekend'}
                  </div>
                  <div style={{
                    fontSize: 11,
                    fontWeight: 700,
                    padding: '2px 4px',
                    borderRadius: 4,
                    background: entry.recommendedFund.includes('G Fund') ? 'rgba(16,185,129,0.2)' : 'rgba(59,130,246,0.2)',
                    color: entry.recommendedFund.includes('G Fund') ? '#10b981' : '#3b82f6'
                  }}>
                    {entry.recommendedFund}
                  </div>
                </div>
              ))}
            </div>
          </div>

          {/* Macro & Financial News Drivers Summary */}
          {result.macroNewsSummary && (
            <div className="card mb-lg" style={{ borderLeft: '4px solid var(--clr-primary)' }}>
              <div className="card-title mb-xs" style={{ display: 'flex', alignItems: 'center', gap: 8, fontSize: 16 }}>
                📰 Macroeconomic & Political News Drivers
              </div>
              <p style={{ color: 'var(--clr-text-muted)', fontSize: 14, margin: 0, lineHeight: 1.5 }}>
                {result.macroNewsSummary}
              </p>
            </div>
          )}

          {/* 3-Move Strategy Roadmap Cards */}
          <div className="card mb-lg">
            <div className="card-title mb-xs">🚀 3-Move Monthly Transfer Roadmap (TSP Rules)</div>
            <p style={{ color: 'var(--clr-text-muted)', fontSize: 13, marginBottom: 'var(--space-lg)' }}>
              TSP permits up to 3 Interfund Transfers (IFT) per calendar month. Move 3 is restricted exclusively to the 100% G Fund.
            </p>

            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: 'var(--space-lg)' }}>
              {/* Move 1 Card */}
              <div style={{
                background: 'rgba(59, 130, 246, 0.08)',
                border: '1px solid rgba(59, 130, 246, 0.3)',
                borderRadius: 'var(--radius-lg)',
                padding: 'var(--space-lg)',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between'
              }}>
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                    <span style={{ background: '#3b82f6', color: '#fff', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700 }}>
                      MOVE 1 (Start of Month)
                    </span>
                    <span style={{ fontSize: 12, color: 'var(--clr-text-muted)' }}>Any Funds</span>
                  </div>
                  <h4 style={{ margin: '8px 0 4px', fontSize: 16 }}>{result.move1Plan?.title || 'Move 1 Strategy'}</h4>
                  <div style={{ fontSize: 12, color: '#3b82f6', marginBottom: 12, fontWeight: 600 }}>
                    Trigger: {result.move1Plan?.triggerCondition}
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--clr-text-muted)', marginBottom: 12 }}>
                    {result.move1Plan?.rationale}
                  </p>

                  <div style={{ background: 'rgba(0,0,0,0.2)', padding: '10px 12px', borderRadius: 6, marginBottom: 12 }}>
                    <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', marginBottom: 4, fontWeight: 700 }}>Target Allocation:</div>
                    <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                      {result.move1Plan?.targetAllocation && Object.entries(result.move1Plan.targetAllocation).map(([fund, pct]) => (
                        <span key={fund} style={{ fontSize: 13, fontWeight: 700, color: FUND_COLORS[fund] ?? '#fff' }}>
                          {fund}: {pct}%
                        </span>
                      ))}
                    </div>
                  </div>
                </div>

                {result.move1Plan?.targetAllocation && (
                  <button
                    className="btn btn-primary"
                    style={{ width: '100%', marginTop: 8 }}
                    onClick={() => handleApplyAllocation(result.move1Plan.targetAllocation)}
                    disabled={applying}
                  >
                    Apply Move 1 Allocation <ArrowRight size={14} />
                  </button>
                )}
              </div>

              {/* Move 2 Card */}
              <div style={{
                background: 'rgba(245, 158, 11, 0.08)',
                border: '1px solid rgba(245, 158, 11, 0.3)',
                borderRadius: 'var(--radius-lg)',
                padding: 'var(--space-lg)',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between'
              }}>
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                    <span style={{ background: '#f59e0b', color: '#fff', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700 }}>
                      MOVE 2 (Mid-Month Pivot)
                    </span>
                    <span style={{ fontSize: 12, color: 'var(--clr-text-muted)' }}>Any Funds</span>
                  </div>
                  <h4 style={{ margin: '8px 0 4px', fontSize: 16 }}>{result.move2Plan?.title || 'Move 2 Strategy'}</h4>
                  <div style={{ fontSize: 12, color: '#f59e0b', marginBottom: 12, fontWeight: 600 }}>
                    Trigger: {result.move2Plan?.triggerCondition}
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--clr-text-muted)', marginBottom: 12 }}>
                    {result.move2Plan?.rationale}
                  </p>

                  <div style={{ background: 'rgba(0,0,0,0.2)', padding: '10px 12px', borderRadius: 6, marginBottom: 12 }}>
                    <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', marginBottom: 4, fontWeight: 700 }}>Target Allocation:</div>
                    <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                      {result.move2Plan?.targetAllocation && Object.entries(result.move2Plan.targetAllocation).map(([fund, pct]) => (
                        <span key={fund} style={{ fontSize: 13, fontWeight: 700, color: FUND_COLORS[fund] ?? '#fff' }}>
                          {fund}: {pct}%
                        </span>
                      ))}
                    </div>
                  </div>
                </div>

                {result.move2Plan?.targetAllocation && (
                  <button
                    className="btn btn-secondary"
                    style={{ width: '100%', marginTop: 8 }}
                    onClick={() => handleApplyAllocation(result.move2Plan.targetAllocation)}
                    disabled={applying}
                  >
                    Apply Move 2 Allocation <ArrowRight size={14} />
                  </button>
                )}
              </div>

              {/* Move 3 Card */}
              <div style={{
                background: 'rgba(16, 185, 129, 0.08)',
                border: '1px solid rgba(16, 185, 129, 0.3)',
                borderRadius: 'var(--radius-lg)',
                padding: 'var(--space-lg)',
                display: 'flex',
                flexDirection: 'column',
                justifyContent: 'space-between'
              }}>
                <div>
                  <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 8 }}>
                    <span style={{ background: '#10b981', color: '#fff', padding: '2px 8px', borderRadius: 4, fontSize: 11, fontWeight: 700 }}>
                      MOVE 3 (Safety Exit)
                    </span>
                    <span style={{ fontSize: 12, color: '#10b981', fontWeight: 700 }}>G-FUND ONLY</span>
                  </div>
                  <h4 style={{ margin: '8px 0 4px', fontSize: 16 }}>{result.move3Plan?.title || 'Move 3 Emergency Exit'}</h4>
                  <div style={{ fontSize: 12, color: '#10b981', marginBottom: 12, fontWeight: 600, display: 'flex', alignItems: 'center', gap: 4 }}>
                    <ShieldAlert size={14} /> Trigger: {result.move3Plan?.triggerCondition}
                  </div>
                  <p style={{ fontSize: 13, color: 'var(--clr-text-muted)', marginBottom: 12 }}>
                    {result.move3Plan?.rationale}
                  </p>

                  <div style={{ background: 'rgba(0,0,0,0.2)', padding: '10px 12px', borderRadius: 6, marginBottom: 12 }}>
                    <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', marginBottom: 4, fontWeight: 700 }}>Target Allocation:</div>
                    <div style={{ display: 'flex', gap: 10, flexWrap: 'wrap' }}>
                      <span style={{ fontSize: 14, fontWeight: 800, color: '#10b981' }}>
                        G Fund: 100%
                      </span>
                    </div>
                  </div>
                </div>

                <button
                  className="btn btn-secondary"
                  style={{ width: '100%', marginTop: 8, borderColor: '#10b981', color: '#10b981' }}
                  onClick={() => handleApplyAllocation({ 'G Fund': 100 })}
                  disabled={applying}
                >
                  Apply 100% G-Fund Exit <ShieldAlert size={14} />
                </button>
              </div>
            </div>
          </div>

          {/* Fund Scores */}
          {result.fundScores.length > 0 && (
            <div className="card mb-lg">
              <div className="card-title mb-md">6-Month Technical & Momentum Scores</div>
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

          {/* Full AI Analysis & Rationale */}
          <div className="card">
            <div className="card-title mb-md">📋 Detailed Strategy Rationale & Projections</div>
            <div className="markdown-body">
              <ReactMarkdown>{result.recommendationText}</ReactMarkdown>
            </div>
          </div>
        </>
      )}
    </div>
  )
}

