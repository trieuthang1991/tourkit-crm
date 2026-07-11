import { App, Button, Popconfirm, Select, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
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

function useWorkTasks(status: number | undefined) {
  return useQuery({
    queryKey: ['work-tasks', status ?? 'all'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/work-tasks', {
        params: status === undefined ? {} : { status },
      });
      return z.array(workTaskSchema).parse(data);
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
  const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<WorkTask | null>(null);

  const list = useWorkTasks(statusFilter);
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
      <PageHeader
        title="Công việc"
        extra={
          <Space>
            <Select
              style={{ width: 180 }}
              placeholder="Lọc trạng thái"
              allowClear
              options={STATUS_OPTIONS}
              value={statusFilter}
              onChange={(v) => setStatusFilter(v ?? undefined)}
            />
            {canManage ? (
              <Button type="primary" onClick={() => { setEditing(null); setOpen(true); }}>
                Thêm công việc
              </Button>
            ) : null}
          </Space>
        }
      />
      <Table rowKey="id" columns={columns} dataSource={list.data ?? []} loading={list.isLoading} pagination={false} />
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
