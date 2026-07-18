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

/* ---- StatGrid: hàng thẻ KPI icon-chip DÙNG CHUNG (icon & tone tự suy từ nhãn) ----
   Mọi trang dùng <StatGrid items={[{label,value}]} /> -> đồng nhất icon-chip.
   Có thể override icon/tone từng thẻ khi cần. */
const STAT_ICON_RULES: [RegExp, string][] = [
  [/doanh thu|doanh số|thực thu|đã thu|tiền/i, 'payments'],
  [/còn nợ|công nợ|phải thu|phải chi|còn thiếu|nợ/i, 'account_balance_wallet'],
  [/chi phí|tổng chi|đã chi|chi tiền/i, 'trending_down'],
  [/lợi nhuận|lãi/i, 'trending_up'],
  [/vat|thuế|hoá đơn|hóa đơn/i, 'receipt_long'],
  [/hoa hồng/i, 'percent'],
  [/khách|người mua|hành khách|lead|cơ hội/i, 'groups'],
  [/đơn|order|lkh/i, 'shopping_cart'],
  [/tour|chuyến|khởi hành/i, 'flag'],
  [/vé/i, 'confirmation_number'],
  [/phòng|booking|khách sạn/i, 'hotel'],
  [/xe/i, 'directions_car'],
  [/hdv|hướng dẫn/i, 'badge'],
  [/nhà cung cấp|ncc|đại lý|đối tác/i, 'storefront'],
  [/công việc|task|dự án/i, 'checklist'],
  [/chiến dịch|marketing|gửi|email|zalo/i, 'campaign'],
  [/huỷ|hủy|từ chối|thất bại|quá hạn/i, 'cancel'],
  [/hoàn thành|đã duyệt|phát hành|thành công|đã chốt|đang hoạt động/i, 'check_circle'],
  [/chờ|nháp|đang|sắp|dự kiến/i, 'schedule'],
  [/quỹ|series|kho|tồn/i, 'inventory_2'],
];
function guessStatIcon(label: string): string {
  for (const [re, ic] of STAT_ICON_RULES) if (re.test(label)) return ic;
  return 'insights';
}
const STAT_TONE_RULES: [RegExp, Tone][] = [
  [/doanh thu|đã thu|hoàn thành|thành công|lợi nhuận|đã chốt|đang hoạt động/i, 'success'],
  [/còn nợ|công nợ|phải thu|còn thiếu|huỷ|hủy|từ chối|quá hạn|thiếu|ngừng/i, 'danger'],
  [/chi phí|tổng chi|đã chi|phải chi|cảnh báo/i, 'warning'],
  [/chờ|nháp|đang|sắp|dự kiến/i, 'info'],
];
function guessStatTone(label: string): Tone {
  for (const [re, t] of STAT_TONE_RULES) if (re.test(label)) return t;
  return 'accent';
}
export type StatItem = { label: string; value: ReactNode; icon?: string; tone?: Tone };
export function StatGrid({ items, min = 200, style }: { items: StatItem[]; min?: number; style?: CSSProperties }) {
  return (
    <div className="rf-grid" style={{ gridTemplateColumns: `repeat(auto-fit, minmax(${min}px, 1fr))`, ...style }}>
      {items.map((s, i) => (
        <StatCardIcon key={i} icon={s.icon ?? guessStatIcon(s.label)} tone={s.tone ?? guessStatTone(s.label)} value={s.value} label={s.label} />
      ))}
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
      <div style={{ paddingTop: 14 }}>{cur?.children}</div>
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
