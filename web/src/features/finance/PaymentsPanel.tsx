import { App, Button, Card, Input, InputNumber, Modal, Select, Space, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { providersCrud } from '../providers/providersCrud';
import { useApprovePayment, useCreatePayment, usePayments, useRejectPayment } from './paymentApi';
import { PAYMENT_STATUS } from './paymentTypes';
import type { CreatePaymentForm, Payment } from './paymentTypes';

const { TextArea } = Input;

const EMPTY_FORM: CreatePaymentForm = {
  providerId: null,
  orderCostId: null,
  amount: 0,
  paymentMethod: '',
  partner: null,
  receiverName: null,
  note: null,
};

function CreatePaymentModal({ orderId, open, onClose }: { orderId: string; open: boolean; onClose: () => void }) {
  const { message } = App.useApp();
  const create = useCreatePayment(orderId);
  const providers = providersCrud.useList({ page: 1, size: 200 });
  const [form, setForm] = useState<CreatePaymentForm>(EMPTY_FORM);

  const providerOptions = useMemo(
    () => (providers.data?.items ?? []).map((p) => ({ label: p.name, value: p.id })),
    [providers.data],
  );

  return (
    <Modal
      open={open}
      title="Tạo phiếu chi"
      onCancel={onClose}
      confirmLoading={create.isPending}
      destroyOnHidden
      onOk={async () => {
        try {
          await create.mutateAsync(form);
          message.success('Đã tạo phiếu chi');
          setForm(EMPTY_FORM);
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
        <Select
          style={{ width: '100%' }}
          placeholder="Nhà cung cấp"
          loading={providers.isLoading}
          options={providerOptions}
          value={form.providerId ? form.providerId : undefined}
          allowClear
          onChange={(v) => setForm((f) => ({ ...f, providerId: v ?? null }))}
        />
        <Input
          value={form.receiverName ?? ''}
          onChange={(e) => setForm((f) => ({ ...f, receiverName: e.target.value ? e.target.value : null }))}
          placeholder="Người nhận"
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

function PaymentRowActions({ payment, orderId }: { payment: Payment; orderId: string }) {
  const { has } = useAuth();
  const { message } = App.useApp();
  const approve = useApprovePayment(orderId);
  const reject = useRejectPayment(orderId);
  const canApproveReject = has('payment.approve') && payment.status === 0;

  return (
    <Space>
      {canApproveReject ? (
        <Button
          size="small"
          loading={approve.isPending}
          onClick={async () => {
            try {
              await approve.mutateAsync(payment.id);
              message.success('Đã duyệt phiếu chi');
            } catch (e) {
              message.error(errorMessage(e));
            }
          }}
        >
          Duyệt
        </Button>
      ) : null}
      {canApproveReject ? (
        <Button
          size="small"
          danger
          loading={reject.isPending}
          onClick={async () => {
            try {
              await reject.mutateAsync(payment.id);
              message.success('Đã từ chối phiếu chi');
            } catch (e) {
              message.error(errorMessage(e));
            }
          }}
        >
          Từ chối
        </Button>
      ) : null}
    </Space>
  );
}

export function PaymentsPanel({ orderId }: { orderId: string }) {
  const { has } = useAuth();
  const [createOpen, setCreateOpen] = useState(false);
  const payments = usePayments(orderId);

  const columns: ColumnsType<Payment> = [
    { title: 'Mã phiếu', dataIndex: 'code', key: 'code' },
    { title: 'Số tiền', dataIndex: 'amount', key: 'amount', render: (v: number) => money(v) },
    { title: 'Phương thức', dataIndex: 'paymentMethod', key: 'paymentMethod' },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      render: (v: number) => statusText(PAYMENT_STATUS, v),
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
      render: (_: unknown, payment: Payment) => <PaymentRowActions payment={payment} orderId={orderId} />,
    },
  ];

  return (
    <Card title="Phiếu chi">
      <Space style={{ marginBottom: 16 }}>
        {has('payment.create') ? (
          <Button type="primary" onClick={() => setCreateOpen(true)}>
            Tạo phiếu chi
          </Button>
        ) : null}
      </Space>
      <Table
        rowKey="id"
        columns={columns}
        dataSource={payments.data ?? []}
        loading={payments.isLoading}
        pagination={false}
      />
      <CreatePaymentModal orderId={orderId} open={createOpen} onClose={() => setCreateOpen(false)} />
    </Card>
  );
}
