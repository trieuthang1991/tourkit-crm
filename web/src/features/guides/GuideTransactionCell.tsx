import { App, Button, Input, InputNumber, Modal, Popconfirm, Select, Space, Statistic, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { useAuth } from '../auth/AuthContext';
import {
  guideTxType,
  useCreateGuideTransaction,
  useDeleteGuideTransaction,
  useGuideSettlement,
} from './guideTransactionApi';
import type { GuideTransaction } from './guideTransactionApi';

const TYPE_OPTIONS = [
  { value: guideTxType.revenue, label: 'Thu' },
  { value: guideTxType.expense, label: 'Chi' },
];

export function GuideTransactionCell({ assignmentId }: { assignmentId: string }) {
  const { has } = useAuth();
  const { message } = App.useApp();
  const [open, setOpen] = useState(false);
  const [type, setType] = useState<number>(guideTxType.revenue);
  const [amount, setAmount] = useState(0);
  const [description, setDescription] = useState('');

  const settlement = useGuideSettlement(assignmentId, open);
  const create = useCreateGuideTransaction(assignmentId);
  const remove = useDeleteGuideTransaction(assignmentId);
  const canManage = has('guide.manage');

  async function add() {
    if (amount <= 0 || !description.trim()) {
      message.error('Nhập số tiền > 0 và diễn giải');
      return;
    }
    try {
      await create.mutateAsync({ type, amount, description: description.trim() });
      setAmount(0);
      setDescription('');
      message.success('Đã ghi');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<GuideTransaction> = [
    { title: 'Loại', dataIndex: 'type', key: 'type', render: (v: number) => (v === guideTxType.expense ? 'Chi' : 'Thu') },
    { title: 'Diễn giải', dataIndex: 'description', key: 'description' },
    { title: 'Số tiền', dataIndex: 'amount', key: 'amount', render: (v: number) => money(v) },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 80,
            render: (_: unknown, item: GuideTransaction) => (
              <Popconfirm title="Xoá dòng này?" onConfirm={() => remove.mutateAsync(item.id).catch((e) => message.error(errorMessage(e)))}>
                <Button size="small" danger>
                  Xoá
                </Button>
              </Popconfirm>
            ),
          } as ColumnsType<GuideTransaction>[number],
        ]
      : []),
  ];

  return (
    <>
      <Button size="small" onClick={() => setOpen(true)}>
        Thu-chi
      </Button>
      <Modal open={open} title="Thu-chi HDV theo chuyến" footer={null} onCancel={() => setOpen(false)} width={640}>
        <Space size="large" style={{ marginBottom: 16 }}>
          <Statistic title="Tổng thu" value={settlement.data?.totalRevenue ?? 0} formatter={() => money(settlement.data?.totalRevenue ?? 0)} />
          <Statistic title="Tổng chi" value={settlement.data?.totalExpense ?? 0} formatter={() => money(settlement.data?.totalExpense ?? 0)} />
          <Statistic title="Net (thu − chi)" value={settlement.data?.net ?? 0} formatter={() => money(settlement.data?.net ?? 0)} />
        </Space>
        <Table
          rowKey="id"
          size="small"
          columns={columns}
          dataSource={settlement.data?.items ?? []}
          loading={settlement.isLoading}
          pagination={false}
        />
        {canManage ? (
          <Space style={{ marginTop: 16 }} wrap>
            <Select style={{ width: 90 }} options={TYPE_OPTIONS} value={type} onChange={setType} />
            <InputNumber style={{ width: 140 }} min={0} placeholder="Số tiền" value={amount} onChange={(v) => setAmount(v ?? 0)} />
            <Input style={{ width: 240 }} placeholder="Diễn giải" value={description} onChange={(e) => setDescription(e.target.value)} />
            <Button type="primary" loading={create.isPending} onClick={add}>
              Ghi
            </Button>
          </Space>
        ) : null}
      </Modal>
    </>
  );
}
