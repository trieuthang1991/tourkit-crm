import { useMemo } from 'react';
import { Badge, Calendar, Typography } from 'antd';
import type { CalendarProps } from 'antd';
import type { Dayjs } from 'dayjs';
import dayjs from 'dayjs';
import { useNavigate } from 'react-router-dom';
import { departuresCrud } from '../booking/departuresApi';
import type { Departure } from '../booking/departureTypes';

export function OperationsCalendarPage() {
  const navigate = useNavigate();
  const list = departuresCrud.useList({ page: 1, size: 500 });

  const byDate = useMemo(() => {
    const map = new Map<string, Departure[]>();
    for (const d of list.data?.items ?? []) {
      if (!d.departureDate) {
        continue;
      }
      const key = dayjs(d.departureDate).format('YYYY-MM-DD');
      const arr = map.get(key) ?? [];
      arr.push(d);
      map.set(key, arr);
    }
    return map;
  }, [list.data]);

  const cellRender: CalendarProps<Dayjs>['cellRender'] = (current, info) => {
    if (info.type !== 'date') {
      return info.originNode;
    }
    const items = byDate.get(current.format('YYYY-MM-DD')) ?? [];
    return (
      <ul style={{ listStyle: 'none', margin: 0, padding: 0 }}>
        {items.map((d) => (
          <li key={d.id} style={{ marginBottom: 2 }}>
            <Badge
              status="processing"
              text={
                <Typography.Link onClick={() => navigate(`/departures/${d.id}`)}>
                  {d.code || d.title}
                </Typography.Link>
              }
            />
          </li>
        ))}
      </ul>
    );
  };

  return (
    <>
      <Typography.Title level={3}>Lịch điều hành</Typography.Title>
      <Calendar cellRender={cellRender} />
    </>
  );
}
