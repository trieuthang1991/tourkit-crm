import { App, Button, Card, Input, Modal, Select, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { useAuth } from '../auth/AuthContext';
import { departuresCrud } from './departuresApi';
import { useTransferOrder, useTransfers } from './transferApi';
import type { TourTransfer } from './transferApi';

export function OrderTransferPanel({ orderId }: { orderId: string }) {
  const { has } = useAuth();
  const { message } = App.useApp();
  const [open, setOpen] = useState(false);
  const [toDepartureId, setToDepartureId] = useState<string | undefined>();
  const [reason, setReason] = useState('');

  const history = useTransfers(orderId);
  const departures = departuresCrud.useList({ page: 1, size: 200 });
  const transfer = useTransferOrder(orderId);

  const departureName = useMemo(() => {
    const map = new Map((departures.data?.items ?? []).map((d) => [d.id, `${d.code} — ${d.title}`]));
    return (id: string) => map.get(id) ?? id;
  }, [departures.data]);

  const departureOptions = useMemo(
    () => (departures.data?.items ?? []).map((d) => ({ label: `${d.code} — ${d.title}`, value: d.id })),
    [departures.data],
  );

  async function onTransfer() {
    if (!toDepartureId) return;
    try {
      await transfer.mutateAsync({ toDepartureId, reason: reason.trim() || null });
      message.success('Đã chuyển chuyến');
      setOpen(false);
      setToDepartureId(undefined);
      setReason('');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<TourTransfer> = [
    { title: 'Từ chuyến', dataIndex: 'fromDepartureId', key: 'from', render: (v: string) => departureName(v) },
    { title: 'Sang chuyến', dataIndex: 'toDepartureId', key: 'to', render: (v: string) => departureName(v) },
    { title: 'Lý do', dataIndex: 'reason', key: 'reason', render: (v: string | null) => v ?? '—' },
    {
      title: 'Thời điểm',
      dataIndex: 'transferredAt',
      key: 'transferredAt',
      render: (v: string) => new Date(v).toLocaleString('vi-VN'),
    },
  ];

  return (
    <Card title="Chuyển chuyến (đổi lịch, giữ giá)">
      {has('booking.create') ? (
        <Space style={{ marginBottom: 16 }}>
          <Button type="primary" onClick={() => setOpen(true)}>
            Chuyển chuyến
          </Button>
        </Space>
      ) : null}
      <Table
        rowKey="id"
        columns={columns}
        dataSource={history.data ?? []}
        loading={history.isLoading}
        pagination={false}
        locale={{ emptyText: 'Chưa chuyển chuyến lần nào' }}
      />
      <Modal
        open={open}
        title="Chuyển đơn sang chuyến khác"
        okText="Chuyển"
        okButtonProps={{ disabled: !toDepartureId }}
        confirmLoading={transfer.isPending}
        onCancel={() => setOpen(false)}
        onOk={onTransfer}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <Select
            style={{ width: '100%' }}
            placeholder="Chuyến đích"
            showSearch
            optionFilterProp="label"
            loading={departures.isLoading}
            options={departureOptions}
            value={toDepartureId}
            onChange={setToDepartureId}
          />
          <Input.TextArea
            placeholder="Lý do (tuỳ chọn)"
            value={reason}
            onChange={(e) => setReason(e.target.value)}
            rows={2}
          />
        </Space>
      </Modal>
    </Card>
  );
}
