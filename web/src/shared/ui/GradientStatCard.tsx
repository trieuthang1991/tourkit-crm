import type { ReactNode } from 'react';

// Bảng gradient rực rỡ (phối hài hoà quanh accent cam thương hiệu).
export const STAT_GRADIENTS = {
  orange: 'linear-gradient(135deg, #eb5324 0%, #ff7a45 100%)',
  blue: 'linear-gradient(135deg, #4e7bff 0%, #7c5cff 100%)',
  green: 'linear-gradient(135deg, #22c55e 0%, #0ea5a5 100%)',
  purple: 'linear-gradient(135deg, #a855f7 0%, #ec4899 100%)',
} as const;

export type StatGradient = keyof typeof STAT_GRADIENTS;

/**
 * Thẻ KPI nền GRADIENT rực rỡ — icon kính mờ, số mono trắng đậm, badge xu hướng.
 * Dùng cho hàng KPI nổi bật (hướng thiết kế "rực rỡ/gradient").
 */
export function GradientStatCard({
  icon,
  gradient,
  value,
  label,
  trend,
}: {
  icon?: ReactNode;
  gradient: StatGradient;
  value: ReactNode;
  label: string;
  trend?: string;
}) {
  return (
    <div className={`tk-gstat tk-gstat--${gradient}`} style={{ background: STAT_GRADIENTS[gradient] }}>
      <div className="tk-gstat__top">
        {icon ? <span className="tk-gstat__chip">{icon}</span> : <span />}
        {trend ? <span className="tk-gstat__trend">{trend}</span> : null}
      </div>
      <div className="tk-gstat__value">{value}</div>
      <div className="tk-gstat__label">{label}</div>
    </div>
  );
}
