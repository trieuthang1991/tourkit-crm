import { App, Button, Input, Select, Space, Steps, Typography } from 'antd';
import { useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { dateText } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { useActApproval, useApproval, useStartApproval } from './approvalApi';
import { APPROVAL_METHOD, APPROVAL_STATUS, STEP_STATUS } from './approvalTypes';
import type { StartApprovalStepInput } from './approvalTypes';

const { TextArea } = Input;

function stepAntStatus(status: number): 'wait' | 'process' | 'finish' | 'error' {
  if (status === 2) {
    return 'finish';
  }
  if (status === 3) {
    return 'error';
  }
  return 'process';
}

function StartApprovalForm({ receiptId }: { receiptId: string }) {
  const { message } = App.useApp();
  const start = useStartApproval(receiptId);
  const [method, setMethod] = useState<number>(1);
  const [steps, setSteps] = useState<StartApprovalStepInput[]>([{ stepOrder: 1, userIds: [] }]);

  function addStep() {
    setSteps((s) => [...s, { stepOrder: s.length + 1, userIds: [] }]);
  }

  function removeStep(index: number) {
    setSteps((s) => s.filter((_, i) => i !== index).map((step, i) => ({ ...step, stepOrder: i + 1 })));
  }

  function setStepUserIds(index: number, userIds: string[]) {
    setSteps((s) => s.map((step, i) => (i === index ? { ...step, userIds } : step)));
  }

  async function submit() {
    try {
      await start.mutateAsync({ method, steps });
      message.success('Đã khởi tạo quy trình duyệt');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <Space direction="vertical" style={{ width: '100%' }}>
      <Typography.Text strong>Khởi tạo duyệt</Typography.Text>
      <Select
        style={{ width: '100%' }}
        value={method}
        onChange={setMethod}
        options={Object.entries(APPROVAL_METHOD).map(([value, label]) => ({ value: Number(value), label }))}
      />
      {steps.map((step, index) => (
        <Space
          key={index}
          direction="vertical"
          style={{ width: '100%', border: '1px solid #eee', padding: 8 }}
        >
          <Typography.Text>Bước {step.stepOrder}</Typography.Text>
          <Select
            mode="tags"
            style={{ width: '100%' }}
            placeholder="Nhập userId (UUID) rồi Enter"
            value={step.userIds}
            onChange={(v) => setStepUserIds(index, v)}
          />
          {steps.length > 1 ? (
            <Button size="small" danger onClick={() => removeStep(index)}>
              Xoá bước
            </Button>
          ) : null}
        </Space>
      ))}
      <Button onClick={addStep}>Thêm bước</Button>
      <Button type="primary" loading={start.isPending} onClick={submit}>
        Khởi tạo
      </Button>
    </Space>
  );
}

function ActApprovalForm({ receiptId }: { receiptId: string }) {
  const { message } = App.useApp();
  const act = useActApproval(receiptId);
  const [note, setNote] = useState('');

  async function respond(approve: boolean) {
    try {
      await act.mutateAsync({ approve, note: note ? note : null });
      message.success(approve ? 'Đã duyệt' : 'Đã từ chối');
      setNote('');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <Space direction="vertical" style={{ width: '100%' }}>
      <TextArea rows={2} placeholder="Ghi chú" value={note} onChange={(e) => setNote(e.target.value)} />
      <Space>
        <Button type="primary" loading={act.isPending} onClick={() => respond(true)}>
          Duyệt
        </Button>
        <Button danger loading={act.isPending} onClick={() => respond(false)}>
          Từ chối
        </Button>
      </Space>
    </Space>
  );
}

export function ApprovalPanel({ receiptId }: { receiptId: string }) {
  const { has } = useAuth();
  const approval = useApproval(receiptId);

  if (approval.isLoading) {
    return <Typography.Text>Đang tải...</Typography.Text>;
  }

  if (!approval.data) {
    return has('receipt.approval.start') ? (
      <StartApprovalForm receiptId={receiptId} />
    ) : (
      <Typography.Text type="secondary">Chưa có quy trình duyệt.</Typography.Text>
    );
  }

  const a = approval.data;

  return (
    <Space direction="vertical" style={{ width: '100%' }}>
      <Typography.Text>Phương thức: {APPROVAL_METHOD[a.method] ?? a.method}</Typography.Text>
      <Typography.Text>Trạng thái: {APPROVAL_STATUS[a.status] ?? a.status}</Typography.Text>
      <Steps
        direction="vertical"
        size="small"
        current={a.currentStepOrder - 1}
        items={a.steps.map((step) => ({
          title: `Bước ${step.stepOrder} — ${step.userId}`,
          status: stepAntStatus(step.status),
          description: (
            <Space direction="vertical" size={0}>
              <span>Trạng thái: {STEP_STATUS[step.status] ?? step.status}</span>
              {step.actedAt ? <span>Thời gian: {dateText(step.actedAt)}</span> : null}
              {step.note ? <span>Ghi chú: {step.note}</span> : null}
            </Space>
          ),
        }))}
      />
      {a.status === 1 && has('receipt.approval.act') ? <ActApprovalForm receiptId={receiptId} /> : null}
    </Space>
  );
}
