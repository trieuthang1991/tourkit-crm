import { App, Button, Checkbox, Input, List, Modal, Popconfirm, Space, Tag, Typography } from 'antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { postCommentSchema } from './types';
import type { Post, PostComment } from './types';

const { TextArea } = Input;

function usePostComments(postId: string) {
  return useQuery({
    queryKey: ['post-comments', postId],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/posts/${postId}/comments`);
      return z.array(postCommentSchema).parse(data);
    },
  });
}

export function PostCommentsModal({
  post,
  canManage,
  onClose,
}: {
  post: Post;
  canManage: boolean;
  onClose: () => void;
}) {
  const { message } = App.useApp();
  const qc = useQueryClient();
  const list = usePostComments(post.id);
  const [authorName, setAuthorName] = useState('');
  const [content, setContent] = useState('');
  const [approved, setApproved] = useState(true);

  const invalidate = () => qc.invalidateQueries({ queryKey: ['post-comments', post.id] });

  const create = useMutation({
    mutationFn: async () => {
      await httpClient.post(`/api/v1/posts/${post.id}/comments`, {
        authorName,
        content,
        isApproved: approved,
      });
    },
    onSuccess: () => {
      setAuthorName('');
      setContent('');
      setApproved(true);
      invalidate();
    },
  });

  const setApproval = useMutation({
    mutationFn: async ({ id, approve }: { id: string; approve: boolean }) => {
      await httpClient.post(`/api/v1/posts/${post.id}/comments/${id}/${approve ? 'approve' : 'unapprove'}`);
    },
    onSuccess: invalidate,
  });

  const remove = useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/posts/${post.id}/comments/${id}`);
    },
    onSuccess: invalidate,
  });

  async function submit() {
    try {
      await create.mutateAsync();
      message.success('Đã thêm bình luận');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <Modal open title={`Bình luận — ${post.title}`} onCancel={onClose} footer={null} width={640}>
      <List
        dataSource={list.data ?? []}
        loading={list.isLoading}
        locale={{ emptyText: 'Chưa có bình luận' }}
        renderItem={(c: PostComment) => (
          <List.Item
            actions={
              canManage
                ? [
                    <Button
                      key="approve"
                      size="small"
                      onClick={() => setApproval.mutate({ id: c.id, approve: !c.isApproved })}
                    >
                      {c.isApproved ? 'Ẩn duyệt' : 'Duyệt'}
                    </Button>,
                    <Popconfirm key="del" title="Xoá bình luận này?" onConfirm={() => remove.mutate(c.id)}>
                      <Button size="small" danger>
                        Xoá
                      </Button>
                    </Popconfirm>,
                  ]
                : []
            }
          >
            <List.Item.Meta
              title={
                <Space>
                  <span>{c.authorName}</span>
                  <Tag color={c.isApproved ? 'green' : 'default'}>{c.isApproved ? 'Đã duyệt' : 'Chờ duyệt'}</Tag>
                </Space>
              }
              description={c.content}
            />
          </List.Item>
        )}
      />
      {canManage ? (
        <Space direction="vertical" style={{ width: '100%', marginTop: 16 }}>
          <Typography.Text strong>Thêm bình luận</Typography.Text>
          <Input placeholder="Tên người bình luận" value={authorName} onChange={(e) => setAuthorName(e.target.value)} />
          <TextArea rows={3} placeholder="Nội dung" value={content} onChange={(e) => setContent(e.target.value)} />
          <Checkbox checked={approved} onChange={(e) => setApproved(e.target.checked)}>
            Duyệt hiển thị ngay
          </Checkbox>
          <Button type="primary" loading={create.isPending} onClick={submit} disabled={!authorName || !content}>
            Thêm
          </Button>
        </Space>
      ) : null}
    </Modal>
  );
}
