import { App, Button, Popconfirm, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useMemo, useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { PostCommentsModal } from './PostCommentsModal';
import {
  POST_STATUS_OPTIONS,
  postCategorySchema,
  postFormSchema,
  postSchema,
  postStatusLabel,
} from './types';
import type { Post, PostForm } from './types';

const KEY = ['posts'];

function usePosts() {
  return useQuery({
    queryKey: KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/posts');
      return z.array(postSchema).parse(data);
    },
  });
}

function useCategoryOptions() {
  return useQuery({
    queryKey: ['post-categories'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/post-categories');
      return z.array(postCategorySchema).parse(data);
    },
  });
}

function useCreate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: PostForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/posts', body);
      return postSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

function useUpdate() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: PostForm }) => {
      await httpClient.put(`/api/v1/posts/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

function useDelete() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/posts/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

const EMPTY: PostForm = { title: '', slug: '', summary: null, body: '', categoryId: null, status: 0, likeCount: 0 };

export function PostsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('post.manage');
  const [open, setOpen] = useState(false);
  const [editing, setEditing] = useState<Post | null>(null);
  const [commentsFor, setCommentsFor] = useState<Post | null>(null);
  const list = usePosts();
  const categories = useCategoryOptions();
  const create = useCreate();
  const update = useUpdate();
  const remove = useDelete();

  const categoryOptions = useMemo(
    () => (categories.data ?? []).map((c) => ({ label: c.name, value: c.id })),
    [categories.data],
  );

  async function submit(values: PostForm) {
    try {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: values });
      } else {
        await create.mutateAsync(values);
      }
      message.success('Đã lưu');
      setOpen(false);
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

  const columns: ColumnsType<Post> = [
    { title: 'Tiêu đề', dataIndex: 'title', key: 'title' },
    { title: 'Chuyên mục', dataIndex: 'categoryName', key: 'categoryName', render: (v: string | null) => v ?? '—' },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (v: number) => <Tag color={v === 1 ? 'green' : 'default'}>{postStatusLabel(v)}</Tag>,
    },
    {
      title: 'Xuất bản',
      dataIndex: 'publishedAt',
      key: 'publishedAt',
      render: (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—'),
    },
    { title: 'Lượt thích', dataIndex: 'likeCount', key: 'likeCount', width: 100 },
    {
      title: '',
      key: '__comments',
      width: 110,
      render: (_: unknown, item: Post) => (
        <Button size="small" onClick={() => setCommentsFor(item)}>
          Bình luận
        </Button>
      ),
    },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 160,
            render: (_: unknown, item: Post) => (
              <Space>
                <Button size="small" onClick={() => { setEditing(item); setOpen(true); }}>
                  Sửa
                </Button>
                <Popconfirm title="Xoá bài viết này?" onConfirm={() => handleDelete(item.id)}>
                  <Button size="small" danger loading={remove.isPending}>
                    Xoá
                  </Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<Post>[number],
        ]
      : []),
  ];

  const defaultValues: PostForm = editing
    ? {
        title: editing.title,
        slug: editing.slug,
        summary: editing.summary,
        body: editing.body,
        categoryId: editing.categoryId,
        status: editing.status,
        likeCount: editing.likeCount,
      }
    : EMPTY;

  return (
    <>
      <PageHeader
        title="Bài viết"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => { setEditing(null); setOpen(true); }}>
              Thêm bài viết
            </Button>
          ) : null
        }
      />
      <Table rowKey="id" columns={columns} dataSource={list.data ?? []} loading={list.isLoading} pagination={false} />
      {open ? (
        <CrudFormModal
          open={open}
          title={editing ? 'Sửa bài viết' : 'Thêm bài viết'}
          schema={postFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => { setOpen(false); setEditing(null); }}
          onSubmit={submit}
        >
          <TextField name="title" label="Tiêu đề" required />
          <TextField name="slug" label="Slug (đường dẫn)" required />
          <SelectField name="categoryId" label="Chuyên mục" options={categoryOptions} allowClear />
          <TextField name="summary" label="Tóm tắt" />
          <TextAreaField name="body" label="Nội dung" />
          <SelectField name="status" label="Trạng thái" options={POST_STATUS_OPTIONS} required />
          <NumberField name="likeCount" label="Lượt thích" />
        </CrudFormModal>
      ) : null}
      {commentsFor ? (
        <PostCommentsModal post={commentsFor} canManage={canManage} onClose={() => setCommentsFor(null)} />
      ) : null}
    </>
  );
}
