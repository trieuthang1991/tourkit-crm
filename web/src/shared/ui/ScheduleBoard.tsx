import { Button, DatePicker, Segmented, Space, Spin } from './antd';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import type { ReactNode } from 'react';
import { useState } from 'react';
import { ScheduleGantt } from './ScheduleGantt';
import type { GanttBar, GanttRow } from './ScheduleGantt';

type Props = {
  rows: GanttRow[];
  bars: GanttBar[];
  loading?: boolean;
  onBarClick?: (barId: string) => void;
  extra?: ReactNode; // slot lọc thêm (trạng thái…)
  emptyText?: string;
};

// Khung lịch điều: điều khiển khoảng ngày (Trước/Hôm nay/Tiếp + số ngày) rồi vẽ Gantt.
export function ScheduleBoard({ rows, bars, loading, onBarClick, extra, emptyText }: Props) {
  const [rangeStart, setRangeStart] = useState<Dayjs>(dayjs().startOf('day').subtract(2, 'day'));
  const [days, setDays] = useState(21);

  return (
    <div>
      <Space wrap style={{ marginBottom: 12 }}>
        <Button onClick={() => setRangeStart((d) => d.subtract(days, 'day'))}>‹ Trước</Button>
        <Button type="primary" ghost onClick={() => setRangeStart(dayjs().startOf('day').subtract(2, 'day'))}>
          Hôm nay
        </Button>
        <Button onClick={() => setRangeStart((d) => d.add(days, 'day'))}>Tiếp ›</Button>
        <DatePicker
          allowClear={false}
          format="DD/MM/YYYY"
          value={rangeStart}
          onChange={(d) => d && setRangeStart(d.startOf('day'))}
        />
        <Segmented
          value={String(days)}
          onChange={(v) => setDays(Number(v))}
          options={[
            { label: '14 ngày', value: '14' },
            { label: '21 ngày', value: '21' },
            { label: '30 ngày', value: '30' },
          ]}
        />
        {extra}
      </Space>

      <Spin spinning={!!loading}>
        <ScheduleGantt
          rows={rows}
          bars={bars}
          rangeStart={rangeStart}
          days={days}
          onBarClick={onBarClick}
          emptyText={emptyText}
        />
      </Spin>
    </div>
  );
}
