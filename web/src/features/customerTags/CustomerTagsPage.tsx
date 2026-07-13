import { App, Button, Popconfirm, Space, Table, Tag } from 'antd';
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
import { customerTagCreateSchema, customerTagSchema } from './types';
import type { CustomerTag, CustomerTagCreateForm } from './types';

const QUERY_KEY = ['customer-tags'];

function useCustomerTags() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customer-tags');
      return z.array(customerTagSchema).parse(data);
    },
  });
}

function useCreateCustomerTag() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CustomerTagCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/customer-tags', body);
      return customerTagSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdateCustomerTag() {
  const qc = useQueryClient();
  return useMutation({
    // PUT trả 204 No Content — không parse body, chỉ invalidate để refetch.
    mutationFn: async ({ id, body }: { id: string; body: CustomerTagCreateForm }) => {
      await httpClient.put(`/api/v1/customer-tags/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDeleteCustomerTag() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/customer-tags/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const columns: ColumnsType<CustomerTag> = [
  {
    title: 'Nhãn',
    dataIndex: 'name',
    key: 'name',
    render: (name: string, item: CustomerTag) => <Tag color={item.color ?? undefined}>{name}</Tag>,
  },
  { title: 'Màu', dataIndex: 'color', key: 'color' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

export function CustomerTagsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<CustomerTag | null>(null);
  const list = useCustomerTags();
  const create = useCreateCustomerTag();
  const update = useUpdateCustomerTag();
  const remove = useDeleteCustomerTag();

  const canManage = has('customertype.manage');

  async function submit(values: CustomerTagCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: CustomerTagCreateForm) {
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

  const tableColumns: ColumnsType<CustomerTag> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: CustomerTag) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá nhãn này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Nhãn khách hàng"
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
          title="Thêm nhãn khách"
          schema={customerTagCreateSchema}
          defaultValues={{ name: '', color: null, sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên nhãn" required />
          <TextField name="color" label="Màu (vd: gold, red, #1677ff)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa nhãn khách"
          schema={customerTagCreateSchema}
          defaultValues={{ name: editing.name, color: editing.color, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên nhãn" required />
          <TextField name="color" label="Màu (vd: gold, red, #1677ff)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
