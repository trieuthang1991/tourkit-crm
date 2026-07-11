import { App, Button, Input, Modal, Typography } from 'antd';
import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { useAuth } from '../auth/AuthContext';
import type { GuideAssignment } from './guideAssignmentTypes';

/// Bàn giao HDV (legacy HandoverNote): HDV nộp biên bản sau tour. Hiện nội dung đã nộp + cho phép ghi/sửa.
export function GuideHandoverCell({ item }: { item: GuideAssignment }) {
  const { has } = useAuth();
  const { message } = App.useApp();
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);
  const [content, setContent] = useState(item.handoverContent ?? '');
  const canManage = has('guide.manage');

  const submit = useMutation({
    mutationFn: async () => {
      await httpClient.post(`/api/v1/guide-assignments/${item.id}/handover`, { content: content.trim() });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['guideAssignments'] });
      message.success('Đã bàn giao');
      setOpen(false);
    },
    onError: (e) => message.error(errorMessage(e)),
  });

  return (
    <>
      <Button size="small" onClick={() => { setContent(item.handoverContent ?? ''); setOpen(true); }}>
        {item.handedOverAt ? 'Đã bàn giao' : 'Bàn giao'}
      </Button>
      <Modal
        open={open}
        title="Biên bản bàn giao HDV"
        okText="Nộp bàn giao"
        okButtonProps={{ disabled: !canManage || !content.trim() }}
        confirmLoading={submit.isPending}
        onCancel={() => setOpen(false)}
        onOk={() => submit.mutate()}
      >
        {item.handedOverAt ? (
          <Typography.Paragraph type="secondary">
            Đã bàn giao lúc {new Date(item.handedOverAt).toLocaleString('vi-VN')}.
          </Typography.Paragraph>
        ) : null}
        <Input.TextArea
          rows={5}
          placeholder="Nội dung bàn giao: sự cố, còn tồn, phản hồi khách…"
          value={content}
          onChange={(e) => setContent(e.target.value)}
          disabled={!canManage}
        />
      </Modal>
    </>
  );
}
