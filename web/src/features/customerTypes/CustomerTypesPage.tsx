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
import { customerTypeCreateSchema, customerTypeSchema } from './types';
import type { CustomerType, CustomerTypeCreateForm } from './types';

const QUERY_KEY = ['customer-types'];

function useCustomerTypes() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-types');
      return z.array(customerTypeSchema).parse(data);
    },
  });
}

function useCreateCustomerType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CustomerTypeCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/customer-types', body);
      return customerTypeSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdateCustomerType() {
  const qc = useQueryClient();
  return useMutation({
    // PUT trả 204 No Content — không parse body, chỉ invalidate để refetch.
    mutationFn: async ({ id, body }: { id: string; body: CustomerTypeCreateForm }) => {
      await httpClient.put(`/api/v1/customer-types/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDeleteCustomerType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/customer-types/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const columns: ColumnsType<CustomerType> = [
  { title: 'Mã', dataIndex: 'code', key: 'code', width: 80 },
  { title: 'Tên loại khách', dataIndex: 'name', key: 'name' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function CustomerTypesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerType | null>(null);
  const list = useCustomerTypes();
  const create = useCreateCustomerType();
  const update = useUpdateCustomerType();
  const remove = useDeleteCustomerType();

  const canManage = has('customertype.manage');

  async function submit(values: CustomerTypeCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: CustomerTypeCreateForm) {
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

  const tableColumns: ColumnsType<CustomerType> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: CustomerType) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá loại khách này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Loại khách hàng"
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
          title="Thêm loại khách"
          schema={customerTypeCreateSchema}
          defaultValues={{ code: 1, name: '', sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <NumberField name="code" label="Mã (khớp loại khách trên khách hàng)" required />
          <TextField name="name" label="Tên loại khách" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa loại khách"
          schema={customerTypeCreateSchema}
          defaultValues={{ code: editing.code, name: editing.name, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <NumberField name="code" label="Mã (khớp loại khách trên khách hàng)" required />
          <TextField name="name" label="Tên loại khách" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
