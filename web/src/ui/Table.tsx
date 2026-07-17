import { useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import { Empty } from './kit';

/* =========================================================================
   ui/Table — bảng hệ "Refined": header UPPERCASE muted nền #fafafa, hover hàng,
   tfoot tổng, overflow-x. Vanilla CSS (.rf-table). KHÔNG antd.
   ========================================================================= */

export type Column<T> = {
  key: string;
  title: ReactNode;
  render?: (row: T, index: number) => ReactNode;
  dataIndex?: keyof T;
  width?: number;
  align?: 'left' | 'right' | 'center';
  sortable?: boolean;
  sortValue?: (row: T) => number | string;
  mono?: boolean;
};

type Sort = { key: string; dir: 'asc' | 'desc' } | null;

export function Table<T>({
  columns,
  data,
  rowKey,
  loading,
  empty = 'Không có dữ liệu',
  minWidth,
  summary,
  onRowClick,
}: {
  columns: Column<T>[];
  data: T[];
  rowKey: (row: T) => string;
  loading?: boolean;
  empty?: string;
  /** min-width bảng (px) để cuộn ngang khi nhiều cột */
  minWidth?: number;
  /** Dòng tfoot "Tổng cộng (trang này)" — mảng ô theo thứ tự cột (null = ô trống) */
  summary?: ReactNode;
  onRowClick?: (row: T) => void;
}) {
  const [sort, setSort] = useState<Sort>(null);

  const rows = useMemo(() => {
    if (!sort) return data;
    const col = columns.find((c) => c.key === sort.key);
    if (!col) return data;
    const val = (r: T) => (col.sortValue ? col.sortValue(r) : col.dataIndex ? (r[col.dataIndex] as unknown as number | string) : '');
    return [...data].sort((a, b) => {
      const va = val(a);
      const vb = val(b);
      const cmp = va < vb ? -1 : va > vb ? 1 : 0;
      return sort.dir === 'asc' ? cmp : -cmp;
    });
  }, [data, sort, columns]);

  const clickSort = (c: Column<T>) => {
    if (!c.sortable) return;
    setSort((s) => (s?.key !== c.key ? { key: c.key, dir: 'asc' } : s.dir === 'asc' ? { key: c.key, dir: 'desc' } : null));
  };

  return (
    <div className="rf-tablewrap">
      <table className="rf-table" style={{ minWidth }}>
        <thead>
          <tr>
            {columns.map((c) => (
              <th
                key={c.key}
                onClick={() => clickSort(c)}
                style={{ width: c.width, textAlign: c.align, cursor: c.sortable ? 'pointer' : undefined, userSelect: c.sortable ? 'none' : undefined }}
              >
                {c.title}
                {c.sortable ? (
                  <span style={{ marginLeft: 4, fontSize: 9, color: 'var(--tk-muted)' }}>
                    {sort?.key === c.key ? (sort.dir === 'asc' ? '▲' : '▼') : '↕'}
                  </span>
                ) : null}
              </th>
            ))}
          </tr>
        </thead>
        <tbody>
          {loading ? (
            <tr>
              <td colSpan={columns.length} style={{ padding: '40px 0', textAlign: 'center', color: 'var(--tk-muted)' }}>
                Đang tải…
              </td>
            </tr>
          ) : rows.length === 0 ? (
            <tr>
              <td colSpan={columns.length}>
                <Empty text={empty} />
              </td>
            </tr>
          ) : (
            rows.map((r, i) => (
              <tr key={rowKey(r)} onClick={onRowClick ? () => onRowClick(r) : undefined} style={onRowClick ? { cursor: 'pointer' } : undefined}>
                {columns.map((c) => (
                  <td key={c.key} style={{ textAlign: c.align }} className={c.mono ? 'rf-cell-mono' : undefined}>
                    {c.render ? c.render(r, i) : c.dataIndex ? (r[c.dataIndex] as ReactNode) : null}
                  </td>
                ))}
              </tr>
            ))
          )}
        </tbody>
        {summary && rows.length > 0 ? (
          <tfoot>
            <tr>{summary}</tr>
          </tfoot>
        ) : null}
      </table>
    </div>
  );
}

/** Pagination hairline: prev · số trang (active accent) · next + dòng "Hiển thị x–y trong N". */
export function Pagination({
  page,
  pageSize,
  total,
  onChange,
  unit = 'mục',
}: {
  page: number;
  pageSize: number;
  total: number;
  onChange: (p: number) => void;
  unit?: string;
}) {
  const totalPages = Math.max(1, Math.ceil(total / pageSize));
  if (total === 0) return null;
  const from = (page - 1) * pageSize + 1;
  const to = Math.min(page * pageSize, total);

  // Cửa sổ trang gọn: 1 … p-1 p p+1 … N
  const pages: (number | '…')[] = [];
  const push = (v: number | '…') => pages.push(v);
  if (totalPages <= 7) {
    for (let i = 1; i <= totalPages; i++) push(i);
  } else {
    push(1);
    if (page > 3) push('…');
    for (let i = Math.max(2, page - 1); i <= Math.min(totalPages - 1, page + 1); i++) push(i);
    if (page < totalPages - 2) push('…');
    push(totalPages);
  }

  return (
    <div className="rf-pg">
      <span className="rf-pg__info">
        Hiển thị {from.toLocaleString('vi-VN')}–{to.toLocaleString('vi-VN')} trong {total.toLocaleString('vi-VN')} {unit}
      </span>
      <div className="rf-pg__list">
        <button type="button" className="rf-pg__btn" disabled={page <= 1} onClick={() => onChange(page - 1)} aria-label="Trang trước">
          ‹
        </button>
        {pages.map((p, i) =>
          p === '…' ? (
            <span key={`e${i}`} className="rf-pg__info" style={{ padding: '0 4px' }}>
              …
            </span>
          ) : (
            <button key={p} type="button" className={`rf-pg__btn ${p === page ? 'rf-pg__btn--active' : ''}`} onClick={() => onChange(p)}>
              {p}
            </button>
          ),
        )}
        <button type="button" className="rf-pg__btn" disabled={page >= totalPages} onClick={() => onChange(page + 1)} aria-label="Trang sau">
          ›
        </button>
      </div>
    </div>
  );
}
