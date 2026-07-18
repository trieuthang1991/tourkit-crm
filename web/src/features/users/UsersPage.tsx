import { App, Button, Input, Modal, Popconfirm, Space, Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { Controller, useFormContext } from 'react-hook-form';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CellEntity, CellStack, DataCard } from '../../shared/ui';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { CheckboxField, SelectField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import { departmentSchema, positionSchema } from '../departments/types';

/* Màn "Thành viên" (/users) — quản trị người dùng: tạo/sửa, gán vai trò & cơ cấu,
   đặt lại mật khẩu, khoá/mở tài khoản. Hệ Refined (shim shared/ui/antd). */

const userSchema = z.object({
  id: z.string().uuid(),
  email: z.string(),
  fullName: z.string(),
  isActive: z.boolean(),
  departmentId: z.string().nullable(),
  departmentName: z.string().nullable(),
  positionId: z.string().nullable(),
  positionName: z.string().nullable(),
  // Backend đang bổ sung song song — nới lỏng để không vỡ khi field chưa có.
  roleId: z.string().nullable().optional(),
  roleName: z.string().nullable().optional(),
});
type UserRow = z.infer<typeof userSchema>;

const roleOptionSchema = z.object({ id: z.string(), name: z.string() });

// Form tạo mới (email + mật khẩu bắt buộc).
const userCreateSchema = z.object({
  email: z.string().min(1, 'Bắt buộc').email('Email không hợp lệ'),
  fullName: z.string().min(1, 'Bắt buộc'),
  password: z.string().min(6, 'Tối thiểu 6 ký tự'),
  departmentId: z.string().nullable(),
  positionId: z.string().nullable(),
  roleId: z.string().nullable(),
  isActive: z.boolean(),
});
type UserCreateForm = z.infer<typeof userCreateSchema>;

// Form sửa (không đổi email/mật khẩu ở đây).
const userEditSchema = z.object({
  fullName: z.string().min(1, 'Bắt buộc'),
  departmentId: z.string().nullable(),
  positionId: z.string().nullable(),
  roleId: z.string().nullable(),
  isActive: z.boolean(),
});
type UserEditForm = z.infer<typeof userEditSchema>;

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

function useRoleOptions() {
  return useQuery({
    queryKey: ['roles', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/roles');
      return z.array(roleOptionSchema.passthrough()).parse(data);
    },
  });
}

function useCreateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: UserCreateForm) => {
      await httpClient.post('/api/v1/users', body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

function useUpdateUser() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: UserEditForm }) => {
      await httpClient.put(`/api/v1/users/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

function useResetPassword() {
  return useMutation({
    mutationFn: async ({ id, newPassword }: { id: string; newPassword: string }) => {
      await httpClient.post(`/api/v1/users/${id}/reset-password`, { newPassword });
    },
  });
}

function useToggleActive() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.post(`/api/v1/users/${id}/toggle-active`, {});
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: USERS_KEY }),
  });
}

/** Field mật khẩu (che ký tự) — dùng trong CrudFormModal tạo người dùng. */
function PasswordField({ name, label }: { name: string; label: string }) {
  const { control, formState } = useFormContext();
  const err = formState.errors[name]?.message as string | undefined;
  return (
    <Controller
      name={name}
      control={control}
      render={({ field }) => (
        <div className="mb-4">
          <label className="mb-1.5 block text-[12.5px] font-medium text-[var(--tk-heading)]">
            {label} <span className="text-[var(--tk-danger)]">*</span>
          </label>
          <Input.Password value={(field.value as string) ?? ''} onChange={(e) => field.onChange(e.target.value)} placeholder="Nhập mật khẩu" />
          {err ? <div className="mt-1.5 text-[12px] text-[var(--tk-danger)]">{err}</div> : null}
        </div>
      )}
    />
  );
}

/** Modal đặt lại mật khẩu (nhập mật khẩu mới). */
function ResetPasswordModal({ user, onClose }: { user: UserRow; onClose: () => void }) {
  const { message } = App.useApp();
  const reset = useResetPassword();
  const [pw, setPw] = useState('');

  async function submit() {
    if (pw.length < 6) {
      message.error('Mật khẩu tối thiểu 6 ký tự');
      return;
    }
    try {
      await reset.mutateAsync({ id: user.id, newPassword: pw });
      message.success('Đã đặt lại mật khẩu');
      onClose();
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <Modal open title="Đặt lại mật khẩu" onCancel={onClose} onOk={submit} okText="Đặt lại" confirmLoading={reset.isPending}>
      <div style={{ marginBottom: 12, fontSize: 13, color: 'var(--tk-body)' }}>
        Người dùng: <strong style={{ color: 'var(--tk-heading)' }}>{user.fullName}</strong> ({user.email})
      </div>
      <label className="mb-1.5 block text-[12.5px] font-medium text-[var(--tk-heading)]">Mật khẩu mới</label>
      <Input.Password value={pw} onChange={(e) => setPw((e.target as HTMLInputElement).value)} placeholder="Nhập mật khẩu mới" />
    </Modal>
  );
}

export function UsersPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canCreate = has('user.manage');
  const canManage = has('user.manage');

  const users = useUsers();
  const departments = useDepartmentOptions();
  const positions = usePositionOptions();
  const roles = useRoleOptions();

  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<UserRow | null>(null);
  const [resetting, setResetting] = useState<UserRow | null>(null);

  const create = useCreateUser();
  const update = useUpdateUser();
  const toggle = useToggleActive();

  const departmentOptions = useMemo(() => (departments.data ?? []).map((d) => ({ label: d.name, value: d.id })), [departments.data]);
  const positionOptions = useMemo(() => (positions.data ?? []).map((p) => ({ label: p.name, value: p.id })), [positions.data]);
  const roleOptions = useMemo(() => (roles.data ?? []).map((r) => ({ label: r.name, value: r.id })), [roles.data]);

  async function submitCreate(values: UserCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã tạo người dùng');
      setCreating(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: UserEditForm) {
    if (!editing) return;
    try {
      await update.mutateAsync({ id: editing.id, body: values });
      message.success('Đã lưu');
      setEditing(null);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function handleToggle(user: UserRow) {
    try {
      await toggle.mutateAsync(user.id);
      message.success(user.isActive ? 'Đã khoá tài khoản' : 'Đã mở tài khoản');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<UserRow> = [
    {
      title: 'Người dùng',
      key: 'user',
      render: (_: unknown, u: UserRow) => <CellEntity name={u.fullName} meta={u.email} />,
    },
    {
      title: 'Vai trò',
      key: 'role',
      width: 160,
      render: (_: unknown, u: UserRow) => (u.roleName ? <Tag color="purple">{u.roleName}</Tag> : <span style={{ color: 'var(--tk-muted)' }}>—</span>),
    },
    {
      title: 'Phòng ban / Chức danh',
      key: 'org',
      width: 220,
      render: (_: unknown, u: UserRow) => <CellStack main={u.departmentName ?? '—'} sub={u.positionName ?? undefined} />,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isActive',
      key: 'isActive',
      width: 110,
      render: (v: boolean) => (v ? <Tag color="green">Hoạt động</Tag> : <Tag color="red">Đã khoá</Tag>),
    },
  ];

  const tableColumns: ColumnsType<UserRow> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 300,
          render: (_: unknown, u: UserRow) => (
            <Space>
              <Button size="small" onClick={() => setEditing(u)}>
                Sửa
              </Button>
              <Button size="small" onClick={() => setResetting(u)}>
                Đặt lại MK
              </Button>
              <Popconfirm title={u.isActive ? 'Khoá tài khoản này?' : 'Mở lại tài khoản này?'} okText={u.isActive ? 'Khoá' : 'Mở'} onConfirm={() => handleToggle(u)}>
                <Button size="small" danger={u.isActive}>
                  {u.isActive ? 'Khoá' : 'Mở'}
                </Button>
              </Popconfirm>
            </Space>
          ),
        },
      ]
    : columns;

  return (
    <>
      <PageHeader
        title="Thành viên"
        extra={
          canCreate ? (
            <Button type="primary" onClick={() => setCreating(true)}>
              Thêm người dùng
            </Button>
          ) : null
        }
      />
      <DataCard title="Danh sách người dùng">
        <Table rowKey="id" columns={tableColumns} dataSource={users.data ?? []} loading={users.isLoading} pagination={false} />
      </DataCard>

      {creating ? (
        <CrudFormModal
          open={creating}
          title="Thêm người dùng"
          schema={userCreateSchema}
          defaultValues={{ email: '', fullName: '', password: '', departmentId: null, positionId: null, roleId: null, isActive: true }}
          submitting={create.isPending}
          onCancel={() => setCreating(false)}
          onSubmit={submitCreate}
        >
          <TextField name="email" label="Email" required />
          <TextField name="fullName" label="Họ tên" required />
          <PasswordField name="password" label="Mật khẩu" />
          <SelectField name="roleId" label="Vai trò" options={roleOptions} allowClear showSearch />
          <SelectField name="departmentId" label="Phòng ban" options={departmentOptions} allowClear showSearch />
          <SelectField name="positionId" label="Chức danh" options={positionOptions} allowClear showSearch />
          <CheckboxField name="isActive" label="Kích hoạt tài khoản" />
        </CrudFormModal>
      ) : null}

      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa người dùng"
          schema={userEditSchema}
          defaultValues={{
            fullName: editing.fullName,
            departmentId: editing.departmentId,
            positionId: editing.positionId,
            roleId: editing.roleId ?? null,
            isActive: editing.isActive,
          }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="fullName" label="Họ tên" required />
          <SelectField name="roleId" label="Vai trò" options={roleOptions} allowClear showSearch />
          <SelectField name="departmentId" label="Phòng ban" options={departmentOptions} allowClear showSearch />
          <SelectField name="positionId" label="Chức danh" options={positionOptions} allowClear showSearch />
          <CheckboxField name="isActive" label="Kích hoạt tài khoản" />
        </CrudFormModal>
      ) : null}

      {resetting ? <ResetPasswordModal user={resetting} onClose={() => setResetting(null)} /> : null}
    </>
  );
}
