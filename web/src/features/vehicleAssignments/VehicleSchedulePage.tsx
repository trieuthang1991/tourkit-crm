import { Select, Typography } from '../../shared/ui/antd';
import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { ScheduleBoard } from '../../shared/ui/ScheduleBoard';
import type { GanttBar, GanttRow } from '../../shared/ui/ScheduleGantt';
import { vehicleAssignmentSchema } from './vehicleAssignmentTypes';

const STATUS_TONE: Record<number, GanttBar['tone']> = { 1: 'blue', 2: 'green', 4: 'gray' };
const STATUS_OPTIONS = [
  { value: 1, label: 'Chờ duyệt' },
  { value: 2, label: 'Đã điều' },
];

function useVehicleAssignments() {
  return useQuery({
    queryKey: ['vehicleAssignments', 'schedule'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/vehicle-assignments', { params: { page: 1, size: 500 } });
      return pagedSchema(vehicleAssignmentSchema).parse(data).items;
    },
  });
}

export function VehicleSchedulePage() {
  const list = useVehicleAssignments();
  const [status, setStatus] = useState<number | undefined>();

  const { rows, bars } = useMemo(() => {
    const items = (list.data ?? [])
      .filter((a) => a.timeGo && (status === undefined || a.status === status));

    const rowMap = new Map<string, GanttRow>();
    const bars: GanttBar[] = [];
    for (const a of items) {
      if (!rowMap.has(a.vehicleId)) {
        rowMap.set(a.vehicleId, { id: a.vehicleId, label: a.vehicleName ?? 'Xe', sub: undefined });
      }
      bars.push({
        id: a.id,
        rowId: a.vehicleId,
        start: a.timeGo,
        end: a.timeCome ?? a.timeGo,
        label: a.departureCode ?? a.departureTitle ?? 'Tour',
        sub: a.driverName ?? (a.departureCode ? a.departureTitle ?? undefined : undefined),
        tone: STATUS_TONE[a.status] ?? 'blue',
      });
    }
    return { rows: [...rowMap.values()], bars };
  }, [list.data, status]);

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Lịch điều xe
        </Typography.Title>
        <Typography.Text type="secondary">Timeline điều xe theo ngày — mỗi thanh là một chuyến (kèm tài xế).</Typography.Text>
      </div>

      <ScheduleBoard
        rows={rows}
        bars={bars}
        loading={list.isLoading}
        emptyText="Chưa có lịch điều xe trong khoảng ngày."
        extra={
          <Select
            allowClear
            placeholder="Trạng thái"
            style={{ width: 160 }}
            options={STATUS_OPTIONS}
            value={status}
            onChange={(v) => setStatus(v ?? undefined)}
          />
        }
      />
    </>
  );
}
