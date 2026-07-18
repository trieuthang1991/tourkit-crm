import { useEffect, useMemo, useRef, useState } from 'react';
import type { CSSProperties } from 'react';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { Icon } from './kit';

/* =========================================================================
   ui/inputs — control nhập liệu hệ "Refined" (.rf-field / .rf-select). KHÔNG antd.
   ========================================================================= */

export function Input({
  value,
  onChange,
  placeholder,
  icon,
  onEnter,
  filled,
  style,
}: {
  value: string;
  onChange: (v: string) => void;
  placeholder?: string;
  icon?: string;
  onEnter?: () => void;
  filled?: boolean;
  style?: CSSProperties;
}) {
  return (
    <div className={`rf-field ${filled ? 'rf-field--filled' : ''}`} style={style}>
      {icon ? <Icon name={icon} size={18} /> : null}
      <input
        value={value}
        placeholder={placeholder}
        onChange={(e) => onChange(e.target.value)}
        onKeyDown={(e) => e.key === 'Enter' && onEnter?.()}
      />
      {value ? (
        <button
          type="button"
          onClick={() => onChange('')}
          style={{ border: 'none', background: 'transparent', cursor: 'pointer', color: 'var(--tk-muted)', lineHeight: 1 }}
          aria-label="Xoá"
        >
          ✕
        </button>
      ) : null}
    </div>
  );
}

/** Ô tìm kiếm (icon search trái). filled = kiểu topbar. */
export function SearchInput(props: Omit<Parameters<typeof Input>[0], 'icon'>) {
  return <Input {...props} icon="search" />;
}

export function Textarea({ value, onChange, placeholder, rows = 4 }: { value: string; onChange: (v: string) => void; placeholder?: string; rows?: number }) {
  return (
    <textarea
      rows={rows}
      value={value}
      placeholder={placeholder}
      onChange={(e) => onChange(e.target.value)}
      style={{
        width: '100%',
        padding: '9px 11px',
        borderRadius: 'var(--tk-radius)',
        border: '1px solid var(--tk-border)',
        background: 'var(--tk-surface)',
        font: '400 13px var(--tk-font)',
        color: 'var(--tk-heading)',
        outline: 'none',
        resize: 'vertical',
        lineHeight: 1.6,
      }}
    />
  );
}

export function NumberInput({ value, onChange, placeholder, min }: { value: number | null; onChange: (v: number | null) => void; placeholder?: string; min?: number }) {
  return (
    <div className="rf-field">
      <input
        type="number"
        inputMode="decimal"
        style={{ fontFamily: 'var(--tk-font-mono)' }}
        value={value ?? ''}
        min={min}
        placeholder={placeholder ?? '0'}
        onChange={(e) => onChange(e.target.value === '' ? null : Number(e.target.value))}
      />
    </div>
  );
}

export function DateInput({ value, onChange }: { value: string | null; onChange: (v: string | null) => void }) {
  const asDate = value ? new Date(value).toISOString().slice(0, 10) : '';
  return (
    <div className="rf-field">
      <input
        type="date"
        style={{ fontFamily: 'var(--tk-font-mono)' }}
        value={asDate}
        onChange={(e) => onChange(e.target.value ? new Date(e.target.value + 'T00:00:00').toISOString() : null)}
      />
    </div>
  );
}

const RF_WD = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

/** Date-range picker THẬT: 1 field + lịch popover chọn khoảng ngày. Trả về ISO đầu/cuối ngày. */
export function DateRangeInput({
  from,
  to,
  onChange,
  placeholder = ['Từ ngày', 'đến'],
  alignRight = false,
}: {
  from: string | null | undefined;
  to: string | null | undefined;
  onChange: (from: string | undefined, to: string | undefined) => void;
  placeholder?: [string, string];
  alignRight?: boolean;
}) {
  const fromD = from ? dayjs(from) : null;
  const toD = to ? dayjs(to) : null;
  const [open, setOpen] = useState(false);
  const [view, setView] = useState<Dayjs>(() => (fromD ?? dayjs()).startOf('month'));
  const [start, setStart] = useState<Dayjs | null>(fromD);
  const [end, setEnd] = useState<Dayjs | null>(toD);
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    setStart(from ? dayjs(from) : null);
    setEnd(to ? dayjs(to) : null);
    setView((from ? dayjs(from) : dayjs()).startOf('month'));
    const onDoc = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', onDoc);
    return () => document.removeEventListener('mousedown', onDoc);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [open]);

  const firstDow = (view.day() + 6) % 7;
  const gridStart = view.subtract(firstDow, 'day');
  const cells = Array.from({ length: 42 }, (_, i) => gridStart.add(i, 'day'));
  const today = dayjs();
  const label = fromD && toD ? `${fromD.format('DD/MM/YYYY')} → ${toD.format('DD/MM/YYYY')}` : '';

  const clickDay = (dd: Dayjs) => {
    if (!start || (start && end)) {
      setStart(dd);
      setEnd(null);
      return;
    }
    const [s, e] = dd.isBefore(start, 'day') ? [dd, start] : [start, dd];
    setStart(s);
    setEnd(e);
    onChange(s.startOf('day').toISOString(), e.endOf('day').toISOString());
    setOpen(false);
  };
  const clearAll = (e?: React.MouseEvent) => {
    e?.stopPropagation();
    setStart(null);
    setEnd(null);
    onChange(undefined, undefined);
  };

  return (
    <div className={`rf-dp ${open ? 'rf-dp--open' : ''}`} ref={ref}>
      <button type="button" className="rf-dp__trigger" onClick={() => setOpen((o) => !o)}>
        <Icon name="calendar_month" size={18} className="rf-daterange__ic" />
        {label ? (
          <span className="rf-dp__label">{label}</span>
        ) : (
          <span className="rf-dp__label rf-dp__label--ph">{`${placeholder[0]} → ${placeholder[1]}`}</span>
        )}
        {label ? (
          <span className="rf-dp__clear" onClick={clearAll} title="Xoá">
            <Icon name="close" size={16} />
          </span>
        ) : (
          <Icon name="expand_more" size={18} style={{ color: 'var(--tk-muted)' }} />
        )}
      </button>
      {open ? (
        <div className={`rf-dp__pop ${alignRight ? 'rf-dp__pop--right' : ''}`}>
          <div className="rf-dp__head">
            <button type="button" className="rf-dp__nav" onClick={() => setView((v) => v.subtract(1, 'month'))} aria-label="Tháng trước">
              <Icon name="chevron_left" size={18} />
            </button>
            <span className="rf-dp__title">{`Tháng ${view.month() + 1}, ${view.year()}`}</span>
            <button type="button" className="rf-dp__nav" onClick={() => setView((v) => v.add(1, 'month'))} aria-label="Tháng sau">
              <Icon name="chevron_right" size={18} />
            </button>
          </div>
          <div className="rf-dp__wd">
            {RF_WD.map((w) => (
              <span key={w}>{w}</span>
            ))}
          </div>
          <div className="rf-dp__grid">
            {cells.map((dd) => {
              const out = dd.month() !== view.month();
              const isStart = !!start && dd.isSame(start, 'day');
              const isEnd = !!end && dd.isSame(end, 'day');
              const inRange = !!start && !!end && dd.isAfter(start, 'day') && dd.isBefore(end, 'day');
              const cls = ['rf-dp__day'];
              if (out) cls.push('rf-dp__day--out');
              if (dd.isSame(today, 'day')) cls.push('rf-dp__day--today');
              if (inRange) cls.push('rf-dp__day--in');
              if (isStart) cls.push('rf-dp__day--edge', 'rf-dp__day--start');
              if (isEnd) cls.push('rf-dp__day--edge', 'rf-dp__day--end');
              return (
                <button type="button" key={dd.format('YYYY-MM-DD')} className={cls.join(' ')} onClick={() => clickDay(dd)}>
                  {dd.date()}
                </button>
              );
            })}
          </div>
          <div className="rf-dp__foot">
            <span className="rf-dp__hint">{start && !end ? 'Chọn ngày kết thúc' : 'Chọn khoảng ngày'}</span>
            <button
              type="button"
              className="rf-btn rf-btn--text rf-btn--sm"
              onClick={() => {
                clearAll();
                setOpen(false);
              }}
            >
              Xoá
            </button>
          </div>
        </div>
      ) : null}
    </div>
  );
}

export type Option = { label: string; value: string | number };

/** Select tuỳ chỉnh — searchable, single/multi, click-outside. */
export function Select({
  value,
  onChange,
  options,
  placeholder = 'Chọn',
  allowClear,
  showSearch,
  multiple,
  tags,
  style,
}: {
  value: (string | number) | (string | number)[] | null | undefined;
  onChange: (v: (string | number) | (string | number)[] | null) => void;
  options: Option[];
  placeholder?: string;
  allowClear?: boolean;
  showSearch?: boolean;
  multiple?: boolean;
  /** tags = multi + cho phép tự nhập giá trị mới (thay mode="tags" của AntD) */
  tags?: boolean;
  style?: CSSProperties;
}) {
  multiple = multiple || tags;
  showSearch = showSearch || tags;
  const [open, setOpen] = useState(false);
  const [q, setQ] = useState('');
  const ref = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!open) return;
    const h = (e: MouseEvent) => {
      if (ref.current && !ref.current.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener('mousedown', h);
    return () => document.removeEventListener('mousedown', h);
  }, [open]);

  const selected = multiple ? (Array.isArray(value) ? value : []) : value;
  // tags: giá trị tự nhập chưa có trong options -> vẫn phải hiện ra để bỏ chọn được.
  const allOptions = useMemo(() => {
    if (!tags || !Array.isArray(value)) return options;
    const extra = value.filter((v) => !options.some((o) => o.value === v)).map((v) => ({ label: String(v), value: v }));
    return [...options, ...extra];
  }, [options, tags, value]);
  const filtered = useMemo(
    () => (showSearch && q ? allOptions.filter((o) => o.label.toLowerCase().includes(q.toLowerCase())) : allOptions),
    [allOptions, q, showSearch],
  );
  const canAddTag = !!tags && q.trim() !== '' && !allOptions.some((o) => o.label.toLowerCase() === q.trim().toLowerCase());
  const labelOf = (v: string | number) => allOptions.find((o) => o.value === v)?.label ?? String(v);
  const addTag = () => {
    const v = q.trim();
    if (!v) return;
    const arr2 = Array.isArray(selected) ? [...selected] : [];
    if (!arr2.includes(v)) arr2.push(v);
    onChange(arr2);
    setQ('');
  };
  const display =
    multiple && Array.isArray(selected) && selected.length
      ? `${labelOf(selected[0]!)}${selected.length > 1 ? ` +${selected.length - 1}` : ''}`
      : !multiple && selected != null && selected !== ''
        ? labelOf(selected as string | number)
        : '';

  const toggle = (v: string | number) => {
    if (multiple) {
      const arr = Array.isArray(selected) ? [...selected] : [];
      const i = arr.indexOf(v);
      if (i >= 0) arr.splice(i, 1);
      else arr.push(v);
      onChange(arr);
    } else {
      onChange(v);
      setOpen(false);
    }
  };

  return (
    <div ref={ref} className="rf-select" style={style}>
      <button type="button" onClick={() => setOpen((o) => !o)} className={`rf-select__btn ${display ? '' : 'rf-select__btn--placeholder'}`}>
        <span style={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>{display || placeholder}</span>
        {allowClear && display ? (
          <span
            onClick={(e) => {
              e.stopPropagation();
              onChange(multiple ? [] : null);
            }}
            style={{ color: 'var(--tk-muted)' }}
          >
            ✕
          </span>
        ) : (
          <Icon name="expand_more" size={18} style={{ color: 'var(--tk-muted)' }} />
        )}
      </button>
      {open ? (
        <div className="rf-select__menu">
          {showSearch ? (
            <div style={{ padding: 8, borderBottom: '1px solid var(--tk-line)' }}>
              <input
                autoFocus
                placeholder={tags ? 'Tìm hoặc nhập giá trị mới…' : 'Tìm…'}
                value={q}
                onChange={(e) => setQ(e.target.value)}
                onKeyDown={(e) => {
                  if (e.key === 'Enter' && canAddTag) {
                    e.preventDefault();
                    addTag();
                  }
                }}
                style={{
                  width: '100%',
                  padding: '6px 8px',
                  borderRadius: 6,
                  border: '1px solid var(--tk-border)',
                  outline: 'none',
                  font: '400 13px var(--tk-font)',
                }}
              />
            </div>
          ) : null}
          <div className="rf-select__opts">
            {canAddTag ? (
              <button type="button" onClick={addTag} className="rf-select__opt" style={{ color: 'var(--tk-accent)' }}>
                Thêm “{q.trim()}”
                <Icon name="add" size={16} />
              </button>
            ) : null}
            {filtered.length ? (
              filtered.map((o) => {
                const on = multiple ? Array.isArray(selected) && selected.includes(o.value) : selected === o.value;
                return (
                  <button key={o.value} type="button" onClick={() => toggle(o.value)} className={`rf-select__opt ${on ? 'rf-select__opt--on' : ''}`}>
                    {o.label}
                    {on ? <Icon name="check" size={16} /> : null}
                  </button>
                );
              })
            ) : canAddTag ? null : (
              <div style={{ padding: '14px 12px', textAlign: 'center', fontSize: 13, color: 'var(--tk-muted)' }}>Không có lựa chọn</div>
            )}
          </div>
        </div>
      ) : null}
    </div>
  );
}
