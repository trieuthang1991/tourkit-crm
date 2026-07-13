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
import { customerSourceCreateSchema, customerSourceSchema } from './types';
import type { CustomerSource, CustomerSourceCreateForm } from './types';

const QUERY_KEY = ['customer-sources'];

function useCustomerSources() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-sources');
      return z.array(customerSourceSchema).parse(data);
    },
  });
}

function useCreateCustomerSource() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CustomerSourceCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/customer-sources', body);
      return customerSourceSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdateCustomerSource() {
  const qc = useQueryClient();
  return useMutation({
    // PUT trả 204 No Content — không parse body, chỉ invalidate để refetch.
    mutationFn: async ({ id, body }: { id: string; body: CustomerSourceCreateForm }) => {
      await httpClient.put(`/api/v1/customer-sources/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDeleteCustomerSource() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/customer-sources/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const columns: ColumnsType<CustomerSource> = [
  { title: 'Tên nguồn khách', dataIndex: 'name', key: 'name' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

export function CustomerSourcesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerSource | null>(null);
  const list = useCustomerSources();
  const create = useCreateCustomerSource();
  const update = useUpdateCustomerSource();
  const remove = useDeleteCustomerSource();

  const canManage = has('customertype.manage');

  async function submit(values: CustomerSourceCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: CustomerSourceCreateForm) {
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

  const tableColumns: ColumnsType<CustomerSource> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: CustomerSource) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá nguồn khách này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Nguồn khách hàng"
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
          title="Thêm nguồn khách"
          schema={customerSourceCreateSchema}
          defaultValues={{ name: '', sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên nguồn khách" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa nguồn khách"
          schema={customerSourceCreateSchema}
          defaultValues={{ name: editing.name, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên nguồn khách" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
