import { useMemo } from 'react';
import { Badge, Calendar, Typography } from 'antd';
import type { CalendarProps } from 'antd';
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

  const cellRender: CalendarProps<Dayjs>['cellRender'] = (current, info) => {
    if (info.type !== 'date') return info.originNode;
    const items = byDate.get(current.format('YYYY-MM-DD')) ?? [];
    if (!items.length) return null;
    return (
      <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
        {items.slice(0, fullscreen ? 6 : 2).map((d) => (
          <li key={d.id} style={{ marginBottom: 2, whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}>
            <Badge
              color="#EB5324"
              text={
                <Typography.Link style={{ fontSize: 12 }} onClick={() => navigate(`/departures/${d.id}`)}>
                  {d.title || d.code} · {d.totalSlots} chỗ
                </Typography.Link>
              }
            />
          </li>
        ))}
        {items.length > (fullscreen ? 6 : 2) ? (
          <li style={{ fontSize: 11, color: '#999' }}>+{items.length - (fullscreen ? 6 : 2)} chuyến</li>
        ) : null}
      </ul>
    );
  };

  return <Calendar fullscreen={fullscreen} cellRender={cellRender} />;
}
