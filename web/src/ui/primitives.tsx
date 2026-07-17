import type { CSSProperties, ReactNode } from 'react';
import { useState } from 'react';
import { Icon } from './kit';

/* =========================================================================
   ui/primitives — các primitive hệ "Refined" thay thế AntD:
   Typography (Text/Title/Link), Tag, Statistic, Space/Row/Col, Descriptions,
   Alert, Tooltip, Badge, Avatar, Divider, Progress, Checkbox.
   Giữ TÊN + PROPS gần AntD nhất có thể để feature page ít phải sửa.
   ========================================================================= */

/* ---- Tag: nhận tên màu preset của AntD (green/blue/red/gold/…) ---- */
const TAG_COLOR: Record<string, { fg: string; bg: string }> = {
  default: { fg: 'var(--tk-body)', bg: 'var(--tk-nav-hover)' },
  green: { fg: 'var(--tk-success)', bg: 'var(--tk-success-soft)' },
  success: { fg: 'var(--tk-success)', bg: 'var(--tk-success-soft)' },
  red: { fg: 'var(--tk-danger)', bg: 'var(--tk-danger-soft)' },
  error: { fg: 'var(--tk-danger)', bg: 'var(--tk-danger-soft)' },
  volcano: { fg: 'var(--tk-danger)', bg: 'var(--tk-danger-soft)' },
  gold: { fg: 'var(--tk-warning)', bg: 'var(--tk-warning-soft)' },
  orange: { fg: 'var(--tk-accent)', bg: 'var(--tk-accent-soft)' },
  warning: { fg: 'var(--tk-warning)', bg: 'var(--tk-warning-soft)' },
  blue: { fg: 'var(--tk-info)', bg: 'var(--tk-info-soft)' },
  processing: { fg: 'var(--tk-info)', bg: 'var(--tk-info-soft)' },
  cyan: { fg: 'var(--tk-info)', bg: 'var(--tk-info-soft)' },
  geekblue: { fg: 'var(--tk-info)', bg: 'var(--tk-info-soft)' },
  purple: { fg: 'var(--tk-purple)', bg: 'var(--tk-purple-soft)' },
  magenta: { fg: 'var(--tk-purple)', bg: 'var(--tk-purple-soft)' },
};

export function Tag({
  color = 'default',
  children,
  style,
  title,
  icon,
}: {
  color?: string;
  children?: ReactNode;
  style?: CSSProperties;
  title?: string;
  icon?: string;
}) {
  const c = TAG_COLOR[color] ?? TAG_COLOR.default!;
  return (
    <span className="rf-tag" title={title} style={{ color: c.fg, background: c.bg, ...style }}>
      {icon ? <Icon name={icon} size={14} /> : null}
      {children}
    </span>
  );
}

/* ---- Typography ---- */
export function Text({
  children,
  type,
  strong,
  mono,
  size = 13,
  style,
}: {
  children: ReactNode;
  type?: 'secondary' | 'success' | 'warning' | 'danger';
  strong?: boolean;
  mono?: boolean;
  size?: number;
  style?: CSSProperties;
}) {
  const color =
    type === 'secondary'
      ? 'var(--tk-muted-2)'
      : type === 'success'
        ? 'var(--tk-success)'
        : type === 'warning'
          ? 'var(--tk-warning)'
          : type === 'danger'
            ? 'var(--tk-danger)'
            : 'var(--tk-body)';
  return (
    <span style={{ fontSize: size, fontWeight: strong ? 600 : 400, fontFamily: mono ? 'var(--tk-font-mono)' : undefined, color, ...style }}>{children}</span>
  );
}

/** Title — level 1..5 theo thang typography của handoff. */
const TITLE_SIZE: Record<number, number> = { 1: 23, 2: 19, 3: 16, 4: 14.5, 5: 13 };
export function Title({ children, level = 3, style }: { children: ReactNode; level?: 1 | 2 | 3 | 4 | 5; style?: CSSProperties }) {
  const Tag_ = `h${level}` as 'h1';
  return (
    <Tag_ style={{ margin: 0, font: `700 ${TITLE_SIZE[level]}px var(--tk-font)`, letterSpacing: '-0.02em', color: 'var(--tk-heading)', ...style }}>{children}</Tag_>
  );
}

export function Link({ children, onClick, href, style }: { children: ReactNode; onClick?: () => void; href?: string; style?: CSSProperties }) {
  return (
    <a
      href={href ?? '#'}
      onClick={(e) => {
        if (!href) e.preventDefault();
        onClick?.();
      }}
      style={{ color: 'var(--tk-accent)', fontSize: 13, textDecoration: 'none', cursor: 'pointer', ...style }}
    >
      {children}
    </a>
  );
}

export const Typography = { Text, Title, Link };

/* ---- Statistic ---- */
export function Statistic({
  title,
  value,
  precision = 0,
  suffix,
  prefix,
  valueStyle,
}: {
  title?: ReactNode;
  value: number | string;
  precision?: number;
  suffix?: ReactNode;
  prefix?: ReactNode;
  valueStyle?: CSSProperties;
}) {
  const shown = typeof value === 'number' ? value.toLocaleString('vi-VN', { minimumFractionDigits: precision, maximumFractionDigits: precision }) : value;
  return (
    <div>
      {title ? <div className="rf-stat__label">{title}</div> : null}
      <div style={{ font: '700 22px var(--tk-font-mono)', color: 'var(--tk-heading)', letterSpacing: '-0.02em', marginTop: 6, ...valueStyle }}>
        {prefix} {shown} {suffix ? <span style={{ fontSize: 13, fontWeight: 500, color: 'var(--tk-muted-2)' }}>{suffix}</span> : null}
      </div>
    </div>
  );
}

/* ---- Layout: Space / Row / Col ---- */
export function Space({
  children,
  size = 8,
  direction = 'horizontal',
  wrap,
  align,
  style,
}: {
  children: ReactNode;
  size?: number;
  direction?: 'horizontal' | 'vertical';
  wrap?: boolean;
  align?: 'start' | 'center' | 'end';
  style?: CSSProperties;
}) {
  return (
    <div
      style={{
        display: 'flex',
        flexDirection: direction === 'vertical' ? 'column' : 'row',
        gap: size,
        flexWrap: wrap ? 'wrap' : undefined,
        alignItems: align ? (align === 'start' ? 'flex-start' : align === 'end' ? 'flex-end' : 'center') : direction === 'horizontal' ? 'center' : undefined,
        ...style,
      }}
    >
      {children}
    </div>
  );
}

/** Row/Col — lưới 24 cột kiểu AntD, dựng bằng CSS grid. */
export function Row({ children, gutter = 0, style }: { children: ReactNode; gutter?: number | [number, number]; style?: CSSProperties }) {
  const [gx, gy] = Array.isArray(gutter) ? gutter : [gutter, gutter];
  return <div style={{ display: 'grid', gridTemplateColumns: 'repeat(24, 1fr)', columnGap: gx, rowGap: gy, ...style }}>{children}</div>;
}

export function Col({ children, span = 24, style }: { children: ReactNode; span?: number; style?: CSSProperties }) {
  return <div style={{ gridColumn: `span ${Math.min(24, Math.max(1, span))}`, minWidth: 0, ...style }}>{children}</div>;
}

/* ---- Descriptions ---- */
export function Descriptions({ items }: { items: { label: ReactNode; value: ReactNode }[] }) {
  return (
    <div className="rf-desc">
      {items.map((it, i) => (
        <div className="rf-desc__row" key={i}>
          <div className="rf-desc__label">{it.label}</div>
          <div className="rf-desc__val">{it.value}</div>
        </div>
      ))}
    </div>
  );
}

/* ---- Alert ---- */
const ALERT: Record<string, { fg: string; bg: string; icon: string }> = {
  success: { fg: 'var(--tk-success)', bg: 'var(--tk-success-soft)', icon: 'check_circle' },
  info: { fg: 'var(--tk-info)', bg: 'var(--tk-info-soft)', icon: 'info' },
  warning: { fg: 'var(--tk-warning)', bg: 'var(--tk-warning-soft)', icon: 'warning' },
  error: { fg: 'var(--tk-danger)', bg: 'var(--tk-danger-soft)', icon: 'error' },
};

export function Alert({
  type = 'info',
  message,
  description,
  style,
}: {
  type?: 'success' | 'info' | 'warning' | 'error';
  message: ReactNode;
  description?: ReactNode;
  style?: CSSProperties;
}) {
  const a = ALERT[type]!;
  return (
    <div className="rf-alert" style={{ background: a.bg, color: 'var(--tk-text-strong)', ...style }}>
      <Icon name={a.icon} size={18} style={{ color: a.fg, flexShrink: 0, marginTop: 1 }} />
      <div>
        <div className={description ? 'rf-alert__title' : undefined}>{message}</div>
        {description ? <div style={{ marginTop: 3, color: 'var(--tk-body)' }}>{description}</div> : null}
      </div>
    </div>
  );
}

/* ---- Tooltip (hover, bong bóng trên) ---- */
export function Tooltip({ title, children }: { title: ReactNode; children: ReactNode }) {
  const [on, setOn] = useState(false);
  if (!title) return <>{children}</>;
  return (
    <span className="rf-tip" onMouseEnter={() => setOn(true)} onMouseLeave={() => setOn(false)}>
      {children}
      {on ? <span className="rf-tip__bub">{title}</span> : null}
    </span>
  );
}

/* ---- Badge ---- */
export function Badge({ count, dot, max = 99, children }: { count?: number; dot?: boolean; max?: number; children: ReactNode }) {
  const show = dot || (count ?? 0) > 0;
  return (
    <span className="rf-badge">
      {children}
      {show ? (
        <span className="rf-badge__dot" style={dot ? { minWidth: 8, height: 8, padding: 0 } : undefined}>
          {dot ? '' : (count ?? 0) > max ? `${max}+` : count}
        </span>
      ) : null}
    </span>
  );
}

/* ---- Avatar ---- */
export function Avatar({ children, src, size = 34, style }: { children?: ReactNode; src?: string; size?: number; style?: CSSProperties }) {
  return (
    <span className="rf-avatar" style={{ width: size, height: size, fontSize: Math.round(size * 0.38), ...style }}>
      {src ? <img src={src} alt="" /> : children}
    </span>
  );
}

/* ---- Divider ---- */
export function Divider({ vertical, style }: { vertical?: boolean; style?: CSSProperties }) {
  return <span className={vertical ? 'rf-divider rf-divider--v' : 'rf-divider'} style={style} role="separator" />;
}

/* ---- Progress ---- */
export function Progress({ percent, tone, showInfo = true }: { percent: number; tone?: string; showInfo?: boolean }) {
  const p = Math.min(100, Math.max(0, percent));
  return (
    <div style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
      <div className="rf-progress__track" style={{ flex: 1 }}>
        <div className="rf-progress__fill" style={{ width: `${p}%`, background: tone }} />
      </div>
      {showInfo ? <span style={{ font: '600 11.5px var(--tk-font-mono)', color: 'var(--tk-muted-2)', minWidth: 34, textAlign: 'right' }}>{Math.round(p)}%</span> : null}
    </div>
  );
}

/* ---- Checkbox ---- */
export function Checkbox({ checked, onChange, children }: { checked: boolean; onChange: (v: boolean) => void; children?: ReactNode }) {
  return (
    <label className="rf-check">
      <input type="checkbox" checked={checked} onChange={(e) => onChange(e.target.checked)} />
      {children}
    </label>
  );
}
