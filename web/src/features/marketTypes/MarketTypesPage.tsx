import { App, Button, Table } from 'antd';
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

const columns: ColumnsType<MarketType> = [
  { title: 'Tên', dataIndex: 'name', key: 'name' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function MarketTypesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const list = useMarketTypes();
  const create = useCreateMarketType();

  const canCreate = has('market.manage');

  async function submit(values: MarketTypeCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <>
      <PageHeader
        title="Loại thị trường"
        extra={
          canCreate ? (
            <Button type="primary" onClick={() => setOpen(true)}>
              Thêm
            </Button>
          ) : null
        }
      />
      <Table
        rowKey="id"
        columns={columns}
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
    </>
  );
}
