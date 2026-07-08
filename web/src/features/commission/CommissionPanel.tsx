import { App, Button, Card, Descriptions, Input, InputNumber, Modal, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { useCreateProfitShare, useOrderProfit, useProfitShares } from './commissionApi';
import type { CreateProfitShareForm, ProfitShare } from './commissionApi';

const EMPTY_FORM: CreateProfitShareForm = { userId: '', percentage: 0 };

function CreateProfitShareModal({ orderId, open, onClose }: { orderId: string; open: boolean; onClose: () => void }) {
  const { message } = App.useApp();
  const create = useCreateProfitShare(orderId);
  const [form, setForm] = useState<CreateProfitShareForm>(EMPTY_FORM);

  return (
    <Modal
      open={open}
      title="Thêm chia lợi nhuận"
      onCancel={onClose}
      confirmLoading={create.isPending}
      destroyOnClose
      onOk={async () => {
        try {
          await create.mutateAsync(form);
          message.success('Đã thêm chia lợi nhuận');
          setForm(EMPTY_FORM);
          onClose();
        } catch (e) {
          message.error(errorMessage(e));
        }
      }}
    >
      <Space direction="vertical" style={{ width: '100%' }}>
        <Input
          value={form.userId}
          onChange={(e) => setForm((f) => ({ ...f, userId: e.target.value }))}
          placeholder="ID người dùng"
        />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          max={100}
          value={form.percentage}
          onChange={(v) => setForm((f) => ({ ...f, percentage: v ?? 0 }))}
          placeholder="Tỉ lệ (%)"
        />
      </Space>
    </Modal>
  );
}

export function CommissionPanel({ orderId }: { orderId: string }) {
  const { has } = useAuth();
  const [createOpen, setCreateOpen] = useState(false);
  const profit = useOrderProfit(orderId);
  const shares = useProfitShares(orderId);

  const columns: ColumnsType<ProfitShare> = [
    { title: 'ID người dùng', dataIndex: 'userId', key: 'userId' },
    { title: 'Tỉ lệ (%)', dataIndex: 'percentage', key: 'percentage' },
    { title: 'Số tiền', dataIndex: 'amount', key: 'amount', render: (v: number) => money(v) },
    { title: 'Lợi nhuận gốc', dataIndex: 'profitBase', key: 'profitBase', render: (v: number) => money(v) },
  ];

  return (
    <Card title="Lợi nhuận">
      <Descriptions column={3} bordered size="small" style={{ marginBottom: 16 }}>
        <Descriptions.Item label="Doanh thu">{profit.data ? money(profit.data.revenue) : ''}</Descriptions.Item>
        <Descriptions.Item label="Chi phí">{profit.data ? money(profit.data.cost) : ''}</Descriptions.Item>
        <Descriptions.Item label="Lợi nhuận">{profit.data ? money(profit.data.profit) : ''}</Descriptions.Item>
      </Descriptions>
      <Space style={{ marginBottom: 16 }}>
        {has('commission.create') ? (
          <Button type="primary" onClick={() => setCreateOpen(true)}>
            Thêm chia lợi nhuận
          </Button>
        ) : null}
      </Space>
      <Table
        rowKey="id"
        columns={columns}
        dataSource={shares.data ?? []}
        loading={shares.isLoading}
        pagination={false}
      />
      <CreateProfitShareModal orderId={orderId} open={createOpen} onClose={() => setCreateOpen(false)} />
    </Card>
  );
}
