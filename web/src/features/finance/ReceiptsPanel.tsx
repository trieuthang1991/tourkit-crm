import { App, Button, Card, Descriptions, Input, InputNumber, Modal, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { useApproveReceipt, useBalance, useCreateReceipt, useReceipts, useRejectReceipt } from './financeApi';
import { RECEIPT_STATUS } from './receiptTypes';
import type { CreateReceiptForm, Receipt } from './receiptTypes';

const { TextArea } = Input;

function CreateReceiptModal({ orderId, open, onClose }: { orderId: string; open: boolean; onClose: () => void }) {
  const { message } = App.useApp();
  const create = useCreateReceipt(orderId);
  const [form, setForm] = useState<CreateReceiptForm>({ amount: 0, paymentMethod: '', partner: null, note: null });

  return (
    <Modal
      open={open}
      title="Tạo phiếu thu"
      onCancel={onClose}
      confirmLoading={create.isPending}
      destroyOnClose
      onOk={async () => {
        try {
          await create.mutateAsync(form);
          message.success('Đã tạo phiếu thu');
          setForm({ amount: 0, paymentMethod: '', partner: null, note: null });
          onClose();
        } catch (e) {
          message.error(errorMessage(e));
        }
      }}
    >
      <Space direction="vertical" style={{ width: '100%' }}>
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={form.amount}
          onChange={(v) => setForm((f) => ({ ...f, amount: v ?? 0 }))}
          placeholder="Số tiền"
        />
        <Input
          value={form.paymentMethod}
          onChange={(e) => setForm((f) => ({ ...f, paymentMethod: e.target.value }))}
          placeholder="Phương thức thanh toán"
        />
        <Input
          value={form.partner ?? ''}
          onChange={(e) => setForm((f) => ({ ...f, partner: e.target.value ? e.target.value : null }))}
          placeholder="Đối tác"
        />
        <TextArea
          rows={2}
          value={form.note ?? ''}
          onChange={(e) => setForm((f) => ({ ...f, note: e.target.value ? e.target.value : null }))}
          placeholder="Ghi chú"
        />
      </Space>
    </Modal>
  );
}

function ReceiptRowActions({ receipt, orderId }: { receipt: Receipt; orderId: string }) {
  const { has } = useAuth();
  const { message } = App.useApp();
  const approve = useApproveReceipt(orderId);
  const reject = useRejectReceipt(orderId);
  const isPending = receipt.status === 1 && !receipt.isRecognized;

  if (!has('receipt.approve') || !isPending) {
    return null;
  }

  return (
    <Space>
      <Button
        size="small"
        loading={approve.isPending}
        onClick={async () => {
          try {
            await approve.mutateAsync(receipt.id);
            message.success('Đã duyệt phiếu thu');
          } catch (e) {
            message.error(errorMessage(e));
          }
        }}
      >
        Duyệt
      </Button>
      <Button
        size="small"
        danger
        loading={reject.isPending}
        onClick={async () => {
          try {
            await reject.mutateAsync(receipt.id);
            message.success('Đã từ chối phiếu thu');
          } catch (e) {
            message.error(errorMessage(e));
          }
        }}
      >
        Từ chối
      </Button>
    </Space>
  );
}

export function ReceiptsPanel({ orderId }: { orderId: string }) {
  const { has } = useAuth();
  const [createOpen, setCreateOpen] = useState(false);
  const balance = useBalance(orderId);
  const receipts = useReceipts(orderId);

  const columns: ColumnsType<Receipt> = [
    { title: 'Mã phiếu', dataIndex: 'code', key: 'code' },
    { title: 'Số tiền', dataIndex: 'amount', key: 'amount', render: (v: number) => money(v) },
    { title: 'Phương thức', dataIndex: 'paymentMethod', key: 'paymentMethod' },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (v: number) => statusText(RECEIPT_STATUS, v),
    },
    {
      title: 'Đã ghi nhận',
      dataIndex: 'isRecognized',
      key: 'isRecognized',
      render: (v: boolean) => (v ? <Tag color="green">Có</Tag> : <Tag>Chưa</Tag>),
    },
    {
      title: '',
      key: '__actions',
      render: (_: unknown, receipt: Receipt) => <ReceiptRowActions receipt={receipt} orderId={orderId} />,
    },
  ];

  return (
    <Card title="Công nợ / Phiếu thu">
      <Descriptions column={3} bordered size="small" style={{ marginBottom: 16 }}>
        <Descriptions.Item label="Tổng tiền">{balance.data ? money(balance.data.total) : ''}</Descriptions.Item>
        <Descriptions.Item label="Đã thu">{balance.data ? money(balance.data.paid) : ''}</Descriptions.Item>
        <Descriptions.Item label="Còn lại">{balance.data ? money(balance.data.outstanding) : ''}</Descriptions.Item>
      </Descriptions>
      <Space style={{ marginBottom: 16 }}>
        {has('receipt.create') ? (
          <Button type="primary" onClick={() => setCreateOpen(true)}>
            Tạo phiếu thu
          </Button>
        ) : null}
      </Space>
      <Table
        rowKey="id"
        columns={columns}
        dataSource={receipts.data ?? []}
        loading={receipts.isLoading}
        pagination={false}
      />
      <CreateReceiptModal orderId={orderId} open={createOpen} onClose={() => setCreateOpen(false)} />
    </Card>
  );
}
