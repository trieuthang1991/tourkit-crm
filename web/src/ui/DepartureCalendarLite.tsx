import { useMemo, useState } from 'react';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { departuresCrud } from '../features/booking/departuresApi';
import type { Departure } from '../features/booking/departureTypes';

const WD = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

/** Lịch tháng khởi hành — AntD-free (Tailwind). Ngày có tour: chấm gradient + số chuyến + tên tuyến. */
export function DepartureCalendarLite() {
  const nav = useNavigate();
  const list = departuresCrud.useList({ page: 1, size: 500 });
  const [month, setMonth] = useState(dayjs().startOf('month'));

  const byDate = useMemo(() => {
    const m = new Map<string, Departure[]>();
    for (const d of list.data?.items ?? []) {
      if (!d.departureDate) continue;
      const k = dayjs(d.departureDate).format('YYYY-MM-DD');
      const arr = m.get(k) ?? [];
      arr.push(d);
      m.set(k, arr);
    }
    return m;
  }, [list.data]);

  const firstDow = (month.day() + 6) % 7; // Mon=0
  const gridStart = month.subtract(firstDow, 'day');
  const cells = Array.from({ length: 42 }, (_, i) => gridStart.add(i, 'day'));
  const today = dayjs();

  return (
    <div className="px-4 pb-4 pt-1">
      <div className="mb-3 flex items-center justify-between">
        <div className="text-[15px] font-semibold text-[var(--tk-heading)]">
          Tháng {month.format('M, YYYY')}
        </div>
        <div className="flex items-center gap-4">
          <span className="flex items-center gap-1.5 text-[12px] text-[var(--tk-muted)]">
            <span className="inline-block h-2 w-2 rounded-full" style={{ background: 'var(--tk-accent-grad)' }} />
            Ngày có tour
          </span>
          <div className="flex gap-1">
            <button type="button" onClick={() => setMonth((m) => m.subtract(1, 'month'))} className="flex h-7 w-7 items-center justify-center rounded-lg border border-[var(--tk-border)] text-[var(--tk-muted-2)] hover:border-[var(--tk-border)]">‹</button>
            <button type="button" onClick={() => setMonth(dayjs().startOf('month'))} className="rounded-lg border border-[var(--tk-border)] px-2 text-[12px] text-[var(--tk-body)] hover:border-[var(--tk-border)]">Nay</button>
            <button type="button" onClick={() => setMonth((m) => m.add(1, 'month'))} className="flex h-7 w-7 items-center justify-center rounded-lg border border-[var(--tk-border)] text-[var(--tk-muted-2)] hover:border-[var(--tk-border)]">›</button>
          </div>
        </div>
      </div>

      <div className="grid grid-cols-7 gap-px overflow-hidden rounded-xl border border-[var(--tk-line)] bg-[var(--tk-line)]">
        {WD.map((w, i) => (
          <div key={w} className={`bg-[var(--tk-header-bg)] py-2 text-center text-[11px] font-semibold ${i >= 5 ? 'text-[var(--tk-danger)]' : 'text-[var(--tk-muted)]'}`}>{w}</div>
        ))}
        {cells.map((d) => {
          const inMonth = d.month() === month.month();
          const items = byDate.get(d.format('YYYY-MM-DD')) ?? [];
          const isToday = d.isSame(today, 'day');
          return (
            <div
              key={d.format('YYYY-MM-DD')}
              className={`min-h-[92px] bg-white p-1.5 ${inMonth ? '' : 'opacity-40'}`}
              style={isToday ? { boxShadow: 'inset 0 0 0 2px var(--tk-accent)' } : undefined}
            >
              <div className={`mb-1 text-right font-mono text-[12px] ${d.day() === 0 || d.day() === 6 ? 'text-[var(--tk-danger)]' : 'text-[var(--tk-body)]'}`}>
                {d.date()}
              </div>
              {items.length ? (
                <div className="flex flex-col gap-1">
                  <span className="inline-flex w-fit items-center gap-1 rounded-full px-1.5 py-0.5 text-[10px] font-semibold text-white" style={{ background: 'var(--tk-accent-grad)' }}>
                    {items.length} chuyến
                  </span>
                  {items.slice(0, 2).map((it) => (
                    <button
                      key={it.id}
                      type="button"
                      onClick={() => nav(`/departures/${it.id}`)}
                      title={`${it.title || it.code} · ${it.totalSlots} chỗ`}
                      className="truncate text-left text-[11px] leading-tight text-[var(--tk-body)] hover:text-[var(--tk-accent)]"
                    >
                      {it.title || it.code} <span className="font-mono text-[var(--tk-muted)]">· {it.totalSlots}</span>
                    </button>
                  ))}
                  {items.length > 2 ? <span className="text-[10px] text-[var(--tk-muted)]">+{items.length - 2} chuyến</span> : null}
                </div>
              ) : null}
            </div>
          );
        })}
      </div>
    </div>
  );
}
