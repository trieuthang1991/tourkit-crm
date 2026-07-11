import { App, Select, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { departmentSchema, positionSchema } from '../departments/types';

const userSchema = z.object({
  id: z.string().uuid(),
  email: z.string(),
  fullName: z.string(),
  isActive: z.boolean(),
  departmentId: z.string().nullable(),
  departmentName: z.string().nullable(),
  positionId: z.string().nullable(),
  positionName: z.string().nullable(),
});
type UserRow = z.infer<typeof userSchema>;

const USERS_KEY = ['users'];

function useUsers() {
  return useQuery({
    queryKey: USERS_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userSchema).parse(data);
    },
  });
}

function useDepartmentOptions() {
  return useQuery({
    queryKey: ['departments'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/departments');
      return z.array(departmentSchema).parse(data);
    },
  });
}

function usePositionOptions() {
  return useQuery({
    queryKey: ['positions'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/positions');
      return z.array(positionSchema).parse(data);
    },
  });
}

function useAssignOrg() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, departmentId, positionId }: { id: string; departmentId: string | null; positionId: string | null }) => {
      await httpClient.put(`/api/v1/users/${id}/org`, { departmentId, positionId });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

export function UsersPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('user.manage');
  const users = useUsers();
  const departments = useDepartmentOptions();
  const positions = usePositionOptions();
  const assign = useAssignOrg();

  const departmentOptions = useMemo(
    () => (departments.data ?? []).map((d) => ({ label: d.name, value: d.id })),
    [departments.data],
  );
  const positionOptions = useMemo(
    () => (positions.data ?? []).map((p) => ({ label: p.name, value: p.id })),
    [positions.data],
  );

  async function setOrg(user: UserRow, departmentId: string | null, positionId: string | null) {
    try {
      await assign.mutateAsync({ id: user.id, departmentId, positionId });
      message.success('Đã cập nhật cơ cấu');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<UserRow> = [
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
    { title: 'Email', dataIndex: 'email', key: 'email' },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 110,
      render: (v: boolean) => (v ? <Tag color="green">Hoạt động</Tag> : <Tag>Khoá</Tag>),
    },
    {
      title: 'Phòng ban',
      key: 'department',
      width: 220,
      render: (_: unknown, u: UserRow) =>
        canManage ? (
          <Select
            style={{ width: '100%' }}
            placeholder="— Chưa gán —"
            allowClear
            options={departmentOptions}
            value={u.departmentId ?? undefined}
            onChange={(v) => setOrg(u, v ?? null, u.positionId)}
          />
        ) : (
          (u.departmentName ?? '—')
        ),
    },
    {
      title: 'Chức vụ',
      key: 'position',
      width: 220,
      render: (_: unknown, u: UserRow) =>
        canManage ? (
          <Select
            style={{ width: '100%' }}
            placeholder="— Chưa gán —"
            allowClear
            options={positionOptions}
            value={u.positionId ?? undefined}
            onChange={(v) => setOrg(u, u.departmentId, v ?? null)}
          />
        ) : (
          (u.positionName ?? '—')
        ),
    },
  ];

  return (
    <>
      <PageHeader title="Người dùng" />
      <Table rowKey="id" columns={columns} dataSource={users.data ?? []} loading={users.isLoading} pagination={false} />
    </>
  );
}
