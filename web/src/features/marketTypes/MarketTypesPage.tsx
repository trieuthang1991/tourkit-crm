import { App, Button, Popconfirm, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { CatalogStatusTag } from '../../shared/ui/CatalogStatusTag';
import { NumberField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { marketTypeCreateSchema, marketTypeSchema } from './types';
import type { MarketType, MarketTypeCreateForm } from './types';

const QUERY_KEY = ['market-types'];

function useMarketTypes() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/market-types');
      return z.array(marketTypeSchema).parse(data);
    },
  });
}

function useCreateMarketType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: MarketTypeCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/market-types', body);
      return marketTypeSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdateMarketType() {
  const qc = useQueryClient();
  return useMutation({
    // PUT trả 204 No Content — không parse body, chỉ invalidate để refetch.
    mutationFn: async ({ id, body }: { id: string; body: MarketTypeCreateForm }) => {
      await httpClient.put(`/api/v1/market-types/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDeleteMarketType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/market-types/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const columns: ColumnsType<MarketType> = [
  { title: 'Tên', dataIndex: 'name', key: 'name' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => <CatalogStatusTag status={v} /> },
];

export function MarketTypesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<MarketType | null>(null);
  const list = useMarketTypes();
  const create = useCreateMarketType();
  const update = useUpdateMarketType();
  const remove = useDeleteMarketType();

  const canManage = has('market.manage');

  async function submit(values: MarketTypeCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: MarketTypeCreateForm) {
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

  const tableColumns: ColumnsType<MarketType> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: MarketType) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá loại thị trường này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Loại thị trường"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => setOpen(true)}>
              Thêm
            </Button>
          ) : null
        }
      />
      <Table
        rowKey="id"
        columns={tableColumns}
        dataSource={list.data ?? []}
        loading={list.isLoading}
        pagination={false}
      />
      {open ? (
        <CrudFormModal
          open={open}
          title="Thêm loại thị trường"
          schema={marketTypeCreateSchema}
          defaultValues={{ name: '', parentId: null, sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên" required />
          <TextField name="parentId" label="Danh mục cha (id)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa loại thị trường"
          schema={marketTypeCreateSchema}
          defaultValues={{ name: editing.name, parentId: editing.parentId, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên" required />
          <TextField name="parentId" label="Danh mục cha (id)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
