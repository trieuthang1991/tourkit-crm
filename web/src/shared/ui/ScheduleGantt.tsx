import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';

// Thanh phân công trên timeline (1 bar = 1 lịch điều), gán vào 1 hàng tài nguyên (HDV/xe).
export type GanttBar = {
  id: string;
  rowId: string;
  start: string | null; // ISO
  end: string | null;   // ISO (null → 1 ngày)
  label: string;
  sub?: string;
  tone?: 'blue' | 'green' | 'gray' | 'orange';
};

export type GanttRow = { id: string; label: string; sub?: string };

type Props = {
  rows: GanttRow[];
  bars: GanttBar[];
  rangeStart: Dayjs; // ngày đầu cột
  days: number;      // số cột ngày
  labelWidth?: number;
  colWidth?: number;
  onBarClick?: (barId: string) => void;
  emptyText?: string;
};

const TONE_BG: Record<NonNullable<GanttBar['tone']>, string> = {
  blue: 'var(--tk-info)',
  green: 'var(--tk-success)',
  gray: 'var(--tk-muted)',
  orange: 'var(--tk-accent)',
};

// Lưới Gantt: cột trái = tài nguyên, cột phải = các ngày; bar chiếm ô ngày [start..end].
export function ScheduleGantt({
  rows,
  bars,
  rangeStart,
  days,
  labelWidth = 210,
  colWidth = 46,
  onBarClick,
  emptyText = 'Không có phân công trong khoảng ngày.',
}: Props) {
  const start = rangeStart.startOf('day');
  const dayList = Array.from({ length: days }, (_, i) => start.add(i, 'day'));
  const rowHeight = 52;
  const gridWidth = days * colWidth;

  const barsByRow = new Map<string, GanttBar[]>();
  for (const b of bars) {
    const arr = barsByRow.get(b.rowId) ?? [];
    arr.push(b);
    barsByRow.set(b.rowId, arr);
  }

  // Vị trí bar: offset ngày từ rangeStart; kẹp trong [0, days].
  function place(b: GanttBar) {
    const s = b.start ? dayjs(b.start).startOf('day') : start;
    const e = b.end ? dayjs(b.end).startOf('day') : s;
    const from = Math.max(0, s.diff(start, 'day'));
    const toExclusive = Math.min(days, e.diff(start, 'day') + 1);
    const span = Math.max(1, toExclusive - from);
    if (toExclusive <= 0 || from >= days) return null;
    return { left: from * colWidth, width: span * colWidth - 4 };
  }

  const today = dayjs().startOf('day');

  return (
    <div style={{ overflowX: 'auto', border: '1px solid var(--tk-border)', borderRadius: 8, background: 'var(--tk-surface)' }}>
      <div style={{ minWidth: labelWidth + gridWidth }}>
        {/* Header ngày */}
        <div style={{ display: 'flex', position: 'sticky', top: 0, zIndex: 2, background: 'var(--tk-header-bg)', borderBottom: '1px solid var(--tk-border)' }}>
          <div style={{ width: labelWidth, flex: '0 0 auto', padding: '8px 12px', fontWeight: 600, fontSize: 13, color: 'var(--tk-heading)' }}>
            Tài nguyên
          </div>
          <div style={{ display: 'flex' }}>
            {dayList.map((d) => {
              const weekend = d.day() === 0 || d.day() === 6;
              const isToday = d.isSame(today, 'day');
              return (
                <div
                  key={d.toISOString()}
                  style={{
                    width: colWidth,
                    flex: '0 0 auto',
                    textAlign: 'center',
                    padding: '6px 0',
                    fontSize: 11,
                    lineHeight: 1.3,
                    color: isToday ? 'var(--tk-accent)' : weekend ? 'var(--tk-muted)' : 'var(--tk-body)',
                    fontWeight: isToday ? 700 : 500,
                    background: isToday ? 'var(--tk-accent-soft)' : weekend ? 'var(--tk-header-bg)' : 'transparent',
                    borderLeft: '1px solid var(--tk-line)',
                  }}
                >
                  <div>{d.format('DD')}</div>
                  <div style={{ fontSize: 10 }}>Th{d.format('MM')}</div>
                </div>
              );
            })}
          </div>
        </div>

        {/* Hàng tài nguyên */}
        {rows.length === 0 ? (
          <div style={{ padding: 24, textAlign: 'center', color: 'var(--tk-muted)' }}>{emptyText}</div>
        ) : (
          rows.map((r) => (
            <div key={r.id} style={{ display: 'flex', borderBottom: '1px solid var(--tk-line)', minHeight: rowHeight }}>
              <div style={{ width: labelWidth, flex: '0 0 auto', padding: '8px 12px', borderRight: '1px solid var(--tk-border)' }}>
                <div style={{ fontWeight: 600, fontSize: 13, color: 'var(--tk-heading)' }}>{r.label}</div>
                {r.sub ? <div style={{ fontSize: 11, color: 'var(--tk-muted)' }}>{r.sub}</div> : null}
              </div>
              <div style={{ position: 'relative', width: gridWidth, flex: '0 0 auto' }}>
                {/* Lưới cột nền */}
                <div style={{ position: 'absolute', inset: 0, display: 'flex' }}>
                  {dayList.map((d) => {
                    const weekend = d.day() === 0 || d.day() === 6;
                    const isToday = d.isSame(today, 'day');
                    return (
                      <div
                        key={d.toISOString()}
                        style={{
                          width: colWidth,
                          flex: '0 0 auto',
                          borderLeft: '1px solid var(--tk-line)',
                          background: isToday ? 'var(--tk-accent-soft)' : weekend ? 'var(--tk-header-bg)' : 'transparent',
                        }}
                      />
                    );
                  })}
                </div>
                {/* Bars */}
                {(barsByRow.get(r.id) ?? []).map((b, idx) => {
                  const pos = place(b);
                  if (!pos) return null;
                  return (
                    <div
                      key={b.id}
                      onClick={onBarClick ? () => onBarClick(b.id) : undefined}
                      title={`${b.label}${b.sub ? ' · ' + b.sub : ''}`}
                      style={{
                        position: 'absolute',
                        top: 8 + (idx % 1) * 0,
                        left: pos.left + 2,
                        width: pos.width,
                        height: rowHeight - 16,
                        background: TONE_BG[b.tone ?? 'blue'],
                        color: '#fff',
                        borderRadius: 6,
                        padding: '4px 8px',
                        fontSize: 11,
                        lineHeight: 1.25,
                        overflow: 'hidden',
                        cursor: onBarClick ? 'pointer' : 'default',
                        boxShadow: 'var(--tk-shadow-sm)',
                      }}
                    >
                      <div style={{ fontWeight: 600, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{b.label}</div>
                      {b.sub ? <div style={{ opacity: 0.85, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>{b.sub}</div> : null}
                    </div>
                  );
                })}
              </div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
