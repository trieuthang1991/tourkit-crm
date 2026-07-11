import { App, Button, Popconfirm, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { CheckboxField, NumberField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { paymentAccountCreateSchema, paymentAccountSchema } from './types';
import type { PaymentAccount, PaymentAccountCreateForm } from './types';

const QUERY_KEY = ['payment-accounts'];

function usePaymentAccounts() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payment-accounts');
      return z.array(paymentAccountSchema).parse(data);
    },
  });
}

function useCreatePaymentAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: PaymentAccountCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/payment-accounts', body);
      return paymentAccountSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdatePaymentAccount() {
  const qc = useQueryClient();
  return useMutation({
    // PUT trả 204 No Content — không parse body.
    mutationFn: async ({ id, body }: { id: string; body: PaymentAccountCreateForm }) => {
      await httpClient.put(`/api/v1/payment-accounts/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDeletePaymentAccount() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/payment-accounts/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const EMPTY: PaymentAccountCreateForm = {
  name: '',
  bankName: null,
  accountNumber: null,
  accountHolder: null,
  branch: null,
  transferNote: null,
  isDefault: false,
  sortOrder: 0,
};

const columns: ColumnsType<PaymentAccount> = [
  {
    title: 'Tên',
    dataIndex: 'name',
    key: 'name',
    render: (name: string, item: PaymentAccount) => (
      <Space>
        {name}
        {item.isDefault ? <Tag color="green">Mặc định</Tag> : null}
      </Space>
    ),
  },
  { title: 'Ngân hàng', dataIndex: 'bankName', key: 'bankName' },
  { title: 'Số TK', dataIndex: 'accountNumber', key: 'accountNumber' },
  { title: 'Chủ TK', dataIndex: 'accountHolder', key: 'accountHolder' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

function AccountFields() {
  return (
    <>
      <TextField name="name" label="Tên hiển thị" required />
      <TextField name="bankName" label="Ngân hàng" />
      <TextField name="accountNumber" label="Số tài khoản" />
      <TextField name="accountHolder" label="Chủ tài khoản" />
      <TextField name="branch" label="Chi nhánh" />
      <TextField name="transferNote" label="Nội dung chuyển khoản mặc định" />
      <CheckboxField name="isDefault" label="Tài khoản mặc định (in lên chứng từ)" />
      <NumberField name="sortOrder" label="Thứ tự" required />
    </>
  );
}

export function PaymentAccountsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<PaymentAccount | null>(null);
  const list = usePaymentAccounts();
  const create = useCreatePaymentAccount();
  const update = useUpdatePaymentAccount();
  const remove = useDeletePaymentAccount();

  const canManage = has('paymentaccount.manage');

  async function submit(values: PaymentAccountCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: PaymentAccountCreateForm) {
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

  const tableColumns: ColumnsType<PaymentAccount> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: PaymentAccount) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá tài khoản này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Tài khoản nhận tiền"
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
          title="Thêm tài khoản nhận tiền"
          schema={paymentAccountCreateSchema}
          defaultValues={EMPTY}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <AccountFields />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa tài khoản nhận tiền"
          schema={paymentAccountCreateSchema}
          defaultValues={{
            name: editing.name,
            bankName: editing.bankName,
            accountNumber: editing.accountNumber,
            accountHolder: editing.accountHolder,
            branch: editing.branch,
            transferNote: editing.transferNote,
            isDefault: editing.isDefault,
            sortOrder: editing.sortOrder,
          }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <AccountFields />
        </CrudFormModal>
      ) : null}
    </>
  );
}
