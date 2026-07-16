import { useMemo } from 'react';
import { Calendar } from '../../shared/ui/antd';
import type { CalendarProps } from '../../shared/ui/antd';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { departuresCrud } from './departuresApi';
import type { Departure } from './departureTypes';

/**
 * Lịch tháng khởi hành: mỗi ngày liệt kê các chuyến (legacy "Lịch khởi hành").
 * Dùng chung cho trang Lịch điều hành và widget trên Bàn làm việc.
 */
export function DepartureCalendar({ fullscreen = true }: { fullscreen?: boolean }) {
  const navigate = useNavigate();
  const list = departuresCrud.useList({ page: 1, size: 500 });

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

  // Kiểu Stitch: mỗi ngày có tour hiện badge "N chuyến" (chấm cam) + tên tuyến + số chỗ (mono).
  // GIỮ NGUYÊN dữ liệu & điều hướng — chỉ đổi trình bày.
  const max = fullscreen ? 3 : 2;
  const cellRender: CalendarProps<Dayjs>['cellRender'] = (current, info) => {
    if (info.type !== 'date') return info.originNode;
    const items = byDate.get(current.format('YYYY-MM-DD')) ?? [];
    if (!items.length) return null;
    return (
      <div style={{ display: 'flex', flexDirection: 'column', gap: 3, paddingTop: 2 }}>
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
            style={{
              fontSize: 12,
              lineHeight: 1.35,
              cursor: 'pointer',
              whiteSpace: 'nowrap',
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              color: 'var(--tk-body)',
            }}
          >
            {d.title || d.code}
            <span style={{ color: 'var(--tk-muted)', fontFamily: 'var(--tk-font-mono)' }}> · {d.totalSlots} chỗ</span>
          </div>
        ))}
        {items.length > max ? (
          <div style={{ fontSize: 11, color: 'var(--tk-muted)' }}>+{items.length - max} chuyến khác</div>
        ) : null}
      </div>
    );
  };

  return <Calendar fullscreen={fullscreen} cellRender={cellRender} />;
}
