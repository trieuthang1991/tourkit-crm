import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import {
  PRIORITY_OPTIONS,
  STATUS_OPTIONS,
  priorityLabel,
  statusLabel,
  workTaskFormSchema,
  workTaskSchema,
} from './types';
import type { WorkTask, WorkTaskForm } from './types';

const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });

type WorkTaskFilter = { status?: number; assigneeUserId?: string; q?: string; priority?: number };

function useWorkTasks(filter: WorkTaskFilter) {
  return useQuery({
    queryKey: ['work-tasks', filter],
    queryFn: async () => {
      const params: Record<string, unknown> = {};
      if (filter.status !== undefined) params.status = filter.status;
      if (filter.assigneeUserId) params.assigneeUserId = filter.assigneeUserId;
      if (filter.q) params.q = filter.q;
      if (filter.priority !== undefined) params.priority = filter.priority;
      const { data } = await httpClient.get<unknown>('/api/v1/work-tasks', { params });
      return z.array(workTaskSchema).parse(data);
    },
  });
}

const workTaskStatsSchema = z.object({
  total: z.number(),
  todo: z.number(),
  inProgress: z.number(),
  done: z.number(),
  cancelled: z.number(),
  overdue: z.number(),
});

function useWorkTaskStats() {
  return useQuery({
    queryKey: ['work-tasks', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/work-tasks/stats');
      return workTaskStatsSchema.parse(data);
    },
  });
}

function useUserOptions() {
  return useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
}

function useCreate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: WorkTaskForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/work-tasks', body);
      return workTaskSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['work-tasks'] }),
  });
}

function useUpdate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: WorkTaskForm }) => {
      await httpClient.put(`/api/v1/work-tasks/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['work-tasks'] }),
  });
}

function useDelete() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/work-tasks/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['work-tasks'] }),
  });
}

const EMPTY: WorkTaskForm = {
  title: '',
  description: null,
  assigneeUserId: null,
  dueDate: null,
  priority: 1,
  status: 0,
  relatedOrderId: null,
};

const priorityColor = (v: number) => (v === 2 ? 'red' : v === 0 ? 'default' : 'blue');
const statusColor = (v: number) => (v === 2 ? 'green' : v === 3 ? 'default' : v === 1 ? 'processing' : 'orange');

export function WorkTasksPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('task.manage');

  const [status, setStatus] = useState<number | undefined>(undefined);
  const [search, setSearch] = useState('');
  const [assigneeUserId, setAssigneeUserId] = useState<string | undefined>();
  const [priority, setPriority] = useState<number | undefined>();
  const [applied, setApplied] = useState<{ q?: string; assigneeUserId?: string; priority?: number }>({});
  const applyFilters = () => setApplied({ q: search || undefined, assigneeUserId, priority });
  const resetFilters = () => {
    setSearch('');
    setAssigneeUserId(undefined);
    setPriority(undefined);
    setStatus(undefined);
    setApplied({});
  };

  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<WorkTask | null>(null);

  const list = useWorkTasks({ status, ...applied });
  const stats = useWorkTaskStats();
  const users = useUserOptions();
  const create = useCreate();
  const update = useUpdate();
  const remove = useDelete();

  const userOptions = useMemo(
    () => (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id })),
    [users.data],
  );

  async function submit(values: WorkTaskForm) {
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: values });
      } else {
        await create.mutateAsync(values);
      }
      message.success('Đã lưu');
      setOpen(false);
      setEditing(null);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function handleDelete(id: string) {
    try {
      await remove.mutateAsync(id);
      message.success('Đã xoá');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<WorkTask> = [
    { title: 'Công việc', dataIndex: 'title', key: 'title' },
    { title: 'Người được giao', dataIndex: 'assigneeName', key: 'assigneeName', render: (v: string | null) => v ?? '—' },
    {
      title: 'Hạn',
      dataIndex: 'dueDate',
      key: 'dueDate',
      render: (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—'),
    },
    { title: 'Ưu tiên', dataIndex: 'priority', key: 'priority', render: (v: number) => <Tag color={priorityColor(v)}>{priorityLabel(v)}</Tag> },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => <Tag color={statusColor(v)}>{statusLabel(v)}</Tag> },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 160,
            render: (_: unknown, item: WorkTask) => (
              <Space>
                <Button size="small" onClick={() => { setEditing(item); setOpen(true); }}>
                  Sửa
                </Button>
                <Popconfirm title="Xoá công việc này?" onConfirm={() => handleDelete(item.id)}>
                  <Button size="small" danger loading={remove.isPending}>
                    Xoá
                  </Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<WorkTask>[number],
        ]
      : []),
  ];

  const defaultValues: WorkTaskForm = editing
    ? {
        title: editing.title,
        description: editing.description,
        assigneeUserId: editing.assigneeUserId,
        dueDate: editing.dueDate,
        priority: editing.priority,
        status: editing.status,
        relatedOrderId: editing.relatedOrderId,
      }
    : EMPTY;

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Công việc
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => { setEditing(null); setOpen(true); }}>
            Thêm công việc
          </Button>
        ) : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Tổng công việc', value: stats.data?.total ?? 0 },
          { title: 'Cần làm', value: stats.data?.todo ?? 0 },
          { title: 'Đang làm', value: stats.data?.inProgress ?? 0 },
          { title: 'Hoàn thành', value: stats.data?.done ?? 0 },
          { title: 'Huỷ', value: stats.data?.cancelled ?? 0 },
          { title: 'Quá hạn', value: stats.data?.overdue ?? 0 },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Tiêu đề công việc" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Người được giao"
              options={userOptions} value={assigneeUserId} onChange={(v) => setAssigneeUserId(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select allowClear style={{ width: '100%' }} placeholder="Ưu tiên"
              options={PRIORITY_OPTIONS} value={priority} onChange={(v) => setPriority(v ?? undefined)} />
          </Col>
          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>Tìm kiếm</Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => setStatus(val === 'all' ? undefined : Number(val))}
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...STATUS_OPTIONS.map((o) => ({ label: o.label, value: String(o.value) }))]}
        />
      </div>

      <Table rowKey="id" columns={columns} dataSource={list.data ?? []} loading={list.isLoading} scroll={{ x: 'max-content' }} pagination={false} />
      {open ? (
        <CrudFormModal
          open={open}
          title={editing ? 'Sửa công việc' : 'Thêm công việc'}
          schema={workTaskFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => { setOpen(false); setEditing(null); }}
          onSubmit={submit}
        >
          <TextField name="title" label="Tiêu đề" required />
          <TextAreaField name="description" label="Mô tả" />
          <SelectField name="assigneeUserId" label="Người được giao" options={userOptions} allowClear />
          <DatePickerField name="dueDate" label="Hạn hoàn thành" />
          <SelectField name="priority" label="Ưu tiên" options={PRIORITY_OPTIONS} required />
          <SelectField name="status" label="Trạng thái" options={STATUS_OPTIONS} required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
