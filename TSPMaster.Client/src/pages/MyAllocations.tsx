import { useEffect, useState, useCallback } from 'react'
import { allocationsApi } from '../api/client'
import { Save, PieChart } from 'lucide-react'
import { PieChart as RechartsPie, Pie, Cell, Tooltip, ResponsiveContainer, Legend } from 'recharts'

interface AllocationItem { fundName: string; percentage: number }

const ALL_FUNDS = ['G Fund', 'F Fund', 'C Fund', 'S Fund', 'I Fund',
  'L Income', 'L 2030', 'L 2035', 'L 2040', 'L 2045', 'L 2050', 'L 2055', 'L 2060', 'L 2065', 'L 2070', 'L 2075']

const FUND_COLORS: Record<string, string> = {
  'G Fund': '#10b981', 'F Fund': '#8b5cf6', 'C Fund': '#3b82f6',
  'S Fund': '#f59e0b', 'I Fund': '#ef4444',
  'L Income': '#6366f1', 'L 2030': '#ec4899', 'L 2035': '#14b8a6',
  'L 2040': '#f97316', 'L 2045': '#84cc16', 'L 2050': '#06b6d4',
  'L 2055': '#a855f7', 'L 2060': '#f43f5e', 'L 2065': '#0ea5e9', 'L 2070': '#22c55e', 'L 2075': '#eab308',
}

interface TransferStatus {
  transfersUsed: number
  remainingTransfers: number
  maxTransfers: number
  isMove3GFundOnly: boolean
  currentMonth: string
}

export default function MyAllocations() {
  const [allocations, setAllocations] = useState<AllocationItem[]>(
    ALL_FUNDS.map(f => ({ fundName: f, percentage: 0 }))
  )
  const [transferStatus, setTransferStatus] = useState<TransferStatus | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [success, setSuccess] = useState(false)
  const [error, setError] = useState('')

  const loadData = useCallback(async () => {
    try {
      const [data, status] = await Promise.all([
        allocationsApi.get(),
        allocationsApi.getStatus().catch(() => null)
      ])
      setAllocations(ALL_FUNDS.map(f => ({
        fundName: f,
        percentage: data.find((d: AllocationItem) => d.fundName === f)?.percentage ?? 0
      })))
      if (status) setTransferStatus(status)
    } catch (e) { console.error(e) }
    finally { setLoading(false) }
  }, [])

  useEffect(() => {
    loadData()
  }, [loadData])

  const total = allocations.reduce((sum, a) => sum + a.percentage, 0)
  const isValid = Math.abs(total - 100) < 0.01

  const updatePercent = useCallback((fundName: string, value: number) => {
    setAllocations(prev => prev.map(a => a.fundName === fundName ? { ...a, percentage: value } : a))
    setSuccess(false)
    setError('')
  }, [])

  const handleSave = async () => {
    if (!isValid) { setError('Allocations must sum to exactly 100%.'); return }
    setSaving(true)
    setError('')
    try {
      await allocationsApi.set(allocations.filter(a => a.percentage > 0))
      setSuccess(true)
      await loadData()
      setTimeout(() => setSuccess(false), 3000)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Failed to save allocations.')
    } finally { setSaving(false) }
  }

  const pieData = allocations.filter(a => a.percentage > 0).map(a => ({
    name: a.fundName, value: a.percentage
  }))

  return (
    <div className="fade-in">
      <div className="page-header" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        <div>
          <h1 className="page-title">My Allocations</h1>
          <p className="page-subtitle">Set your TSP fund allocations. Interfund Transfers (IFT) are subject to 3 moves/month rules.</p>
        </div>

        {transferStatus && (
          <div style={{
            background: 'var(--clr-surface-2)',
            border: '1px solid var(--clr-border)',
            borderRadius: 'var(--radius-lg)',
            padding: '8px 16px',
            textAlign: 'right'
          }}>
            <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', textTransform: 'uppercase', fontWeight: 700 }}>
              Monthly Transfers ({transferStatus.currentMonth})
            </div>
            <div style={{ fontSize: 16, fontWeight: 800, color: transferStatus.remainingTransfers === 0 ? '#ef4444' : transferStatus.isMove3GFundOnly ? '#10b981' : 'var(--clr-primary)' }}>
              {transferStatus.transfersUsed} / {transferStatus.maxTransfers} Moves Used ({transferStatus.remainingTransfers} Left)
            </div>
          </div>
        )}
      </div>

      {transferStatus?.isMove3GFundOnly && (
        <div className="alert alert-warning mb-lg" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          🛡️ <strong>Move 3 Warning:</strong> You are on your 3rd Interfund Transfer of the month. Under TSP rules, this move is restricted exclusively to <strong>100% G Fund</strong>.
        </div>
      )}

      {transferStatus?.remainingTransfers === 0 && (
        <div className="alert alert-error mb-lg" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          🚫 <strong>Transfer Limit Reached:</strong> You have executed all 3 Interfund Transfers allowed for {transferStatus.currentMonth}. Moves reset on the 1st of next month.
        </div>
      )}

      <div className="grid-2" style={{ alignItems: 'start' }}>
        {/* Slider Controls */}
        <div className="card">
          <div className="card-header">
            <div className="card-title">Fund Allocation</div>
            <div className={`allocation-total ${isValid ? 'valid' : 'invalid'}`}
              style={{ padding: '4px 12px', borderRadius: 'var(--radius-full)', fontSize: 14 }}>
              {total.toFixed(0)}% / 100%
            </div>
          </div>

          {success && <div className="alert alert-success">✅ Allocations saved successfully!</div>}
          {error && <div className="alert alert-error">⚠️ {error}</div>}

          {loading ? (
            <div className="loading-container"><div className="spinner" /></div>
          ) : (
            <>
              <div style={{ maxHeight: 480, overflowY: 'auto', paddingRight: 4 }}>
                {allocations.map(a => (
                  <div key={a.fundName} className="allocation-row">
                    <div className="allocation-fund-label" style={{ display: 'flex', alignItems: 'center', gap: 6 }}>
                      <span style={{ width: 8, height: 8, borderRadius: '50%', background: FUND_COLORS[a.fundName] ?? '#64748b', flexShrink: 0 }} />
                      {a.fundName}
                    </div>
                    <input
                      type="range"
                      className="allocation-slider"
                      min={0} max={100} step={1}
                      value={a.percentage}
                      onChange={e => updatePercent(a.fundName, Number(e.target.value))}
                      style={{ accentColor: FUND_COLORS[a.fundName] ?? 'var(--clr-primary)' } as React.CSSProperties}
                    />
                    <div className="allocation-pct">{a.percentage}%</div>
                    <input
                      type="number"
                      min={0} max={100}
                      value={a.percentage}
                      onChange={e => updatePercent(a.fundName, Math.min(100, Math.max(0, Number(e.target.value))))}
                      style={{ width: 54, background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 6, color: 'var(--clr-text)', padding: '4px 6px', fontSize: 13 }}
                    />
                  </div>
                ))}
              </div>

              <div className={`allocation-total ${isValid ? 'valid' : 'invalid'}`} style={{ marginTop: 'var(--space-md)' }}>
                <span>Total</span>
                <span>{total.toFixed(1)}%</span>
              </div>

              <button
                id="save-allocations"
                className="btn btn-primary"
                onClick={handleSave}
                disabled={saving || !isValid || transferStatus?.remainingTransfers === 0}
                style={{ marginTop: 'var(--space-md)', width: '100%', justifyContent: 'center' }}
              >
                {saving ? <span className="spinner" style={{ width: 16, height: 16, borderWidth: 2 }} /> : <><Save size={16} /> Save Allocations</>}
              </button>
            </>
          )}
        </div>

        {/* Pie Chart */}
        <div className="card" style={{ minHeight: 400 }}>
          <div className="card-header">
            <div className="card-title"><PieChart size={16} style={{ display: 'inline', marginRight: 6 }} />Allocation Breakdown</div>
          </div>

          {pieData.length === 0 ? (
            <div style={{ textAlign: 'center', padding: 'var(--space-3xl)', color: 'var(--clr-text-muted)' }}>
              Move sliders to visualize your allocation
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={360}>
              <RechartsPie>
                <Pie data={pieData} cx="50%" cy="45%" outerRadius={130} dataKey="value"
                  label={({ name, value }) => `${name}: ${value}%`}
                  labelLine={{ stroke: 'var(--clr-text-muted)' }}>
                  {pieData.map(entry => (
                    <Cell key={entry.name} fill={FUND_COLORS[entry.name] ?? '#6366f1'} />
                  ))}
                </Pie>
                <Tooltip
                  contentStyle={{ background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 8, fontSize: 12 }}
                  formatter={(v: number) => [`${v}%`, 'Allocation']}
                />
                <Legend wrapperStyle={{ fontSize: 11, color: 'var(--clr-text-muted)', paddingTop: 8 }} />
              </RechartsPie>
            </ResponsiveContainer>
          )}
        </div>
      </div>
    </div>
  )
}
