import type { ReactNode } from 'react';

type Tone = 'accent' | 'success' | 'warning' | 'info' | 'danger';

// Thẻ thống kê chuẩn: icon chip + số mono + nhãn + xu hướng. Không inline style.
export function StatCard({
  icon,
  tone = 'accent',
  value,
  label,
  trend,
}: {
  icon: ReactNode;
  tone?: Tone;
  value: ReactNode;
  label: string;
  trend?: { dir: 'up' | 'down'; text: string };
}) {
  return (
    <div className="tk-stat">
      <div className="tk-stat__top">
        <span className={`tk-stat__chip tk-chip--${tone}`}>{icon}</span>
        {trend ? (
          <span className={`tk-trend tk-trend--${trend.dir}`}>{trend.text}</span>
        ) : null}
      </div>
      <div className="tk-stat__value">{value}</div>
      <div className="tk-stat__label">{label}</div>
    </div>
  );
}

// Hàng thẻ: <StatRow cols={5}>...</StatRow> (đặt --tk-stat-cols)
export function StatRow({ cols = 5, children }: { cols?: number; children: ReactNode }) {
  return (
    <div className="tk-stats" style={{ ['--tk-stat-cols' as string]: cols }}>
      {children}
    </div>
  );
}
