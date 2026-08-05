import { useEffect, useState } from 'react'
import { fundsApi } from '../api/client'
import { subDays, format } from 'date-fns'
import {
  LineChart, Line, XAxis, YAxis, CartesianGrid, Tooltip,
  Legend, ResponsiveContainer
} from 'recharts'

interface PricePoint { date: string; price: number }

const FUND_META: Record<string, { color: string; label: string }> = {
  'G Fund': { color: '#10b981', label: 'G (Gov. Securities)' },
  'F Fund': { color: '#8b5cf6', label: 'F (Fixed Income)' },
  'C Fund': { color: '#3b82f6', label: 'C (S&P 500)' },
  'S Fund': { color: '#f59e0b', label: 'S (Small Cap)' },
  'I Fund': { color: '#ef4444', label: 'I (International)' },
}

const LIFECYCLE_FUNDS = ['L Income', 'L 2030', 'L 2035', 'L 2040', 'L 2045', 'L 2050', 'L 2055', 'L 2060', 'L 2065', 'L 2070', 'L 2075']
const LIFECYCLE_COLOR = '#6366f1'

const RANGES = [
  { label: '1M', days: 30 },
  { label: '3M', days: 90 },
  { label: '6M', days: 180 },
  { label: '1Y', days: 365 },
  { label: '3Y', days: 1095 },
]

type TabType = 'core' | 'lifecycle'

export default function FundPrices() {
  const [tab, setTab] = useState<TabType>('core')
  const [range, setRange] = useState(90)
  const [history, setHistory] = useState<Record<string, PricePoint[]>>({})
  const [latest, setLatest] = useState<{ fundName: string; price: number; changePercent: number | null }[]>([])
  const [loading, setLoading] = useState(true)
  const [selectedFunds, setSelectedFunds] = useState<Set<string>>(new Set(['G Fund', 'C Fund', 'S Fund']))

  useEffect(() => {
    const load = async () => {
      setLoading(true)
      try {
        const to = new Date()
        const from = subDays(to, range)
        const [hist, lat] = await Promise.all([
          fundsApi.getAllHistory(format(from, 'yyyy-MM-dd'), format(to, 'yyyy-MM-dd')),
          fundsApi.getLatest(),
        ])
        setHistory(hist)
        setLatest(lat)
      } catch (e) { console.error(e) }
      finally { setLoading(false) }
    }
    load()
  }, [range])

  // Normalize prices to percentage return from start for comparison
  const buildChartData = (funds: string[]) => {
    const allDates = new Set<string>()
    funds.forEach(f => history[f]?.forEach(p => allDates.add(p.date)))
    const sortedDates = Array.from(allDates).sort()

    const startPrices: Record<string, number> = {}
    funds.forEach(f => {
      const first = history[f]?.[0]
      if (first) startPrices[f] = first.price
    })

    return sortedDates.map(date => {
      const row: Record<string, string | number> = { date }
      funds.forEach(f => {
        const pt = history[f]?.find(p => p.date === date)
        if (pt && startPrices[f]) {
          row[f] = parseFloat((((pt.price - startPrices[f]) / startPrices[f]) * 100).toFixed(2))
        }
      })
      return row
    })
  }

  const toggleFund = (name: string) => {
    setSelectedFunds(prev => {
      const next = new Set(prev)
      next.has(name) ? next.delete(name) : next.add(name)
      return next
    })
  }

  const coreFundNames = Object.keys(FUND_META)
  const activeFunds = tab === 'core'
    ? coreFundNames.filter(f => selectedFunds.has(f))
    : LIFECYCLE_FUNDS.filter(f => selectedFunds.has(f))

  const chartData = buildChartData(activeFunds)

  return (
    <div className="fade-in">
      <div className="page-header">
        <h1 className="page-title">Fund Prices</h1>
        <p className="page-subtitle">Historical closing prices sourced nightly from TSP.gov</p>
      </div>

      {/* Latest Prices Table */}
      <div className="card mb-lg">
        <div className="card-header">
          <div className="card-title">Latest Closing Prices</div>
          <div style={{ display: 'flex', gap: 'var(--space-sm)' }}>
            <button className={`btn btn-sm ${tab === 'core' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setTab('core')}>Core Funds</button>
            <button className={`btn btn-sm ${tab === 'lifecycle' ? 'btn-primary' : 'btn-secondary'}`} onClick={() => setTab('lifecycle')}>Lifecycle</button>
          </div>
        </div>

        {loading ? (
          <div className="loading-container" style={{ padding: 'var(--space-xl)' }}>
            <div className="spinner" />
          </div>
        ) : (
          <table className="data-table">
            <thead>
              <tr>
                <th></th>
                <th>Fund</th>
                <th>Latest Price</th>
                <th>Daily Change</th>
                <th>Daily %</th>
              </tr>
            </thead>
            <tbody>
              {latest
                .filter(f => tab === 'core' ? coreFundNames.includes(f.fundName) : LIFECYCLE_FUNDS.includes(f.fundName))
                .map(f => {
                  const pos = (f.changePercent ?? 0) >= 0
                  const color = FUND_META[f.fundName]?.color ?? LIFECYCLE_COLOR
                  const checked = selectedFunds.has(f.fundName)
                  return (
                    <tr key={f.fundName} style={{ cursor: 'pointer' }} onClick={() => toggleFund(f.fundName)}>
                      <td>
                        <input type="checkbox" checked={checked} readOnly
                          style={{ accentColor: color, cursor: 'pointer' }} />
                      </td>
                      <td>
                        <span style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                          <span style={{ width: 10, height: 10, borderRadius: '50%', background: color, display: 'inline-block' }} />
                          <strong>{f.fundName}</strong>
                        </span>
                      </td>
                      <td style={{ fontFamily: 'var(--font-heading)', fontWeight: 700, fontSize: 16 }}>
                        ${f.price.toFixed(4)}
                      </td>
                      <td className={pos ? 'text-success' : 'text-danger'}>—</td>
                      <td>
                        {f.changePercent !== null ? (
                          <span className={`stat-change ${pos ? 'positive' : 'negative'}`}>
                            {pos ? '+' : ''}{f.changePercent.toFixed(2)}%
                          </span>
                        ) : '—'}
                      </td>
                    </tr>
                  )
                })}
            </tbody>
          </table>
        )}
      </div>

      {/* Chart */}
      <div className="card">
        <div className="card-header">
          <div>
            <div className="card-title">Normalized Return (%)</div>
            <div className="card-subtitle">Percentage return from the start of the selected period</div>
          </div>
          <div className="range-buttons">
            {RANGES.map(r => (
              <button key={r.label} className={`range-btn${range === r.days ? ' active' : ''}`}
                onClick={() => setRange(r.days)}>
                {r.label}
              </button>
            ))}
          </div>
        </div>

        {loading ? (
          <div className="loading-container"><div className="spinner" /></div>
        ) : activeFunds.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 'var(--space-2xl)', color: 'var(--clr-text-muted)' }}>
            Select at least one fund above to display the chart.
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={380}>
            <LineChart data={chartData} margin={{ top: 5, right: 20, left: 0, bottom: 5 }}>
              <CartesianGrid strokeDasharray="3 3" stroke="rgba(255,255,255,0.05)" />
              <XAxis dataKey="date" tick={{ fill: '#64748b', fontSize: 11 }}
                tickFormatter={v => new Date(v).toLocaleDateString('en-US', { month: 'short', day: 'numeric' })}
                interval="preserveStartEnd" />
              <YAxis tick={{ fill: '#64748b', fontSize: 11 }}
                tickFormatter={v => `${v > 0 ? '+' : ''}${v}%`} />
              <Tooltip
                contentStyle={{ background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 8, fontSize: 12 }}
                labelStyle={{ color: 'var(--clr-text-muted)' }}
                formatter={(v: number, name: string) => [`${v > 0 ? '+' : ''}${v}%`, name]}
                labelFormatter={(l: string) => new Date(l).toLocaleDateString('en-US', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' })}
              />
              <Legend wrapperStyle={{ fontSize: 12, color: 'var(--clr-text-muted)', paddingTop: 16 }} />
              {activeFunds.map(fundName => {
                const color = FUND_META[fundName]?.color ?? LIFECYCLE_COLOR
                return (
                  <Line key={fundName} type="monotone" dataKey={fundName}
                    stroke={color} strokeWidth={2} dot={false} activeDot={{ r: 4, fill: color }} />
                )
              })}
            </LineChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
