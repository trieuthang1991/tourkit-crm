import { App, Button, Card, Input, InputNumber, Modal, Select, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import { useCreateOrderCost, useOrderCosts } from './orderCostApi';
import type { CreateOrderCostForm, OrderCost } from './orderCostApi';
import { providersCrud } from './providersCrud';

const EMPTY_FORM: CreateOrderCostForm = {
  providerId: '',
  serviceName: null,
  dayIndex: 1,
  expectedAmount: 0,
  actualAmount: 0,
  deposit: 0,
  surcharge: 0,
  vat: 0,
  status: 1,
};

function CreateOrderCostModal({ orderId, open, onClose }: { orderId: string; open: boolean; onClose: () => void }) {
  const { message } = App.useApp();
  const create = useCreateOrderCost(orderId);
  const providers = providersCrud.useList({ page: 1, size: 200 });
  const [form, setForm] = useState<CreateOrderCostForm>(EMPTY_FORM);

  const providerOptions = useMemo(
    () => (providers.data?.items ?? []).map((p) => ({ label: p.name, value: p.id })),
    [providers.data],
  );

  return (
    <Modal
      open={open}
      title="Thêm chi phí"
      onCancel={onClose}
      confirmLoading={create.isPending}
      destroyOnClose
      onOk={async () => {
        try {
          await create.mutateAsync(form);
          message.success('Đã thêm chi phí');
          setForm(EMPTY_FORM);
          onClose();
        } catch (e) {
          message.error(errorMessage(e));
        }
      }}
    >
      <Space direction="vertical" style={{ width: '100%' }}>
        <Select
          style={{ width: '100%' }}
          placeholder="Nhà cung cấp"
          loading={providers.isLoading}
          options={providerOptions}
          value={form.providerId ? form.providerId : undefined}
          onChange={(v) => setForm((f) => ({ ...f, providerId: v }))}
        />
        <Input
          value={form.serviceName ?? ''}
          onChange={(e) => setForm((f) => ({ ...f, serviceName: e.target.value ? e.target.value : null }))}
          placeholder="Dịch vụ"
        />
        <InputNumber
          style={{ width: '100%' }}
          value={form.dayIndex}
          onChange={(v) => setForm((f) => ({ ...f, dayIndex: v ?? 0 }))}
          placeholder="Ngày thứ"
        />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={form.expectedAmount}
          onChange={(v) => setForm((f) => ({ ...f, expectedAmount: v ?? 0 }))}
          placeholder="Chi phí dự kiến"
        />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={form.actualAmount}
          onChange={(v) => setForm((f) => ({ ...f, actualAmount: v ?? 0 }))}
          placeholder="Chi phí thực tế"
        />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={form.deposit}
          onChange={(v) => setForm((f) => ({ ...f, deposit: v ?? 0 }))}
          placeholder="Đặt cọc"
        />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={form.surcharge}
          onChange={(v) => setForm((f) => ({ ...f, surcharge: v ?? 0 }))}
          placeholder="Phụ thu"
        />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={form.vat}
          onChange={(v) => setForm((f) => ({ ...f, vat: v ?? 0 }))}
          placeholder="VAT"
        />
        <InputNumber
          style={{ width: '100%' }}
          value={form.status}
          onChange={(v) => setForm((f) => ({ ...f, status: v ?? 0 }))}
          placeholder="Trạng thái"
        />
      </Space>
    </Modal>
  );
}

export function OrderCostsPanel({ orderId }: { orderId: string }) {
  const { has } = useAuth();
  const [createOpen, setCreateOpen] = useState(false);
  const costs = useOrderCosts(orderId);
  const providers = providersCrud.useList({ page: 1, size: 200 });

  const providerName = useMemo(() => {
    const map = new Map((providers.data?.items ?? []).map((p) => [p.id, p.name]));
    return (providerId: string) => map.get(providerId) ?? providerId;
  }, [providers.data]);

  const columns: ColumnsType<OrderCost> = [
    { title: 'Dịch vụ', dataIndex: 'serviceName', key: 'serviceName' },
    {
      title: 'Nhà cung cấp',
      dataIndex: 'providerId',
      key: 'providerId',
      render: (v: string) => providerName(v),
    },
    { title: 'Ngày thứ', dataIndex: 'dayIndex', key: 'dayIndex' },
    {
      title: 'Chi phí dự kiến',
      dataIndex: 'expectedAmount',
      key: 'expectedAmount',
      render: (v: number) => money(v),
    },
    {
      title: 'Chi phí thực tế',
      dataIndex: 'actualAmount',
      key: 'actualAmount',
      render: (v: number) => money(v),
    },
    { title: 'VAT', dataIndex: 'vat', key: 'vat', render: (v: number) => money(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
  ];

  return (
    <Card title="Chi phí">
      <Space style={{ marginBottom: 16 }}>
        {has('cost.create') ? (
          <Button type="primary" onClick={() => setCreateOpen(true)}>
            Thêm chi phí
          </Button>
        ) : null}
      </Space>
      <Table rowKey="id" columns={columns} dataSource={costs.data ?? []} loading={costs.isLoading} pagination={false} />
      <CreateOrderCostModal orderId={orderId} open={createOpen} onClose={() => setCreateOpen(false)} />
    </Card>
  );
}
