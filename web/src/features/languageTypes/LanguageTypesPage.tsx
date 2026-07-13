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
import { languageTypeCreateSchema, languageTypeSchema } from './types';
import type { LanguageType, LanguageTypeCreateForm } from './types';

const QUERY_KEY = ['language-types'];

function useLanguageTypes() {
  return useQuery({
    queryKey: QUERY_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/language-types');
      return z.array(languageTypeSchema).parse(data);
    },
  });
}

function useCreateLanguageType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: LanguageTypeCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/language-types', body);
      return languageTypeSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useUpdateLanguageType() {
  const qc = useQueryClient();
  return useMutation({
    // PUT trả 204 No Content — không parse body.
    mutationFn: async ({ id, body }: { id: string; body: LanguageTypeCreateForm }) => {
      await httpClient.put(`/api/v1/language-types/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

function useDeleteLanguageType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/language-types/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: QUERY_KEY }),
  });
}

const columns: ColumnsType<LanguageType> = [
  { title: 'Ngôn ngữ', dataIndex: 'name', key: 'name' },
  { title: 'Mã', dataIndex: 'code', key: 'code', width: 100 },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => <CatalogStatusTag status={v} /> },
];

export function LanguageTypesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<LanguageType | null>(null);
  const list = useLanguageTypes();
  const create = useCreateLanguageType();
  const update = useUpdateLanguageType();
  const remove = useDeleteLanguageType();

  const canManage = has('guide.manage');

  async function submit(values: LanguageTypeCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: LanguageTypeCreateForm) {
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

  const tableColumns: ColumnsType<LanguageType> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: LanguageType) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá ngôn ngữ này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Ngôn ngữ HDV"
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
          title="Thêm ngôn ngữ"
          schema={languageTypeCreateSchema}
          defaultValues={{ name: '', code: null, sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên ngôn ngữ" required />
          <TextField name="code" label="Mã ISO (vd: en, zh)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa ngôn ngữ"
          schema={languageTypeCreateSchema}
          defaultValues={{ name: editing.name, code: editing.code, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên ngôn ngữ" required />
          <TextField name="code" label="Mã ISO (vd: en, zh)" />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
