import { useEffect, useState, useCallback } from 'react'
import { allocationsApi, usersApi, analysisApi } from '../api/client'
import {
  Save, PieChart, DollarSign, Calendar, History, Trash2,
  Sparkles, TrendingUp, TrendingDown, ShieldAlert, AlertCircle, PlusCircle, X, ChevronDown, ChevronUp
} from 'lucide-react'
import {
  PieChart as RechartsPie, Pie, Cell, Tooltip, ResponsiveContainer, Legend,
  AreaChart, Area, XAxis, YAxis, CartesianGrid
} from 'recharts'

interface AllocationItem {
  fundName: string
  percentage: number
}

interface TransferStatus {
  transfersUsed: number
  remainingTransfers: number
  maxTransfers: number
  isMove3GFundOnly: boolean
  currentMonth: string
}

interface AllocationMove {
  id: number
  effectiveDate: string
  description: string
  balanceAtMove: number
  allocations: AllocationItem[]
  moveNumberInMonth: number
  monthKey: string
  createdAt: string
}

interface OverviewData {
  initialTspBalance: number
  currentTspBalance: number
  initialBalanceDate: string | null
  currentAllocations: AllocationItem[]
  transferStatus: TransferStatus
  moveHistory: AllocationMove[]
  recommendedFund?: string
  recommendationText?: string
}

interface PerformancePoint {
  date: string
  portfolioValue: number
}

const CORE_FUNDS = ['G Fund', 'F Fund', 'C Fund', 'S Fund', 'I Fund']

const L_FUNDS = [
  'L Income', 'L 2030', 'L 2035', 'L 2040', 'L 2045', 'L 2050', 'L 2055', 'L 2060', 'L 2065', 'L 2070', 'L 2075'
]

const ALL_FUNDS = [...CORE_FUNDS, ...L_FUNDS]

const FUND_COLORS: Record<string, string> = {
  'G Fund': '#10b981', 'F Fund': '#8b5cf6', 'C Fund': '#3b82f6',
  'S Fund': '#f59e0b', 'I Fund': '#ef4444',
  'L Income': '#6366f1', 'L 2030': '#ec4899', 'L 2035': '#14b8a6',
  'L 2040': '#f97316', 'L 2045': '#84cc16', 'L 2050': '#06b6d4',
  'L 2055': '#a855f7', 'L 2060': '#f43f5e', 'L 2065': '#0ea5e9', 'L 2070': '#22c55e', 'L 2075': '#eab308',
}

const RANGES = [
  { label: '1M', days: 30 },
  { label: '3M', days: 90 },
  { label: '6M', days: 180 },
  { label: '1Y', days: 365 },
]

export default function MyAllocations() {
  const [overview, setOverview] = useState<OverviewData | null>(null)
  const [loading, setLoading] = useState(true)
  const [saving, setSaving] = useState(false)
  const [savingBalance, setSavingBalance] = useState(false)
  const [success, setSuccess] = useState('')
  const [error, setError] = useState('')

  // Balance Form State
  const [balanceInput, setBalanceInput] = useState<string>('')
  const [showBalanceForm, setShowBalanceForm] = useState(false)

  // Move Form Modal State
  const [showMoveModal, setShowMoveModal] = useState(false)
  const [showLFunds, setShowLFunds] = useState(false)
  const [moveDate, setMoveDate] = useState<string>(new Date().toISOString().split('T')[0])
  const [moveDescription, setMoveDescription] = useState<string>('')
  const [formAllocations, setFormAllocations] = useState<AllocationItem[]>(
    ALL_FUNDS.map(f => ({ fundName: f, percentage: 0 }))
  )

  // Performance Chart State
  const [perfDays, setPerfDays] = useState(90)
  const [perfData, setPerfData] = useState<{ totalValue: number; totalGain: number; totalGainPercent: number; history: PerformancePoint[] } | null>(null)
  const [perfLoading, setPerfLoading] = useState(false)

  const loadOverview = useCallback(async () => {
    try {
      setLoading(true)
      let data: OverviewData
      try {
        data = await allocationsApi.getOverview()
      } catch (err) {
        console.warn('Overview endpoint returned error, using fallback API endpoints:', err)
        const [currentAllocations, status, history, recommendation] = await Promise.all([
          allocationsApi.get().catch(() => []),
          allocationsApi.getStatus().catch(() => ({ transfersUsed: 0, remainingTransfers: 3, maxTransfers: 3, isMove3GFundOnly: false, currentMonth: new Date().toISOString().slice(0, 7) })),
          allocationsApi.getHistory().catch(() => []),
          analysisApi.getRecommendation().catch(() => null)
        ])

        data = {
          initialTspBalance: 0,
          currentTspBalance: 0,
          initialBalanceDate: null,
          currentAllocations: currentAllocations || [],
          transferStatus: status,
          moveHistory: history || [],
          recommendedFund: recommendation?.topRecommendation,
          recommendationText: recommendation?.recommendationText
        }
      }

      setOverview(data)

      // Initialize form allocations from active current allocations
      setFormAllocations(ALL_FUNDS.map(f => ({
        fundName: f,
        percentage: data.currentAllocations.find((a: AllocationItem) => a.fundName === f)?.percentage ?? 0
      })))

      if (data.initialTspBalance > 0) {
        setBalanceInput(data.initialTspBalance.toString())
      }
    } catch (e) {
      console.error(e)
      setError('Failed to load allocation overview.')
    } finally {
      setLoading(false)
    }
  }, [])

  const loadPerformance = useCallback(async () => {
    try {
      setPerfLoading(true)
      const res = await usersApi.getPerformance(perfDays)
      setPerfData(res)
    } catch (e) {
      console.error(e)
    } finally {
      setPerfLoading(false)
    }
  }, [perfDays])

  useEffect(() => {
    loadOverview()
  }, [loadOverview])

  useEffect(() => {
    loadPerformance()
  }, [loadPerformance])

  const totalPercentage = formAllocations.reduce((sum, a) => sum + a.percentage, 0)
  const isValidTotal = Math.abs(totalPercentage - 100) < 0.01

  const updatePercent = (fundName: string, val: number) => {
    setFormAllocations(prev => prev.map(a => a.fundName === fundName ? { ...a, percentage: val } : a))
    setSuccess('')
    setError('')
  }

  const applyQuickPreset = (presetDict: Record<string, number>) => {
    setFormAllocations(ALL_FUNDS.map(f => ({
      fundName: f,
      percentage: presetDict[f] ?? 0
    })))
    setSuccess('')
    setError('')
  }

  const handleSetInitialBalance = async () => {
    const val = parseFloat(balanceInput)
    if (isNaN(val) || val < 0) {
      setError('Please enter a valid non-negative TSP balance.')
      return
    }
    setSavingBalance(true)
    setError('')
    try {
      await allocationsApi.setInitialBalance(val, new Date().toISOString())
      setSuccess('Initial TSP balance saved successfully!')
      setShowBalanceForm(false)
      await loadOverview()
      await loadPerformance()
      setTimeout(() => setSuccess(''), 3000)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Failed to save balance.')
    } finally {
      setSavingBalance(false)
    }
  }

  const handlePreFillRecommendation = () => {
    if (!overview?.recommendedFund) return
    const rec = overview.recommendedFund
    const newAlloc: Record<string, number> = {}

    if (rec.includes('G Fund')) {
      newAlloc['G Fund'] = 100
    } else if (rec.includes('C Fund') && rec.includes('S Fund')) {
      newAlloc['C Fund'] = 50
      newAlloc['S Fund'] = 50
    } else {
      newAlloc[rec] = 100
    }

    applyQuickPreset(newAlloc)
    setMoveDescription(`Followed AI Recommendation: ${rec}`)
    setSuccess(`Pre-filled target allocation from AI recommendation: ${rec}`)
    setTimeout(() => setSuccess(''), 4000)
  }

  const handleRecordMove = async () => {
    if (!isValidTotal) {
      setError('Allocations must sum to exactly 100%.')
      return
    }
    setSaving(true)
    setError('')
    try {
      const activeAllocations = formAllocations.filter(a => a.percentage > 0)

      await allocationsApi.recordMove({
        effectiveDate: moveDate,
        description: moveDescription,
        allocations: activeAllocations
      })

      setSuccess('Move recorded successfully! Performance tracking updated.')
      setMoveDescription('')
      setShowMoveModal(false)
      await loadOverview()
      await loadPerformance()
      setTimeout(() => setSuccess(''), 3000)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Failed to record move.')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteMove = async (id: number) => {
    if (!confirm('Are you sure you want to delete this move entry from your history?')) return
    try {
      await allocationsApi.deleteMove(id)
      setSuccess('Move entry removed.')
      await loadOverview()
      await loadPerformance()
      setTimeout(() => setSuccess(''), 3000)
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { message?: string } } })?.response?.data?.message
      setError(msg ?? 'Failed to delete move.')
    }
  }

  const pieData = (overview?.currentAllocations ?? [])
    .filter(a => a.percentage > 0)
    .map(a => ({ name: a.fundName, value: a.percentage }))

  const status = overview?.transferStatus
  const isPositiveGain = (perfData?.totalGain ?? 0) >= 0

  return (
    <div className="fade-in" style={{ paddingBottom: 'var(--space-3xl)' }}>
      {/* Header Banner */}
      <div className="page-header" style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 16 }}>
        <div>
          <h1 className="page-title" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
            <PieChart size={28} style={{ color: 'var(--clr-primary)' }} />
            My Allocations & Performance Tracking
          </h1>
          <p className="page-subtitle">
            Set your TSP balance, log moves made on <strong>tsp.gov</strong>, track your move history, and monitor performance over time.
          </p>
        </div>

        {/* Action Button & Monthly Counter */}
        <div style={{ display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap' }}>
          <button
            className="btn btn-primary"
            onClick={() => setShowMoveModal(true)}
            style={{ padding: '10px 20px', fontSize: 14, fontWeight: 700, display: 'flex', alignItems: 'center', gap: 8, boxShadow: 'var(--shadow-glow)' }}
          >
            <PlusCircle size={18} /> Record a Move Made on tsp.gov
          </button>

          {status && (
            <div style={{
              background: 'var(--clr-surface-2)',
              border: '1px solid var(--clr-border)',
              borderRadius: 'var(--radius-lg)',
              padding: '10px 18px',
              minWidth: 240,
              boxShadow: 'var(--shadow-sm)'
            }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 4 }}>
                <span style={{ fontSize: 11, color: 'var(--clr-text-muted)', textTransform: 'uppercase', fontWeight: 700 }}>
                  Monthly Moves ({status.currentMonth})
                </span>
                <span className={`badge ${status.remainingTransfers === 0 ? 'badge-danger' : status.isMove3GFundOnly ? 'badge-warning' : 'badge-primary'}`}>
                  {status.remainingTransfers} Left
                </span>
              </div>

              <div style={{ fontSize: 16, fontWeight: 800, color: 'var(--clr-text)' }}>
                {status.transfersUsed} / {status.maxTransfers} Moves Executed
              </div>

              <div style={{ display: 'flex', gap: 6, marginTop: 6 }}>
                {[1, 2, 3].map(step => {
                  const isExecuted = step <= status.transfersUsed
                  const isMove3G = step === 3
                  return (
                    <div key={step} style={{
                      flex: 1, height: 5, borderRadius: 4,
                      background: isExecuted
                        ? (isMove3G ? '#f59e0b' : 'var(--clr-primary)')
                        : 'rgba(255,255,255,0.1)',
                      transition: 'all 0.3s ease'
                    }} title={step === 3 ? 'Move 3: 100% G Fund Only' : `Move ${step}: Any Fund`} />
                  )
                })}
              </div>
            </div>
          )}
        </div>
      </div>

      {/* Global Alerts */}
      {status?.isMove3GFundOnly && (
        <div className="alert alert-warning mb-lg" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <ShieldAlert size={20} />
          <span>
            <strong>Move 3 Rule Notice:</strong> You are executing your 3rd Interfund Transfer for {status.currentMonth}.
            Under TSP regulations, your 3rd move must be <strong>100% G Fund</strong>.
          </span>
        </div>
      )}

      {status?.remainingTransfers === 0 && (
        <div className="alert alert-error mb-lg" style={{ display: 'flex', alignItems: 'center', gap: 10 }}>
          <AlertCircle size={20} />
          <span>
            <strong>Monthly Move Limit Reached:</strong> You have executed all 3 allowed Interfund Transfers for {status.currentMonth}.
            Additional moves will unlock on the 1st of next month.
          </span>
        </div>
      )}

      {success && <div className="alert alert-success mb-lg">✅ {success}</div>}
      {error && <div className="alert alert-error mb-lg">⚠️ {error}</div>}

      {/* Overview Cards Grid */}
      <div className="grid-2" style={{ alignItems: 'start', marginBottom: 'var(--space-xl)' }}>
        
        {/* TSP Balance & Active Allocation Card */}
        <div className="card">
          <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <div className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <DollarSign size={18} style={{ color: 'var(--clr-primary)' }} />
              TSP Account Balance
            </div>
            <button
              className="btn btn-secondary btn-sm"
              onClick={() => setShowBalanceForm(!showBalanceForm)}
            >
              {showBalanceForm ? 'Cancel' : (overview?.initialTspBalance ? 'Edit Balance' : '+ Set TSP Balance')}
            </button>
          </div>

          {showBalanceForm ? (
            <div style={{ background: 'var(--clr-surface-2)', padding: 16, borderRadius: 'var(--radius-md)', marginBottom: 16 }}>
              <label style={{ fontSize: 12, fontWeight: 600, display: 'block', marginBottom: 6 }}>
                Enter Initial / Current TSP Balance ($):
              </label>
              <div style={{ display: 'flex', gap: 8 }}>
                <div style={{ position: 'relative', flex: 1 }}>
                  <span style={{ position: 'absolute', left: 12, top: 10, color: 'var(--clr-text-muted)' }}>$</span>
                  <input
                    type="number"
                    step="0.01"
                    min="0"
                    placeholder="e.g. 50000"
                    value={balanceInput}
                    onChange={e => setBalanceInput(e.target.value)}
                    style={{ width: '100%', paddingLeft: 28, background: 'var(--clr-bg)', border: '1px solid var(--clr-border)', borderRadius: 6, color: 'var(--clr-text)', height: 38 }}
                  />
                </div>
                <button
                  className="btn btn-primary btn-sm"
                  onClick={handleSetInitialBalance}
                  disabled={savingBalance}
                >
                  {savingBalance ? <span className="spinner" style={{ width: 14, height: 14, borderWidth: 2 }} /> : <><Save size={14} /> Save</>}
                </button>
              </div>
            </div>
          ) : (
            <div style={{ display: 'flex', gap: 24, padding: '12px 0 20px', borderBottom: '1px solid var(--clr-border)', marginBottom: 20 }}>
              <div>
                <div style={{ fontSize: 12, color: 'var(--clr-text-muted)', fontWeight: 600 }}>Initial Setup Balance</div>
                <div style={{ fontSize: 24, fontWeight: 800, color: 'var(--clr-primary)' }}>
                  {overview?.initialTspBalance
                    ? `$${overview.initialTspBalance.toLocaleString('en-US', { minimumFractionDigits: 2 })}`
                    : '$0.00'}
                </div>
              </div>
              <div style={{ borderLeft: '1px solid var(--clr-border)', paddingLeft: 24 }}>
                <div style={{ fontSize: 12, color: 'var(--clr-text-muted)', fontWeight: 600 }}>Current Estimated Portfolio Value</div>
                <div style={{ fontSize: 24, fontWeight: 800, color: 'var(--clr-text)' }}>
                  {perfData
                    ? `$${perfData.totalValue.toLocaleString('en-US', { minimumFractionDigits: 2 })}`
                    : (overview?.currentTspBalance ? `$${overview.currentTspBalance.toLocaleString('en-US', { minimumFractionDigits: 2 })}` : '$0.00')}
                </div>
              </div>
            </div>
          )}

          {/* Active Allocation Grid */}
          <div style={{ fontWeight: 700, fontSize: 14, marginBottom: 12, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
            <span>Current Fund Allocations</span>
            <span style={{ fontSize: 12, color: 'var(--clr-text-muted)' }}>Active Holdings</span>
          </div>

          {loading ? (
            <div className="loading-container"><div className="spinner" /></div>
          ) : overview?.currentAllocations.length === 0 ? (
            <div style={{ textAlign: 'center', padding: '24px 0', color: 'var(--clr-text-muted)', fontSize: 13 }}>
              No active allocations recorded yet. Click <strong>"Record a Move Made on tsp.gov"</strong> to input your first move.
            </div>
          ) : (
            <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(140px, 1fr))', gap: 10 }}>
              {overview?.currentAllocations.map(a => (
                <div key={a.fundName} style={{
                  background: 'var(--clr-surface-2)',
                  border: '1px solid var(--clr-border)',
                  borderRadius: 8,
                  padding: '8px 12px',
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between'
                }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, fontSize: 13, fontWeight: 600 }}>
                    <span style={{ width: 8, height: 8, borderRadius: '50%', background: FUND_COLORS[a.fundName] ?? '#64748b' }} />
                    {a.fundName}
                  </div>
                  <div style={{ fontWeight: 800, color: 'var(--clr-primary)', fontSize: 14 }}>
                    {a.percentage}%
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>

        {/* Holdings Breakdown Pie Chart Card */}
        <div className="card" style={{ minHeight: 330 }}>
          <div className="card-header">
            <div className="card-title"><PieChart size={16} style={{ display: 'inline', marginRight: 6 }} />Holdings Breakdown</div>
          </div>

          {pieData.length === 0 ? (
            <div style={{ textAlign: 'center', padding: 'var(--space-3xl)', color: 'var(--clr-text-muted)' }}>
              No active holdings
            </div>
          ) : (
            <ResponsiveContainer width="100%" height={260}>
              <RechartsPie>
                <Pie data={pieData} cx="50%" cy="50%" innerRadius={55} outerRadius={95} dataKey="value"
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
                <Legend wrapperStyle={{ fontSize: 11, color: 'var(--clr-text-muted)', paddingTop: 4 }} />
              </RechartsPie>
            </ResponsiveContainer>
          )}
        </div>
      </div>

      {/* Record Move Modal Drawer */}
      {showMoveModal && (
        <div
          style={{
            position: 'fixed',
            top: 0, left: 0, right: 0, bottom: 0,
            background: 'rgba(10, 14, 26, 0.82)',
            backdropFilter: 'blur(8px)',
            zIndex: 1000,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            padding: 16,
          }}
          onClick={e => { if (e.target === e.currentTarget) setShowMoveModal(false) }}
        >
          <div
            className="card"
            style={{
              width: '100%',
              maxWidth: 620,
              maxHeight: '90vh',
              overflowY: 'auto',
              border: '1px solid var(--clr-border-hover)',
              boxShadow: 'var(--shadow-lg)',
              animation: 'fadeIn 0.2s ease-out'
            }}
          >
            {/* Modal Header */}
            <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
              <div>
                <div className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
                  <Calendar size={20} style={{ color: 'var(--clr-primary)' }} />
                  Record a Move Made on tsp.gov
                </div>
                <div className="card-subtitle">
                  Enter target percentages for funds resulting from your move.
                </div>
              </div>
              <button
                onClick={() => setShowMoveModal(false)}
                style={{ background: 'transparent', border: 'none', color: 'var(--clr-text-muted)', cursor: 'pointer', padding: 4 }}
                title="Close"
              >
                <X size={22} />
              </button>
            </div>

            {/* Date & Description Fields */}
            <div className="grid-2" style={{ gap: 12, marginBottom: 16 }}>
              <div>
                <label style={{ fontSize: 12, fontWeight: 700, display: 'block', marginBottom: 4 }}>
                  Move Execution Date:
                </label>
                <input
                  type="date"
                  className="form-control"
                  value={moveDate}
                  onChange={e => setMoveDate(e.target.value)}
                  style={{ width: '100%', background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 6, padding: '8px 12px', color: 'var(--clr-text)', fontSize: 13 }}
                />
              </div>

              <div>
                <label style={{ fontSize: 12, fontWeight: 700, display: 'block', marginBottom: 4 }}>
                  Description / Notes (Optional):
                </label>
                <input
                  type="text"
                  className="form-control"
                  placeholder="e.g. 50% C / 50% S Rebalance"
                  value={moveDescription}
                  onChange={e => setMoveDescription(e.target.value)}
                  style={{ width: '100%', background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 6, padding: '8px 12px', color: 'var(--clr-text)', fontSize: 13 }}
                />
              </div>
            </div>

            {/* Quick Action Presets */}
            <div style={{ display: 'flex', gap: 8, flexWrap: 'wrap', marginBottom: 16 }}>
              {overview?.recommendedFund && (
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={handlePreFillRecommendation}
                  style={{ color: 'var(--clr-accent)', borderColor: 'var(--clr-accent)', fontSize: 12 }}
                >
                  <Sparkles size={13} /> AI Pick ({overview.recommendedFund})
                </button>
              )}
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => applyQuickPreset({ 'C Fund': 100 })}
                style={{ fontSize: 12 }}
              >
                100% C Fund
              </button>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => applyQuickPreset({ 'C Fund': 50, 'S Fund': 50 })}
                style={{ fontSize: 12 }}
              >
                50/50 C & S
              </button>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => applyQuickPreset({ 'G Fund': 100 })}
                style={{ fontSize: 12 }}
              >
                100% G Fund
              </button>
              <button
                className="btn btn-secondary btn-sm"
                onClick={() => applyQuickPreset({})}
                style={{ fontSize: 12, marginLeft: 'auto', color: 'var(--clr-text-muted)' }}
              >
                Clear
              </button>
            </div>

            {/* Core Funds Allocation Input Box Grid */}
            <div style={{ background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 10, padding: 14, marginBottom: 16 }}>
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 12 }}>
                <span style={{ fontSize: 13, fontWeight: 700 }}>Core Funds Allocation</span>
                <div className={`allocation-total ${isValidTotal ? 'valid' : 'invalid'}`}
                  style={{ padding: '3px 12px', borderRadius: 'var(--radius-full)', fontSize: 13, fontWeight: 800 }}>
                  Total: {totalPercentage.toFixed(0)}% / 100%
                </div>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))', gap: 10 }}>
                {CORE_FUNDS.map(fundName => {
                  const currentVal = formAllocations.find(a => a.fundName === fundName)?.percentage ?? 0
                  return (
                    <div key={fundName} style={{
                      background: 'var(--clr-surface)',
                      border: '1px solid var(--clr-border)',
                      borderRadius: 8,
                      padding: '10px 8px',
                      textAlign: 'center'
                    }}>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: 6, fontSize: 12, fontWeight: 700, marginBottom: 6 }}>
                        <span style={{ width: 8, height: 8, borderRadius: '50%', background: FUND_COLORS[fundName] ?? '#64748b' }} />
                        {fundName}
                      </div>
                      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                        <input
                          type="number"
                          min={0}
                          max={100}
                          value={currentVal === 0 ? '' : currentVal}
                          placeholder="0"
                          onChange={e => updatePercent(fundName, Math.min(100, Math.max(0, Number(e.target.value))))}
                          style={{
                            width: 54,
                            textAlign: 'center',
                            background: 'var(--clr-bg)',
                            border: '1px solid var(--clr-border)',
                            borderRadius: 6,
                            color: 'var(--clr-text)',
                            fontWeight: 800,
                            fontSize: 15,
                            padding: '4px'
                          }}
                        />
                        <span style={{ marginLeft: 2, fontSize: 13, fontWeight: 700, color: 'var(--clr-text-muted)' }}>%</span>
                      </div>
                    </div>
                  )
                })}
              </div>
            </div>

            {/* Lifecycle (L) Funds Collapsible Section */}
            <div style={{ marginBottom: 20 }}>
              <button
                type="button"
                onClick={() => setShowLFunds(!showLFunds)}
                style={{
                  background: 'transparent',
                  border: 'none',
                  color: 'var(--clr-primary)',
                  cursor: 'pointer',
                  fontSize: 12,
                  fontWeight: 600,
                  display: 'flex',
                  alignItems: 'center',
                  gap: 4,
                  padding: '4px 0'
                }}
              >
                {showLFunds ? <ChevronUp size={16} /> : <ChevronDown size={16} />}
                {showLFunds ? 'Hide Lifecycle (L) Funds' : '+ Show Lifecycle (L) Funds'}
              </button>

              {showLFunds && (
                <div style={{ background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 10, padding: 14, marginTop: 8 }}>
                  <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(100px, 1fr))', gap: 10 }}>
                    {L_FUNDS.map(fundName => {
                      const currentVal = formAllocations.find(a => a.fundName === fundName)?.percentage ?? 0
                      return (
                        <div key={fundName} style={{
                          background: 'var(--clr-surface)',
                          border: '1px solid var(--clr-border)',
                          borderRadius: 8,
                          padding: '8px',
                          textAlign: 'center'
                        }}>
                          <div style={{ fontSize: 11, fontWeight: 700, marginBottom: 4, color: 'var(--clr-text-muted)' }}>
                            {fundName}
                          </div>
                          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                            <input
                              type="number"
                              min={0}
                              max={100}
                              value={currentVal === 0 ? '' : currentVal}
                              placeholder="0"
                              onChange={e => updatePercent(fundName, Math.min(100, Math.max(0, Number(e.target.value))))}
                              style={{
                                width: 50,
                                textAlign: 'center',
                                background: 'var(--clr-bg)',
                                border: '1px solid var(--clr-border)',
                                borderRadius: 6,
                                color: 'var(--clr-text)',
                                fontWeight: 700,
                                fontSize: 13,
                                padding: '3px'
                              }}
                            />
                            <span style={{ marginLeft: 2, fontSize: 12, color: 'var(--clr-text-muted)' }}>%</span>
                          </div>
                        </div>
                      )
                    })}
                  </div>
                </div>
              )}
            </div>

            {/* Actions */}
            <div style={{ display: 'flex', gap: 12 }}>
              <button
                className="btn btn-secondary"
                onClick={() => setShowMoveModal(false)}
                style={{ flex: 1 }}
              >
                Cancel
              </button>
              <button
                className="btn btn-primary"
                onClick={handleRecordMove}
                disabled={saving || !isValidTotal || status?.remainingTransfers === 0}
                style={{ flex: 2, justifyContent: 'center', fontWeight: 700 }}
              >
                {saving ? <span className="spinner" style={{ width: 18, height: 18, borderWidth: 2 }} /> : <><Save size={18} /> Record Move & Update Tracking</>}
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Move History Table Section */}
      <div className="card" style={{ marginBottom: 'var(--space-xl)' }}>
        <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
          <div>
            <div className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <History size={18} style={{ color: 'var(--clr-primary)' }} />
              Move History & Monthly Log
            </div>
            <div className="card-subtitle">
              Historical record of all Interfund Transfer (IFT) moves logged by date.
            </div>
          </div>

          <button
            className="btn btn-primary btn-sm"
            onClick={() => setShowMoveModal(true)}
            style={{ display: 'flex', alignItems: 'center', gap: 6 }}
          >
            <PlusCircle size={15} /> Record Move
          </button>
        </div>

        {loading ? (
          <div className="loading-container"><div className="spinner" /></div>
        ) : !overview?.moveHistory || overview.moveHistory.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 'var(--space-3xl)', color: 'var(--clr-text-muted)' }}>
            <History size={40} style={{ margin: '0 auto 12px', opacity: 0.4 }} />
            <div>No move history recorded yet. Click <strong>"Record a Move Made on tsp.gov"</strong> above to log your first move.</div>
          </div>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table className="data-table">
              <thead>
                <tr>
                  <th>Date Executed</th>
                  <th>Monthly Move</th>
                  <th>Target Allocation</th>
                  <th>Balance at Move</th>
                  <th>Description / Notes</th>
                  <th style={{ textAlign: 'right' }}>Action</th>
                </tr>
              </thead>
              <tbody>
                {overview.moveHistory.map(move => (
                  <tr key={move.id}>
                    <td>
                      <strong>
                        {new Date(move.effectiveDate).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}
                      </strong>
                    </td>
                    <td>
                      <span className={`badge ${move.moveNumberInMonth === 3 ? 'badge-warning' : 'badge-primary'}`}>
                        Move #{move.moveNumberInMonth} ({move.monthKey})
                      </span>
                    </td>
                    <td>
                      <div style={{ display: 'flex', flexWrap: 'wrap', gap: 6 }}>
                        {move.allocations.map(a => (
                          <span key={a.fundName} style={{
                            background: 'var(--clr-surface-2)',
                            border: '1px solid var(--clr-border)',
                            borderRadius: 4,
                            padding: '2px 8px',
                            fontSize: 11,
                            fontWeight: 700
                          }}>
                            {a.fundName}: {a.percentage}%
                          </span>
                        ))}
                      </div>
                    </td>
                    <td>${move.balanceAtMove.toLocaleString('en-US', { minimumFractionDigits: 2 })}</td>
                    <td style={{ fontSize: 13, color: 'var(--clr-text-muted)' }}>{move.description || '—'}</td>
                    <td style={{ textAlign: 'right' }}>
                      <button
                        className="btn btn-secondary btn-sm"
                        onClick={() => handleDeleteMove(move.id)}
                        title="Delete move"
                        style={{ color: 'var(--clr-danger)', borderColor: 'rgba(239, 68, 68, 0.3)' }}
                      >
                        <Trash2 size={14} />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {/* Performance Chart Section */}
      <div className="card">
        <div className="card-header" style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12 }}>
          <div>
            <div className="card-title" style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <TrendingUp size={18} style={{ color: 'var(--clr-success)' }} />
              Performance Over Time (Based on Actual Moves)
            </div>
            <div className="card-subtitle">
              Tracks actual portfolio growth ($) and returns (%) calculated from your sequence of recorded moves.
            </div>
          </div>

          <div style={{ display: 'flex', gap: 6 }}>
            {RANGES.map(r => (
              <button
                key={r.label}
                className={`range-btn${perfDays === r.days ? ' active' : ''}`}
                onClick={() => setPerfDays(r.days)}
              >
                {r.label}
              </button>
            ))}
          </div>
        </div>

        {/* Performance Stat Row */}
        {perfData && (
          <div style={{ display: 'flex', gap: 24, padding: '12px 0 20px', borderBottom: '1px solid var(--clr-border)', marginBottom: 20 }}>
            <div>
              <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', fontWeight: 700, textTransform: 'uppercase' }}>Portfolio Value</div>
              <div style={{ fontSize: 24, fontWeight: 800, color: 'var(--clr-text)' }}>
                ${perfData.totalValue.toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
              </div>
            </div>

            <div style={{ borderLeft: '1px solid var(--clr-border)', paddingLeft: 24 }}>
              <div style={{ fontSize: 11, color: 'var(--clr-text-muted)', fontWeight: 700, textTransform: 'uppercase' }}>Total Return ($ / %)</div>
              <div style={{ fontSize: 24, fontWeight: 800, color: isPositiveGain ? 'var(--clr-success)' : 'var(--clr-danger)', display: 'flex', alignItems: 'center', gap: 6 }}>
                {isPositiveGain ? <TrendingUp size={20} /> : <TrendingDown size={20} />}
                {isPositiveGain ? '+' : ''}${Math.abs(perfData.totalGain).toLocaleString('en-US', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}
                <span style={{ fontSize: 14, opacity: 0.9 }}>({isPositiveGain ? '+' : ''}{perfData.totalGainPercent.toFixed(2)}%)</span>
              </div>
            </div>
          </div>
        )}

        {/* Area Chart */}
        {perfLoading ? (
          <div className="loading-container"><div className="spinner" /></div>
        ) : !perfData || perfData.history.length === 0 ? (
          <div style={{ textAlign: 'center', padding: 'var(--space-3xl)', color: 'var(--clr-text-muted)' }}>
            No performance data available for this range.
          </div>
        ) : (
          <ResponsiveContainer width="100%" height={340}>
            <AreaChart data={perfData.history} margin={{ top: 10, right: 20, left: 10, bottom: 5 }}>
              <defs>
                <linearGradient id="allocPerfGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor={isPositiveGain ? '#10b981' : '#ef4444'} stopOpacity={0.3} />
                  <stop offset="95%" stopColor={isPositiveGain ? '#10b981' : '#ef4444'} stopOpacity={0} />
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
                formatter={(v: number) => [`$${v.toLocaleString('en-US', { minimumFractionDigits: 2 })}`, 'Portfolio Value']}
                labelFormatter={(l: string) => new Date(l).toLocaleDateString('en-US', { weekday: 'short', year: 'numeric', month: 'short', day: 'numeric' })}
              />
              <Area type="monotone" dataKey="portfolioValue"
                stroke={isPositiveGain ? '#10b981' : '#ef4444'}
                strokeWidth={2.5}
                fill="url(#allocPerfGrad)"
                dot={false}
                activeDot={{ r: 5, fill: isPositiveGain ? '#10b981' : '#ef4444' }}
              />
            </AreaChart>
          </ResponsiveContainer>
        )}
      </div>
    </div>
  )
}
