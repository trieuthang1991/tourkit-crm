import * as React from 'react';
import { cn } from './cn';
import { Card } from './card';

export type StatTone = 'brand' | 'success' | 'warning' | 'danger' | 'info' | 'neutral';

const toneText: Record<StatTone, string> = {
  brand: 'text-brand',
  success: 'text-emerald-600',
  warning: 'text-amber-600',
  danger: 'text-red-600',
  info: 'text-sky-600',
  neutral: 'text-ink',
};

/** Thẻ thống kê đầu màn — bám statCards hệ cũ (nhãn + số + icon). */
export function StatCard({
  label,
  value,
  icon,
  tone = 'neutral',
  className,
}: {
  label: string;
  value: React.ReactNode;
  icon?: React.ReactNode;
  tone?: StatTone;
  className?: string;
}) {
  return (
    <Card className={cn('h-full', className)}>
      <div className="flex items-center gap-3 px-5 py-4">
        {icon && (
          <div className={cn('flex size-10 shrink-0 items-center justify-center rounded-lg bg-canvas', toneText[tone])}>
            {icon}
          </div>
        )}
        <div className="min-w-0">
          <div className="truncate text-xs text-muted">{label}</div>
          <div className={cn('text-xl font-semibold tabular-nums', toneText[tone])}>{value}</div>
        </div>
      </div>
    </Card>
  );
}

/** Hàng thẻ thống kê responsive: 2 cột mobile → 3 (md) → 5 (xl). */
export function StatRow({ children, className }: { children: React.ReactNode; className?: string }) {
  return <div className={cn('mb-4 grid grid-cols-2 gap-4 md:grid-cols-3 xl:grid-cols-5', className)}>{children}</div>;
}
