import { useState } from 'react';
import type { ReactNode } from 'react';

/* =========================================================================
   ui/kit — bộ component AntD-FREE (Tailwind + inline cho gradient động).
   Dùng cho pilot màn Dashboard. KHÔNG import 'antd'.
   ========================================================================= */

export const GRADIENTS = {
  orange: 'linear-gradient(135deg,#eb5324 0%,#ff7a45 100%)',
  blue: 'linear-gradient(135deg,#4e7bff 0%,#7c5cff 100%)',
  green: 'linear-gradient(135deg,#22c55e 0%,#0ea5a5 100%)',
  purple: 'linear-gradient(135deg,#a855f7 0%,#ec4899 100%)',
  pink: 'linear-gradient(135deg,#f43f5e 0%,#fb923c 100%)',
} as const;
export type GradKey = keyof typeof GRADIENTS;
const ORDER: GradKey[] = ['orange', 'blue', 'green', 'purple'];
export const gradOf = (i: number): string => GRADIENTS[ORDER[i % ORDER.length] ?? 'orange'];

/* ---- Card ---- */
export function Card({ className = '', children }: { className?: string; children: ReactNode }) {
  return (
    <div className={`bg-white rounded-[18px] shadow-[0_8px_24px_-10px_rgba(34,41,47,0.12)] ${className}`}>{children}</div>
  );
}
export function CardHead({ icon, title, extra }: { icon?: ReactNode; title: ReactNode; extra?: ReactNode }) {
  return (
    <div className="flex items-center justify-between px-[18px] py-[14px] border-b border-[#f1eff5]">
      <div className="flex items-center gap-2 font-semibold text-[15px] text-[#5e5873]">
        {icon}
        <span>{title}</span>
      </div>
      {extra}
    </div>
  );
}

/* ---- Thẻ KPI gradient ---- */
export function GradientStat({
  icon,
  gradient,
  value,
  label,
}: {
  icon?: ReactNode;
  gradient: GradKey;
  value: ReactNode;
  label: string;
}) {
  const shadow: Record<GradKey, string> = {
    orange: '0 12px 26px -10px rgba(235,83,36,.45)',
    blue: '0 12px 26px -10px rgba(78,123,255,.5)',
    green: '0 12px 26px -10px rgba(16,185,129,.45)',
    purple: '0 12px 26px -10px rgba(168,85,247,.5)',
    pink: '0 12px 26px -10px rgba(244,63,94,.5)',
  };
  return (
    <div
      className="relative overflow-hidden rounded-[20px] px-[22px] py-5 text-white transition-transform duration-150 hover:-translate-y-0.5"
      style={{ background: GRADIENTS[gradient], boxShadow: shadow[gradient] }}
    >
      <div
        className="pointer-events-none absolute inset-0"
        style={{ background: 'radial-gradient(120% 120% at 100% 0%, rgba(255,255,255,.25), transparent 55%)' }}
      />
      <div className="relative z-10 flex items-center justify-between">
        {icon ? (
          <span className="flex h-11 w-11 items-center justify-center rounded-[13px] border border-white/25 bg-white/20 text-xl text-white backdrop-blur">
            {icon}
          </span>
        ) : (
          <span />
        )}
      </div>
      <div className="relative z-10 mt-[18px] font-mono text-[30px] font-bold leading-[1.05] tracking-[-0.03em]">{value}</div>
      <div className="relative z-10 mt-[5px] text-[13px] font-medium opacity-90">{label}</div>
    </div>
  );
}

/* ---- Avatar gradient (tròn) ---- */
export function GradAvatar({ i, children, size = 36 }: { i: number; children: ReactNode; size?: number }) {
  return (
    <span
      className="inline-flex shrink-0 items-center justify-center rounded-full font-mono font-bold text-white"
      style={{
        width: size,
        height: size,
        fontSize: size > 40 ? 22 : 13,
        background: gradOf(i),
        boxShadow: '0 4px 10px -3px rgba(0,0,0,.25)',
      }}
    >
      {children}
    </span>
  );
}

/* ---- Pill trạng thái ---- */
const PILL_TONE: Record<string, string> = {
  success: 'bg-[#eafbe7] text-[#1f9d57]',
  warning: 'bg-[#fff3e6] text-[#d9821f]',
  danger: 'bg-[#fbeae1] text-[#d1494a]',
  info: 'bg-[#e6f0ff] text-[#3f6bd6]',
  muted: 'bg-[#f0eef4] text-[#6e6b7b]',
};
export function Pill({ tone = 'muted', children }: { tone?: keyof typeof PILL_TONE; children: ReactNode }) {
  return (
    <span className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[12px] font-medium ${PILL_TONE[tone]}`}>
      {children}
    </span>
  );
}

/* ---- Button ---- */
export function Btn({
  children,
  onClick,
  variant = 'ghost',
  iconColor,
  className = '',
}: {
  children: ReactNode;
  onClick?: () => void;
  variant?: 'ghost' | 'primary' | 'link';
  iconColor?: string;
  className?: string;
}) {
  const base = 'inline-flex items-center gap-2 rounded-[9px] text-[13px] font-medium transition-colors cursor-pointer';
  const styles: Record<string, string> = {
    ghost: 'h-9 px-3 border border-[#e6e3ee] bg-white text-[#6e6b7b] hover:border-[#d3cfe0] hover:text-[#5e5873]',
    primary: 'h-9 px-4 text-white',
    link: 'text-[#eb5324] hover:opacity-80',
  };
  return (
    <button
      type="button"
      onClick={onClick}
      className={`${base} ${styles[variant]} ${className}`}
      style={variant === 'primary' ? { background: GRADIENTS.orange } : undefined}
    >
      {iconColor ? <span style={{ color: iconColor }}>{/* icon slot handled by children */}</span> : null}
      {children}
    </button>
  );
}

/* ---- Tabs (controlled đơn giản) ---- */
export function Tabs({ items }: { items: { key: string; label: ReactNode; children: ReactNode }[] }) {
  const [active, setActive] = useState(items[0]?.key);
  const cur = items.find((t) => t.key === active) ?? items[0];
  return (
    <div>
      <div className="flex gap-1 border-b border-[#f1eff5] px-1">
        {items.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setActive(t.key)}
            className={`relative px-3 py-2.5 text-[13px] font-medium transition-colors ${
              active === t.key ? 'text-[#eb5324]' : 'text-[#8b899a] hover:text-[#5e5873]'
            }`}
          >
            {t.label}
            {active === t.key ? <span className="absolute inset-x-2 -bottom-px h-0.5 rounded bg-[#eb5324]" /> : null}
          </button>
        ))}
      </div>
      <div>{cur?.children}</div>
    </div>
  );
}

/* ---- Empty ---- */
export function Empty({ text }: { text: string }) {
  return <div className="py-10 text-center text-[13px] text-[#a8a5b5]">{text}</div>;
}
