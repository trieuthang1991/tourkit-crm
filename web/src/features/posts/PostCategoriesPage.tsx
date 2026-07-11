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
import { postCategoryCreateSchema, postCategorySchema } from './types';
import type { PostCategory, PostCategoryCreateForm } from './types';

const KEY = ['post-categories'];

function useCategories() {
  return useQuery({
    queryKey: KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/post-categories');
      return z.array(postCategorySchema).parse(data);
    },
  });
}

function useCreate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: PostCategoryCreateForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/post-categories', body);
      return postCategorySchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

function useUpdate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: PostCategoryCreateForm }) => {
      await httpClient.put(`/api/v1/post-categories/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

function useDelete() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/post-categories/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

const columns: ColumnsType<PostCategory> = [
  { title: 'Chuyên mục', dataIndex: 'name', key: 'name' },
  { title: 'Slug', dataIndex: 'slug', key: 'slug' },
  { title: 'Thứ tự', dataIndex: 'sortOrder', key: 'sortOrder' },
];

export function PostCategoriesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<PostCategory | null>(null);
  const list = useCategories();
  const create = useCreate();
  const update = useUpdate();
  const remove = useDelete();

  const canManage = has('post.manage');

  async function submit(values: PostCategoryCreateForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã lưu');
      setOpen(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function submitEdit(values: PostCategoryCreateForm) {
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

  const tableColumns: ColumnsType<PostCategory> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, item: PostCategory) => (
            <Space>
              <Button size="small" onClick={() => setEditing(item)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá chuyên mục này?" onConfirm={() => handleDelete(item.id)}>
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
        title="Chuyên mục bài viết"
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
          title="Thêm chuyên mục"
          schema={postCategoryCreateSchema}
          defaultValues={{ name: '', slug: '', sortOrder: 0 }}
          submitting={create.isPending}
          onCancel={() => setOpen(false)}
          onSubmit={submit}
        >
          <TextField name="name" label="Tên chuyên mục" required />
          <TextField name="slug" label="Slug" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
      {editing ? (
        <CrudFormModal
          open={!!editing}
          title="Sửa chuyên mục"
          schema={postCategoryCreateSchema}
          defaultValues={{ name: editing.name, slug: editing.slug, sortOrder: editing.sortOrder }}
          submitting={update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submitEdit}
        >
          <TextField name="name" label="Tên chuyên mục" required />
          <TextField name="slug" label="Slug" required />
          <NumberField name="sortOrder" label="Thứ tự" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
