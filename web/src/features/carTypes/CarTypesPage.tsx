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
import { carTypeCreateSchema, carTypeSchema } from './types';
import type { CarType, CarTypeCreateForm } from './types';

const QUERY_KEY = ['car-types'];

function useCarTypes() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/car-types');
      return z.array(carTypeSchema).parse(data);
    },
  });
}

function useCreateCarType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CarTypeCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/car-types', body);
      return carTypeSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdateCarType() {
  const qc = useQueryClient();
  return useMutation({
    // PUT trả 204 No Content — không parse body.
    mutationFn: async ({ id, body }: { id: string; body: CarTypeCreateForm }) => {
      await httpClient.put(`/api/v1/car-types/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDeleteCarType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/car-types/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const columns: ColumnsType<CarType> = [
  { title: 'Số ghế', dataIndex: 'code', key: 'code', width: 100 },
  { title: 'Tên loại xe', dataIndex: 'name', key: 'name' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

export function CarTypesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<CarType | null>(null);
  const list = useCarTypes();
  const create = useCreateCarType();
  const update = useUpdateCarType();
  const remove = useDeleteCarType();

  const canManage = has('vehicle.manage');

  async function submit(values: CarTypeCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: CarTypeCreateForm) {
    if (!editing) {
      return;
    }
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

  const tableColumns: ColumnsType<CarType> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: CarType) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá loại xe này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Loại xe"
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
          title="Thêm loại xe"
          schema={carTypeCreateSchema}
          defaultValues={{ code: 0, name: '', sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <NumberField name="code" label="Số ghế" required />
          <TextField name="name" label="Tên loại xe (vd: Xe 45 chỗ)" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa loại xe"
          schema={carTypeCreateSchema}
          defaultValues={{ code: editing.code, name: editing.name, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <NumberField name="code" label="Số ghế" required />
          <TextField name="name" label="Tên loại xe (vd: Xe 45 chỗ)" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
