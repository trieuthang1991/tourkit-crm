import type { ReactNode } from 'react';

type Tone = 'accent' | 'success' | 'warning' | 'info' | 'danger';

// Thẻ thống kê chuẩn: (icon chip) + số mono + nhãn + xu hướng + footer. Không inline style.
// icon & footer tuỳ chọn — dùng chung cho KPI đơn giản (không icon) lẫn thẻ có icon-chip.
export function StatCard({
  icon,
  tone = 'accent',
  value,
  label,
  trend,
  footer,
}: {
  icon?: ReactNode;
  tone?: Tone;
  value: ReactNode;
  label: string;
  trend?: { dir: 'up' | 'down'; text: string };
  footer?: ReactNode;
}) {
  return (
    <div className="tk-stat">
      {icon || trend ? (
        <div className="tk-stat__top">
          {icon ? <span className={`tk-stat__chip tk-chip--${tone}`}>{icon}</span> : <span />}
          {trend ? <span className={`tk-trend tk-trend--${trend.dir}`}>{trend.text}</span> : null}
        </div>
      ) : null}
      <div className="tk-stat__value">{value}</div>
      <div className="tk-stat__label">{label}</div>
      {footer ? <div className="tk-stat__footer">{footer}</div> : null}
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
