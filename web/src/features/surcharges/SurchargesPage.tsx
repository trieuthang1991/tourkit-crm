import { App, Button, Popconfirm, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { CALC_TYPE_OPTIONS, surchargeCreateSchema, surchargeSchema } from './types';
import type { Surcharge, SurchargeCreateForm } from './types';

const QUERY_KEY = ['surcharges'];

function useSurcharges() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/surcharges');
      return z.array(surchargeSchema).parse(data);
    },
  });
}

function useCreate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: SurchargeCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/surcharges', body);
      return surchargeSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: SurchargeCreateForm }) => {
      await httpClient.put(`/api/v1/surcharges/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDelete() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/surcharges/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const calcName = (v: number) => CALC_TYPE_OPTIONS.find((o) => o.value === v)?.label ?? '';

const columns: ColumnsType<Surcharge> = [
  { title: 'Tên phụ thu', dataIndex: 'name', key: 'name' },
  { title: 'Cách tính', dataIndex: 'calcType', key: 'calcType', render: (v: number) => calcName(v) },
  {
    title: 'Giá trị mặc định',
    dataIndex: 'defaultValue',
    key: 'defaultValue',
    render: (v: number, r: Surcharge) => (r.calcType === 1 ? `${v}%` : money(v)),
  },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

export function SurchargesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Surcharge | null>(null);
  const list = useSurcharges();
  const create = useCreate();
  const update = useUpdate();
  const remove = useDelete();

  const canManage = has('booking.create');

  async function submit(values: SurchargeCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: SurchargeCreateForm) {
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

  const tableColumns: ColumnsType<Surcharge> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: Surcharge) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá loại phụ thu này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Loại phụ thu"
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
          title="Thêm loại phụ thu"
          schema={surchargeCreateSchema}
          defaultValues={{ name: '', calcType: 0, defaultValue: 0, sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên phụ thu" required />
          <SelectField name="calcType" label="Cách tính" options={CALC_TYPE_OPTIONS} required />
          <NumberField name="defaultValue" label="Giá trị mặc định (số tiền hoặc %)" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa loại phụ thu"
          schema={surchargeCreateSchema}
          defaultValues={{ name: editing.name, calcType: editing.calcType, defaultValue: editing.defaultValue, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên phụ thu" required />
          <SelectField name="calcType" label="Cách tính" options={CALC_TYPE_OPTIONS} required />
          <NumberField name="defaultValue" label="Giá trị mặc định (số tiền hoặc %)" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
