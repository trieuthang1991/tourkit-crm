import { Select, Typography } from '../../shared/ui/antd';
import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { ScheduleBoard } from '../../shared/ui/ScheduleBoard';
import type { GanttBar, GanttRow } from '../../shared/ui/ScheduleGantt';
import { guideAssignmentSchema } from './guideAssignmentTypes';

const STATUS_TONE: Record<number, GanttBar['tone']> = { 1: 'blue', 2: 'green', 4: 'gray' };
const STATUS_OPTIONS = [
  { value: 1, label: 'Đã tạo' },
  { value: 2, label: 'Đang chạy' },
];

function useGuideAssignments() {
  return useQuery({
    queryKey: ['guideAssignments', 'schedule'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/guide-assignments', { params: { page: 1, size: 500 } });
      return pagedSchema(guideAssignmentSchema).parse(data).items;
    },
  });
}

export function GuideSchedulePage() {
  const list = useGuideAssignments();
  const [status, setStatus] = useState<number | undefined>();

  const { rows, bars } = useMemo(() => {
    const items = (list.data ?? [])
      .filter((a) => a.timeGo && (status === undefined || a.status === status));

    const rowMap = new Map<string, GanttRow>();
    const bars: GanttBar[] = [];
    for (const a of items) {
      if (!rowMap.has(a.providerId)) {
        rowMap.set(a.providerId, { id: a.providerId, label: a.providerName ?? 'HDV', sub: undefined });
      }
      bars.push({
        id: a.id,
        rowId: a.providerId,
        start: a.timeGo,
        end: a.timeCome ?? a.timeGo,
        label: a.departureCode ?? a.departureTitle ?? 'Tour',
        sub: a.departureCode ? a.departureTitle ?? undefined : undefined,
        tone: STATUS_TONE[a.status] ?? 'blue',
      });
    }
    return { rows: [...rowMap.values()], bars };
  }, [list.data, status]);

  return (
    <>
      <div style={{ marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Lịch điều Hướng dẫn viên
        </Typography.Title>
        <Typography.Text type="secondary">Timeline phân công HDV theo ngày — mỗi thanh là một chuyến.</Typography.Text>
      </div>

      <ScheduleBoard
        rows={rows}
        bars={bars}
        loading={list.isLoading}
        emptyText="Chưa có phân công HDV trong khoảng ngày."
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
