import { App, Button, Popconfirm, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { paymentTermCreateSchema, paymentTermSchema } from './types';
import type { PaymentTerm, PaymentTermCreateForm } from './types';

const QUERY_KEY = ['payment-terms'];

function usePaymentTerms() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payment-terms');
      return z.array(paymentTermSchema).parse(data);
    },
  });
}

function useCreate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: PaymentTermCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/payment-terms', body);
      return paymentTermSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: PaymentTermCreateForm }) => {
      await httpClient.put(`/api/v1/payment-terms/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDelete() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/payment-terms/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const columns: ColumnsType<PaymentTerm> = [
  { title: 'Điều khoản', dataIndex: 'name', key: 'name' },
  { title: 'Mô tả', dataIndex: 'description', key: 'description' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

export function PaymentTermsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<PaymentTerm | null>(null);
  const list = usePaymentTerms();
  const create = useCreate();
  const update = useUpdate();
  const remove = useDelete();

  const canManage = has('provider.create');

  async function submit(values: PaymentTermCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: PaymentTermCreateForm) {
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

  const tableColumns: ColumnsType<PaymentTerm> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: PaymentTerm) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá điều khoản này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Điều khoản thanh toán NCC"
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
          title="Thêm điều khoản"
          schema={paymentTermCreateSchema}
          defaultValues={{ name: '', description: null, sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên điều khoản" required />
          <TextAreaField name="description" label="Mô tả (vd: Cọc 30%, còn lại trước khởi hành 7 ngày)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa điều khoản"
          schema={paymentTermCreateSchema}
          defaultValues={{ name: editing.name, description: editing.description, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên điều khoản" required />
          <TextAreaField name="description" label="Mô tả (vd: Cọc 30%, còn lại trước khởi hành 7 ngày)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
