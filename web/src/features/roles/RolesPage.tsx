import { App, Button, Checkbox, Drawer, Input, Popconfirm, Space, Table } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useMemo, useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CellStack, CellText, DataCard } from '../../shared/ui';
import { useAuth } from '../auth/AuthContext';

/* Màn "Vai trò & quyền" (/roles) — quản trị vai trò + gán quyền theo nhóm.
   Hệ Refined (shim shared/ui/antd). */

const roleListItemSchema = z.object({
  id: z.string(),
  name: z.string(),
  permissionCount: z.number(),
  userCount: z.number(),
});
type RoleListItem = z.infer<typeof roleListItemSchema>;

const roleDetailSchema = z.object({
  id: z.string(),
  name: z.string(),
  permissionIds: z.array(z.string()),
});

const permissionSchema = z.object({
  id: z.string(),
  code: z.string(),
  name: z.string(),
  group: z.string(),
});
type Permission = z.infer<typeof permissionSchema>;

const ROLES_KEY = ['roles'];

function useRoles() {
  return useQuery({
    queryKey: ROLES_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/roles');
      return z.array(roleListItemSchema).parse(data);
    },
  });
}

function usePermissions() {
  return useQuery({
    queryKey: ['permissions'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/permissions');
      return z.array(permissionSchema).parse(data);
    },
  });
}

function useRoleDetail(id: string | null) {
  return useQuery({
    queryKey: ['roles', 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/roles/${id}`);
      return roleDetailSchema.parse(data);
    },
  });
}

function useDeleteRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/roles/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ROLES_KEY }),
  });
}

/** Nhóm quyền theo `group`, giữ thứ tự xuất hiện. */
function groupPermissions(perms: Permission[]): { name: string; items: Permission[] }[] {
  const map = new Map<string, Permission[]>();
  for (const p of perms) {
    const g = p.group || 'Khác';
    if (!map.has(g)) map.set(g, []);
    map.get(g)!.push(p);
  }
  return [...map.entries()].map(([name, items]) => ({ name, items }));
}

/** Drawer soạn vai trò: đổi tên + chọn quyền theo nhóm (chọn-tất-cả mỗi nhóm). */
function RoleEditorDrawer({ roleId, initialName, onClose }: { roleId: string | null; initialName: string; onClose: () => void }) {
  const { message } = App.useApp();
  const qc = useQueryClient();
  const isEdit = !!roleId;

  const permissions = usePermissions();
  const detail = useRoleDetail(roleId);

  const [name, setName] = useState(initialName);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [saving, setSaving] = useState(false);

  // Nạp quyền đã gán khi mở form sửa (sau khi detail về).
  useEffect(() => {
    if (isEdit && detail.data) {
      setName(detail.data.name);
      setSelected(new Set(detail.data.permissionIds));
    }
  }, [isEdit, detail.data]);

  const groups = useMemo(() => groupPermissions(permissions.data ?? []), [permissions.data]);

  const toggleOne = (id: string, on: boolean) =>
    setSelected((s) => {
      const next = new Set(s);
      if (on) next.add(id);
      else next.delete(id);
      return next;
    });

  const toggleGroup = (items: Permission[], on: boolean) =>
    setSelected((s) => {
      const next = new Set(s);
      for (const p of items) {
        if (on) next.add(p.id);
        else next.delete(p.id);
      }
      return next;
    });

  async function save() {
    if (!name.trim()) {
      message.error('Nhập tên vai trò');
      return;
    }
    const permissionIds = [...selected];
    setSaving(true);
    try {
      if (isEdit) {
        await httpClient.put(`/api/v1/roles/${roleId}`, { name: name.trim(), permissionIds });
      } else {
        const { data } = await httpClient.post<{ id?: string }>('/api/v1/roles', { name: name.trim() });
        const newId = data?.id;
        // Gán quyền ngay sau khi tạo (POST chỉ nhận tên).
        if (newId && permissionIds.length) {
          await httpClient.put(`/api/v1/roles/${newId}`, { name: name.trim(), permissionIds });
        }
      }
      message.success('Đã lưu vai trò');
      qc.invalidateQueries({ queryKey: ROLES_KEY });
      onClose();
    } catch (e) {
      message.error(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  const loading = permissions.isLoading || (isEdit && detail.isLoading);

  return (
    <Drawer
      open
      width={640}
      title={isEdit ? 'Sửa vai trò' : 'Thêm vai trò'}
      onClose={onClose}
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
          <Button onClick={onClose}>Huỷ</Button>
          <Button type="primary" loading={saving} onClick={save}>
            Lưu
          </Button>
        </div>
      }
    >
      <div className="mb-4">
        <label className="mb-1.5 block text-[12.5px] font-medium text-[var(--tk-heading)]">
          Tên vai trò <span className="text-[var(--tk-danger)]">*</span>
        </label>
        <Input value={name} onChange={(e) => setName((e.target as HTMLInputElement).value)} placeholder="Nhập tên vai trò" />
      </div>

      <div className="mb-2 text-[12.5px] font-medium text-[var(--tk-heading)]">Phân quyền ({selected.size} quyền đã chọn)</div>

      {loading ? (
        <div style={{ padding: 24, textAlign: 'center', color: 'var(--tk-muted)' }}>Đang tải…</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 12 }}>
          {groups.map((g) => {
            const total = g.items.length;
            const on = g.items.filter((p) => selected.has(p.id)).length;
            const allOn = on === total && total > 0;
            return (
              <div key={g.name} className="rf-card" style={{ padding: 12 }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8, paddingBottom: 8, borderBottom: '1px solid var(--tk-line)' }}>
                  <Checkbox
                    checked={allOn}
                    onChange={(e) => toggleGroup(g.items, (e.target as HTMLInputElement).checked)}
                  >
                    <span style={{ fontWeight: 600, color: 'var(--tk-heading)' }}>{g.name}</span>
                  </Checkbox>
                  <span style={{ fontSize: 12, color: 'var(--tk-muted-2)', fontFamily: 'var(--tk-font-mono)' }}>
                    {on}/{total}
                  </span>
                </div>
                <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(220px, 1fr))', gap: '6px 12px' }}>
                  {g.items.map((p) => (
                    <Checkbox key={p.id} checked={selected.has(p.id)} onChange={(e) => toggleOne(p.id, (e.target as HTMLInputElement).checked)}>
                      <span title={p.code} style={{ fontSize: 13, color: 'var(--tk-body)' }}>{p.name}</span>
                    </Checkbox>
                  ))}
                </div>
              </div>
            );
          })}
        </div>
      )}
    </Drawer>
  );
}

export function RolesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('user.manage');

  const roles = useRoles();
  const remove = useDeleteRole();
  const [editor, setEditor] = useState<{ id: string | null; name: string } | null>(null);

  async function handleDelete(id: string) {
    try {
      await remove.mutateAsync(id);
      message.success('Đã xoá vai trò');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<RoleListItem> = [
    { title: 'Vai trò', key: 'name', render: (_: unknown, r: RoleListItem) => <CellText tone="heading" strong>{r.name}</CellText> },
    { title: 'Số quyền', dataIndex: 'permissionCount', key: 'permissionCount', width: 140, render: (v: number) => <CellStack main={v.toLocaleString('vi-VN')} mono /> },
    { title: 'Số người dùng', dataIndex: 'userCount', key: 'userCount', width: 160, render: (v: number) => <CellStack main={v.toLocaleString('vi-VN')} mono /> },
  ];

  const tableColumns: ColumnsType<RoleListItem> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, r: RoleListItem) => (
            <Space>
              <Button size="small" onClick={() => setEditor({ id: r.id, name: r.name })}>
                Sửa
              </Button>
              <Popconfirm title="Xoá vai trò này?" onConfirm={() => handleDelete(r.id)}>
                <Button size="small" danger loading={remove.isPending}>
                  Xoá
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
        title="Vai trò & quyền"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => setEditor({ id: null, name: '' })}>
              Thêm vai trò
            </Button>
          ) : null
        }
      />
      <DataCard title="Danh sách vai trò">
        <Table rowKey="id" columns={tableColumns} dataSource={roles.data ?? []} loading={roles.isLoading} pagination={false} />
      </DataCard>

      {editor ? <RoleEditorDrawer roleId={editor.id} initialName={editor.name} onClose={() => setEditor(null)} /> : null}
    </>
  );
}
