import { useEffect, useState } from 'react'
import { usersApi } from '../api/client'
import { TrendingUp, TrendingDown, BarChart2 } from 'lucide-react'
import {
  AreaChart, Area, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, ReferenceLine
} from 'recharts'

interface DataPoint {
  date: string
  portfolioValue: number
  fundValues: Record<string, number>
}

interface Summary {
  totalValue: number
  totalGain: number
  totalGainPercent: number
  history: DataPoint[]
}

const RANGES = [
  { label: '1M', days: 30 },
  { label: '3M', days: 90 },
  { label: '6M', days: 180 },
  { label: '1Y', days: 365 },
]

export default function Performance() {
  const [summary, setSummary] = useState<Summary | null>(null)
  const [days, setDays] = useState(90)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      try {
        const data = await usersApi.getPerformance(days)
        setSummary(data)
      } catch (e) { console.error(e) }
      finally { setLoading(false) }
    }
    load()
  }, [days])

  const isPositive = (summary?.totalGain ?? 0) >= 0
  const chartData = summary?.history.map(d => ({
    date: d.date,
    value: parseFloat(d.portfolioValue.toFixed(2)),
  })) ?? []

  const baseline = 10000

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title"><BarChart2 size={26} style={{ display: 'inline', marginRight: 10 }} />Performance</h1>
        <p className="page-subtitle">Portfolio performance based on your current fund allocations ($10,000 baseline)</p>
      </div>

      {/* Summary Stats */}
      <div className="stat-grid" style={{ marginBottom: 'var(--space-xl)' }}>
        <div className="stat-card">
          <div className="stat-label">Portfolio Value</div>
          <div className="stat-value">
            {summary ? `$${summary.totalValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}` : '—'}
          </div>
          <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', marginTop: 4 }}>$10,000 baseline</div>
        </div>

        <div className="stat-card">
          <div className="stat-label">Total Gain / Loss</div>
          <div className="stat-value" style={{ color: isPositive ? 'var(--clr-success)' : 'var(--clr-danger)' }}>
            {summary
              ? `${isPositive ? '+' : ''}$${Math.abs(summary.totalGain).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`
              : '—'}
          </div>
          {summary && (
            <div className={`stat-change ${isPositive ? 'positive' : 'negative'}`}>
              {isPositive ? <TrendingUp size={12} /> : <TrendingDown size={12} />}
              {isPositive ? '+' : ''}{summary.totalGainPercent.toFixed(2)}%
            </div>
          )}
        </div>

        <div className="stat-card">
          <div className="stat-label">Analysis Period</div>
          <div className="stat-value" style={{ fontSize: 20 }}>
            {days < 365 ? `${days} days` : `${(days / 365).toFixed(1)}yr`}
          </div>
          <div className="range-buttons" style={{ marginTop: 8 }}>
            {RANGES.map(r => (
              <button key={r.label} className={`range-btn${days === r.days ? ' active' : ''}`}
                onClick={() => setDays(r.days)}>{r.label}</button>
            ))}
          </div>
        </div>
      </div>

      {/* Performance Chart */}
      <div className="card">
        <div className="card-header">
          <div className="card-title">Portfolio Value Over Time</div>
          <div style={{ fontSize: 12, color: 'var(--clr-text-muted)' }}>Based on your current allocation percentages</div>
        </div>

        {loading ? (
          <div className="loading-container"><div className="spinner" /></div>
        ) : !summary || summary.history.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 'var(--space-3xl)', color: 'var(--clr-text-muted)' }}>
            <div style={{ fontSize: 48, marginBottom: 'var(--space-md)' }}>📊</div>
            <div style={{ fontWeight: 600, marginBottom: 8 }}>No portfolio data yet</div>
            <div style={{ fontSize: 13 }}>Set your fund allocations to see performance tracking.</div>
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={380}>
            <AreaChart data={chartData} margin={{ top: 10, right: 20, left: 10, bottom: 5 }}>
              <defs>
                <linearGradient id="portfolioGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor={isPositive ? '#10b981' : '#ef4444'} stopOpacity={0.3} />
                  <stop offset="95%" stopColor={isPositive ? '#10b981' : '#ef4444'} stopOpacity={0} />
                </linearGradient>
              </defs>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
              <XAxis dataKey="date" tick={{ fill: '#64748b', fontSize: 11 }}
                tickFormatter={v => new Date(v).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                interval="preserveStartEnd" />
              <YAxis tick={{ fill: '#64748b', fontSize: 11 }}
                tickFormatter={v => `$${(v / 1000).toFixed(1)}k`} domain={['auto', 'auto']} />
              <Tooltip
                contentStyle={{ background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 8, fontSize: 12 }}
                labelStyle={{ color: 'var(--clr-text-muted)' }}
                formatter={(v: number) => [`$${v.toLocaleString('en-US', { minimumFractionDigits: 2 })}`, 'Portfolio']}
                labelFormatter={(l: string) => new Date(l).toLocaleDateString('en-US', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' })}
              />
              <ReferenceLine y={baseline} stroke="rgba(255,255,255,0.2)" strokeDasharray="6 3"
                label={{ value: '$10k baseline', fill: '#64748b', fontSize: 10, position: 'insideTopLeft' }} />
              <Area type="monotone" dataKey="value"
                stroke={isPositive ? '#10b981' : '#ef4444'}
                strokeWidth={2.5}
                fill="url(#portfolioGrad)"
                dot={false}
                activeDot={{ r: 5, fill: isPositive ? '#10b981' : '#ef4444' }}
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>

      {/* Disclaimer */}
      <div className="alert alert-info" style={{ marginTop: 'var(--space-lg)' }}>
        <span>ℹ️</span>
        <span>
          Performance is calculated on a <strong>$10,000 hypothetical baseline</strong> using your current
          allocation percentages and official TSP closing prices. This is for informational purposes only
          and does not constitute financial advice.
        </span>
      </div>
    </div>
  )
}
