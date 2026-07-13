// Tab phân đoạn (Segmented) chuẩn — dùng cho tab loại/trạng thái trên màn danh sách.
// Controlled: value + onChange. Style .tk-segtabs/.tk-segtab (components.css).
export type SegOption = { label: string; value: string; count?: string | number };

export function SegmentTabs({
  options,
  value,
  onChange,
}: {
  options: SegOption[];
  value: string;
  onChange: (value: string) => void;
}) {
  return (
    <div className="tk-segtabs">
      {options.map((o) => (
        <button
          key={o.value}
          className={`tk-segtab${o.value === value ? ' tk-segtab--active' : ''}`}
          onClick={() => onChange(o.value)}
        >
          {o.label}
          {o.count !== undefined ? <span className="tk-segtab__count">{o.count}</span> : null}
        </button>
      ))}
    </div>
  );
}
