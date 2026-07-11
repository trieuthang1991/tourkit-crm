import { App, Button, Popconfirm, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { roomClassCreateSchema, roomClassSchema } from './types';
import type { RoomClass, RoomClassCreateForm } from './types';

const KEY = ['room-classes'];

function useRoomClasses() {
  return useQuery({
    queryKey: KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/room-classes');
      return z.array(roomClassSchema).parse(data);
    },
  });
}

function useCreate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: RoomClassCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/room-classes', body);
      return roomClassSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

function useUpdate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: RoomClassCreateForm }) => {
      await httpClient.put(`/api/v1/room-classes/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

function useDelete() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/room-classes/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

const columns: ColumnsType<RoomClass> = [
  { title: 'Hạng phòng', dataIndex: 'name', key: 'name' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

export function RoomClassesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<RoomClass | null>(null);
  const list = useRoomClasses();
  const create = useCreate();
  const update = useUpdate();
  const remove = useDelete();

  const canManage = has('servicebooking.manage');

  async function submit(values: RoomClassCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: RoomClassCreateForm) {
    if (!editing) return;
    try {
      await update.mutateAsync({ id: editing.id, body: values });
      message.success('Đã lưu');
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

  const tableColumns: ColumnsType<RoomClass> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: RoomClass) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá hạng phòng này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Hạng phòng khách sạn"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => setOpen(true)}>
              Thêm
            </Button>
          ) : null
        }
      />
      <Table rowKey="id" columns={tableColumns} dataSource={list.data ?? []} loading={list.isLoading} pagination={false} />
      {open ? (
        <CrudFormModal
          open={open}
          title="Thêm hạng phòng"
          schema={roomClassCreateSchema}
          defaultValues={{ name: '', sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên hạng phòng (Standard/Deluxe/Suite)" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa hạng phòng"
          schema={roomClassCreateSchema}
          defaultValues={{ name: editing.name, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên hạng phòng (Standard/Deluxe/Suite)" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
