import { useEffect, useMemo, useRef, useState } from 'react';
import type { CSSProperties } from 'react';
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

/** Khoảng ngày (thay DatePicker.RangePicker). Trả về ISO đầu/cuối ngày. */
export function DateRangeInput({
  from,
  to,
  onChange,
  placeholder = ['Từ ngày', 'đến'],
}: {
  from: string | null | undefined;
  to: string | null | undefined;
  onChange: (from: string | undefined, to: string | undefined) => void;
  placeholder?: [string, string];
}) {
  const d = (v: string | null | undefined) => (v ? new Date(v).toISOString().slice(0, 10) : '');
  const startIso = (v: string) => (v ? new Date(v + 'T00:00:00').toISOString() : undefined);
  const endIso = (v: string) => (v ? new Date(v + 'T23:59:59').toISOString() : undefined);
  return (
    <div className="rf-field" style={{ gap: 4 }}>
      <input type="date" title={placeholder[0]} style={{ fontFamily: 'var(--tk-font-mono)' }} value={d(from)} onChange={(e) => onChange(startIso(e.target.value), to ?? undefined)} />
      <span style={{ color: 'var(--tk-muted)', flexShrink: 0 }}>–</span>
      <input type="date" title={placeholder[1]} style={{ fontFamily: 'var(--tk-font-mono)' }} value={d(to)} onChange={(e) => onChange(from ?? undefined, endIso(e.target.value))} />
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
