import { useMemo, useState } from 'react';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { IconButton } from '../../ui/kit';
import { departuresCrud } from './departuresApi';
import type { Departure } from './departureTypes';

/**
 * Lịch tháng khởi hành: mỗi ngày liệt kê các chuyến (legacy "Lịch khởi hành").
 * Dùng chung cho trang Lịch điều hành và widget trên Bàn làm việc.
 * Tự dựng (dayjs) — KHÔNG dùng antd Calendar. Dữ liệu & điều hướng giữ NGUYÊN.
 */
const WEEKDAYS = ['T2', 'T3', 'T4', 'T5', 'T6', 'T7', 'CN'];

export function DepartureCalendar({ fullscreen = true }: { fullscreen?: boolean }) {
  const navigate = useNavigate();
  const list = departuresCrud.useList({ page: 1, size: 500 });
  const [month, setMonth] = useState<Dayjs>(() => dayjs().startOf('month'));

  const byDate = useMemo(() => {
    const map = new Map<string, Departure[]>();
    for (const d of list.data?.items ?? []) {
      if (!d.departureDate) continue;
      const key = dayjs(d.departureDate).format('YYYY-MM-DD');
      const arr = map.get(key) ?? [];
      arr.push(d);
      map.set(key, arr);
    }
    return map;
  }, [list.data]);

  // Lưới 6 tuần bắt đầu từ Thứ 2 của tuần chứa ngày 1.
  const gridStart = useMemo(() => {
    const first = month.startOf('month');
    const dow = (first.day() + 6) % 7; // 0 = Thứ 2
    return first.subtract(dow, 'day');
  }, [month]);
  const days = useMemo(() => Array.from({ length: 42 }, (_, i) => gridStart.add(i, 'day')), [gridStart]);

  const today = dayjs().format('YYYY-MM-DD');
  const max = fullscreen ? 3 : 2;

  return (
    <div className={`rf-cal ${fullscreen ? '' : 'rf-cal--mini'}`}>
      <div className="rf-cal__head">
        <span className="rf-cal__title">{month.format('[Tháng] M, YYYY')}</span>
        <div className="rf-cal__nav">
          <button type="button" className="rf-btn rf-btn--ghost rf-btn--sm" onClick={() => setMonth(dayjs().startOf('month'))}>
            Hôm nay
          </button>
          <IconButton icon="chevron_left" title="Tháng trước" onClick={() => setMonth((m) => m.subtract(1, 'month'))} />
          <IconButton icon="chevron_right" title="Tháng sau" onClick={() => setMonth((m) => m.add(1, 'month'))} />
        </div>
      </div>
      <div className="rf-cal__wd">
        {WEEKDAYS.map((w) => (
          <div key={w}>{w}</div>
        ))}
      </div>
      <div className="rf-cal__grid">
        {days.map((day) => {
          const key = day.format('YYYY-MM-DD');
          const items = byDate.get(key) ?? [];
          const inMonth = day.month() === month.month();
          const isToday = key === today;
          return (
            <div key={key} className={`rf-cal__cell ${inMonth ? '' : 'rf-cal__cell--out'} ${isToday ? 'rf-cal__today' : ''}`}>
              <span className="rf-cal__daynum">{day.date()}</span>
              {items.length ? (
                <div style={{ display: 'flex', flexDirection: 'column', gap: 3, paddingTop: 4 }}>
                  <span
                    style={{
                      alignSelf: 'flex-start',
                      display: 'inline-flex',
                      alignItems: 'center',
                      gap: 5,
                      fontSize: 11,
                      fontWeight: 600,
                      color: 'var(--tk-accent)',
                      background: 'var(--tk-accent-soft)',
                      borderRadius: 20,
                      padding: '1px 8px',
                      fontFamily: 'var(--tk-font-mono)',
                    }}
                  >
                    <span style={{ width: 6, height: 6, borderRadius: '50%', background: 'var(--tk-accent)', display: 'inline-block' }} />
                    {items.length} chuyến
                  </span>
                  {items.slice(0, max).map((d) => (
                    <div
                      key={d.id}
                      onClick={(e) => {
                        e.stopPropagation();
                        navigate(`/departures/${d.id}`);
                      }}
                      title={`${d.title || d.code} · ${d.totalSlots} chỗ`}
                      style={{ fontSize: 12, lineHeight: 1.35, cursor: 'pointer', whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis', color: 'var(--tk-body)' }}
                    >
                      {d.title || d.code}
                      <span style={{ color: 'var(--tk-muted)', fontFamily: 'var(--tk-font-mono)' }}> · {d.totalSlots} chỗ</span>
                    </div>
                  ))}
                  {items.length > max ? <div style={{ fontSize: 11, color: 'var(--tk-muted)' }}>+{items.length - max} chuyến khác</div> : null}
                </div>
              ) : null}
            </div>
          );
        })}
      </div>
    </div>
  );
}
