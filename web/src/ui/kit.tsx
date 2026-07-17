import { useState } from 'react';
import type { CSSProperties, ReactNode } from 'react';

/* =========================================================================
   ui/kit — primitive hệ "Refined" (design_handoff_refined_frontend).
   Vanilla CSS (.rf-* trong styles/refined.css). KHÔNG import 'antd'.
   ========================================================================= */

export type Tone = 'accent' | 'success' | 'warning' | 'danger' | 'info' | 'muted' | 'purple';

/* ---- Icon: Material Symbols Outlined ---- */
export function Icon({ name, size = 20, className = '', style }: { name: string; size?: number; className?: string; style?: CSSProperties }) {
  return (
    <span className={`material-symbols-outlined ${className}`} style={{ fontSize: size, ...style }} aria-hidden>
      {name}
    </span>
  );
}

/* ---- Button ---- */
export function Button({
  children,
  onClick,
  variant = 'ghost',
  icon,
  size,
  disabled,
  type = 'button',
  className = '',
}: {
  children?: ReactNode;
  onClick?: () => void;
  variant?: 'primary' | 'ghost' | 'text' | 'danger' | 'link';
  icon?: string;
  size?: 'sm';
  disabled?: boolean;
  type?: 'button' | 'submit';
  className?: string;
}) {
  return (
    <button
      type={type}
      onClick={onClick}
      disabled={disabled}
      className={`rf-btn rf-btn--${variant} ${size === 'sm' ? 'rf-btn--sm' : ''} ${className}`}
    >
      {icon ? <Icon name={icon} size={18} /> : null}
      {children}
    </button>
  );
}

export function IconButton({ icon, onClick, title, size = 20 }: { icon: string; onClick?: () => void; title?: string; size?: number }) {
  return (
    <button type="button" className="rf-iconbtn" onClick={onClick} title={title} aria-label={title}>
      <Icon name={icon} size={size} />
    </button>
  );
}

/* ---- Card / DataCard ---- */
export function Card({ children, className = '', style }: { children: ReactNode; className?: string; style?: CSSProperties }) {
  return (
    <div className={`rf-card ${className}`} style={style}>
      {children}
    </div>
  );
}

/** DataCard — card có head (title + extra) và body. */
export function DataCard({
  title,
  extra,
  children,
  bodyless,
  className = '',
}: {
  title: ReactNode;
  extra?: ReactNode;
  children: ReactNode;
  /** true = không padding body (dùng cho bảng) */
  bodyless?: boolean;
  className?: string;
}) {
  return (
    <div className={`rf-card ${className}`}>
      <div className="rf-card__head">
        <div className="rf-card__title">{title}</div>
        {extra}
      </div>
      {bodyless ? children : <div className="rf-card__body">{children}</div>}
    </div>
  );
}

export function SectionTitle({ children }: { children: ReactNode }) {
  return <div className="rf-section">{children}</div>;
}

/* ---- Thẻ KPI (label trên + số mono theo màu semantic) ---- */
export function StatCard({
  label,
  value,
  tone,
  linkText,
  onLink,
}: {
  label: string;
  value: ReactNode;
  tone?: Tone;
  linkText?: string;
  onLink?: () => void;
}) {
  return (
    <div className="rf-stat">
      <div className="rf-stat__label">{label}</div>
      <div className={`rf-stat__value ${tone ? `rf-stat__value--${tone}` : ''}`}>{value}</div>
      {linkText ? (
        <span className="rf-stat__link" onClick={onLink}>
          {linkText} ›
        </span>
      ) : null}
    </div>
  );
}

/** Thẻ KPI có icon-chip vuông (Data khách hàng, thẻ vận hành). */
export function StatCardIcon({ icon, tone = 'accent', value, label }: { icon: string; tone?: Tone; value: ReactNode; label: string }) {
  return (
    <div className="rf-stat rf-stat--icon">
      <span className={`rf-stat__chip rf-chip--${tone}`}>
        <Icon name={icon} size={22} />
      </span>
      <div>
        <div className="rf-stat__num">{value}</div>
        <div className="rf-stat__sub">{label}</div>
      </div>
    </div>
  );
}

/* ---- Pill / StatusTag ---- */
export function Pill({ tone = 'muted', children }: { tone?: Tone; children: ReactNode }) {
  return <span className={`rf-pill rf-pill--${tone}`}>{children}</span>;
}
export { Pill as StatusTag };

/* ---- Seat pills: Tổng / Giữ / Bán / Còn ---- */
export function SeatTags({ total, hold, sold, left }: { total: number; hold: number; sold: number; left: number }) {
  return (
    <span className="rf-seat">
      <span className="rf-seat__total">{total}</span>
      <span className="rf-seat__hold">{hold}</span>
      <span className="rf-seat__sold">{sold}</span>
      <span className="rf-seat__left">{left}</span>
    </span>
  );
}

/* ---- Chip lọc nhanh ---- */
export function FilterChip({ active, onClick, count, children }: { active?: boolean; onClick?: () => void; count?: number; children: ReactNode }) {
  return (
    <button type="button" onClick={onClick} className={`rf-fchip ${active ? 'rf-fchip--active' : ''}`}>
      {children}
      {count != null ? <span className="rf-fchip__count">{count.toLocaleString('vi-VN')}</span> : null}
    </button>
  );
}

/* ---- Segment tabs ---- */
export type SegOption = { value: string; label: ReactNode; count?: number };
export function SegmentTabs({ value, onChange, options }: { value: string; onChange: (v: string) => void; options: SegOption[] }) {
  return (
    <div className="rf-segs">
      {options.map((o) => (
        <button key={o.value} type="button" onClick={() => onChange(o.value)} className={`rf-seg ${value === o.value ? 'rf-seg--active' : ''}`}>
          {o.label}
          {o.count != null ? <span className="rf-seg__count">{o.count.toLocaleString('vi-VN')}</span> : null}
        </button>
      ))}
    </div>
  );
}

/* ---- Tabs (gạch dưới) ---- */
export function Tabs({ items }: { items: { key: string; label: ReactNode; children: ReactNode }[] }) {
  const [active, setActive] = useState(items[0]?.key);
  const cur = items.find((t) => t.key === active) ?? items[0];
  return (
    <div>
      <div style={{ display: 'flex', gap: 2, borderBottom: '1px solid var(--tk-line)', padding: '0 6px' }}>
        {items.map((t) => (
          <button
            key={t.key}
            type="button"
            onClick={() => setActive(t.key)}
            style={{
              position: 'relative',
              border: 'none',
              background: 'transparent',
              cursor: 'pointer',
              padding: '10px 12px',
              font: '500 13px var(--tk-font)',
              color: active === t.key ? 'var(--tk-accent)' : 'var(--tk-muted-2)',
            }}
          >
            {t.label}
            {active === t.key ? (
              <span style={{ position: 'absolute', left: 8, right: 8, bottom: -1, height: 2, borderRadius: 2, background: 'var(--tk-accent)' }} />
            ) : null}
          </button>
        ))}
      </div>
      <div>{cur?.children}</div>
    </div>
  );
}

/* ---- Money cell: tổng (strong) + phụ (semantic) ---- */
export function MoneyCell({ main, sub, subTone = 'success' }: { main: ReactNode; sub?: ReactNode; subTone?: 'success' | 'danger' | 'muted' }) {
  const color = subTone === 'success' ? 'var(--tk-success)' : subTone === 'danger' ? 'var(--tk-danger)' : 'var(--tk-muted-2)';
  return (
    <div style={{ textAlign: 'right', lineHeight: 1.35 }}>
      <div className="rf-cell-mono rf-cell-strong">{main}</div>
      {sub != null ? <div className="rf-cell-sub" style={{ color }}>{sub}</div> : null}
    </div>
  );
}

/* ---- Bar (dòng tiền / phễu) ---- */
export function Bar({ pct, color }: { pct: number; color: string }) {
  return (
    <div className="rf-bar__track">
      <div className="rf-bar__fill" style={{ width: `${Math.max(0, Math.min(100, pct))}%`, background: color }} />
    </div>
  );
}

/* ---- Empty ---- */
export function Empty({ text }: { text: string }) {
  return <div style={{ padding: '40px 0', textAlign: 'center', fontSize: 13, color: 'var(--tk-muted)' }}>{text}</div>;
}
