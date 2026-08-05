import { LineChart, Line, ResponsiveContainer, Tooltip } from 'recharts'

interface SparklineProps {
  data: { date: string; price: number }[]
  color: string
  positive: boolean
}

export function SparklineChart({ data, color, positive }: SparklineProps) {
  const strokeColor = positive ? color : '#ef4444'
  return (
    <ResponsiveContainer width="100%" height={40}>
      <LineChart data={data}>
        <Line
          type="monotone"
          dataKey="price"
          stroke={strokeColor}
          strokeWidth={1.5}
          dot={false}
        />
        <Tooltip
          contentStyle={{ background: 'var(--clr-surface-2)', border: '1px solid var(--clr-border)', borderRadius: 6, fontSize: 11 }}
          labelStyle={{ color: 'var(--clr-text-muted)' }}
          itemStyle={{ color: strokeColor }}
          formatter={(v: number) => [`$${v.toFixed(4)}`, 'Price']}
          labelFormatter={(l: string) => new Date(l).toLocaleDateString()}
        />
      </LineChart>
    </ResponsiveContainer>
  )
}
