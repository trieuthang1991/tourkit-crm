import { App, Button, Card, Input, InputNumber, Modal, Popconfirm, Select, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import {
  surchargeCalcType,
  useCreateOrderSurcharge,
  useDeleteOrderSurcharge,
  useOrderSurcharges,
  useSurchargeCatalog,
} from './surchargeApi';
import type { CreateOrderSurchargeForm, OrderSurcharge } from './surchargeApi';

const CALC_OPTIONS = [
  { label: 'Số tiền cố định', value: surchargeCalcType.fixed },
  { label: '% trên giá gốc', value: surchargeCalcType.percent },
];

const calcLabel = (v: number) => (v === surchargeCalcType.percent ? '%' : 'VND');

const EMPTY_FORM: CreateOrderSurchargeForm = {
  surchargeId: null,
  description: '',
  calcType: surchargeCalcType.fixed,
  value: 0,
};

function CreateSurchargeModal({ orderId, open, onClose }: { orderId: string; open: boolean; onClose: () => void }) {
  const { message } = App.useApp();
  const create = useCreateOrderSurcharge(orderId);
  const catalog = useSurchargeCatalog();
  const [form, setForm] = useState<CreateOrderSurchargeForm>(EMPTY_FORM);

  const catalogOptions = useMemo(
    () =>
      (catalog.data ?? []).map((s) => ({
        label: `${s.name} (${s.calcType === surchargeCalcType.percent ? `${s.defaultValue}%` : money(s.defaultValue)})`,
        value: s.id,
      })),
    [catalog.data],
  );

  return (
    <Modal
      open={open}
      title="Thêm phụ thu"
      onCancel={onClose}
      confirmLoading={create.isPending}
      destroyOnHidden
      onOk={async () => {
        try {
          await create.mutateAsync(form);
          message.success('Đã thêm phụ thu');
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
          placeholder="Chọn từ danh mục phụ thu (tuỳ chọn)"
          allowClear
          loading={catalog.isLoading}
          options={catalogOptions}
          value={form.surchargeId ?? undefined}
          onChange={(v) => {
            const picked = (catalog.data ?? []).find((s) => s.id === v);
            setForm((f) => ({
              ...f,
              surchargeId: v ?? null,
              description: picked?.name ?? f.description,
              calcType: picked?.calcType ?? f.calcType,
              value: picked ? picked.defaultValue : f.value,
            }));
          }}
        />
        <Input
          value={form.description}
          onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
          placeholder="Diễn giải"
        />
        <Select
          style={{ width: '100%' }}
          options={CALC_OPTIONS}
          value={form.calcType}
          onChange={(v) => setForm((f) => ({ ...f, calcType: v }))}
        />
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={form.value}
          onChange={(v) => setForm((f) => ({ ...f, value: v ?? 0 }))}
          placeholder={form.calcType === surchargeCalcType.percent ? 'Phần trăm' : 'Số tiền'}
        />
      </Space>
    </Modal>
  );
}

export function OrderSurchargesPanel({ orderId }: { orderId: string }) {
  const { has } = useAuth();
  const [createOpen, setCreateOpen] = useState(false);
  const surcharges = useOrderSurcharges(orderId);
  const remove = useDeleteOrderSurcharge(orderId);
  const { message } = App.useApp();

  async function onDelete(id: string) {
    try {
      await remove.mutateAsync(id);
      message.success('Đã xoá phụ thu');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const canManage = has('booking.create');

  const columns: ColumnsType<OrderSurcharge> = [
    { title: 'Diễn giải', dataIndex: 'description', key: 'description' },
    { title: 'Cách tính', dataIndex: 'calcType', key: 'calcType', render: (v: number) => (v === surchargeCalcType.percent ? '%' : 'Cố định') },
    { title: 'Giá trị', dataIndex: 'value', key: 'value', render: (v: number, r: OrderSurcharge) => (r.calcType === surchargeCalcType.percent ? `${v}${calcLabel(v)}` : money(v)) },
    { title: 'Thành tiền', dataIndex: 'amount', key: 'amount', render: (v: number) => money(v) },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 100,
            render: (_: unknown, item: OrderSurcharge) => (
              <Popconfirm title="Xoá phụ thu này?" onConfirm={() => onDelete(item.id)}>
                <Button size="small" danger loading={remove.isPending}>
                  Xoá
                </Button>
              </Popconfirm>
            ),
          } as ColumnsType<OrderSurcharge>[number],
        ]
      : []),
  ];

  return (
    <Card title="Phụ thu (cộng vào doanh thu đơn)">
      <Space style={{ marginBottom: 16 }}>
        {canManage ? (
          <Button type="primary" onClick={() => setCreateOpen(true)}>
            Thêm phụ thu
          </Button>
        ) : null}
      </Space>
      <Table rowKey="id" columns={columns} dataSource={surcharges.data ?? []} loading={surcharges.isLoading} pagination={false} />
      <CreateSurchargeModal orderId={orderId} open={createOpen} onClose={() => setCreateOpen(false)} />
    </Card>
  );
}
