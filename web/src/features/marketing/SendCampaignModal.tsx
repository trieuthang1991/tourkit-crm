import { App, Input, Modal } from 'antd';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';

const { TextArea } = Input;

export function SendCampaignModal({
  campaignId,
  open,
  onClose,
}: {
  campaignId: string;
  open: boolean;
  onClose: () => void;
}) {
  const { message } = App.useApp();
  const qc = useQueryClient();
  const [text, setText] = useState('');

  const send = useMutation({
    mutationFn: async (recipients: string[]) => {
      const { data } = await httpClient.post<{ sent: number }>(
        `/api/v1/marketing/campaigns/${campaignId}/send`,
        { recipients },
      );
      return data;
    },
    onSuccess: (data) => {
      message.success(`Đã gửi ${data.sent} người nhận`);
      qc.invalidateQueries({ queryKey: ['campaigns'] });
      setText('');
      onClose();
    },
    onError: (e: unknown) => message.error(errorMessage(e)),
  });

  return (
    <Modal
      open={open}
      title="Gửi chiến dịch"
      onCancel={onClose}
      onOk={() => {
        const recipients = text
          .split(/[\n,]+/)
          .map((s) => s.trim())
          .filter((s) => s.length > 0);
        send.mutate(recipients);
      }}
      confirmLoading={send.isPending}
      destroyOnClose
    >
      <TextArea
        rows={6}
        placeholder="Nhập danh sách người nhận, mỗi dòng hoặc phân tách bằng dấu phẩy"
        value={text}
        onChange={(e) => setText(e.target.value)}
      />
    </Modal>
  );
}
