import { Button, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import { money } from '../../shared/format';
import { useTurnover } from './turnoverApi';
import type { TurnoverRow } from './turnoverApi';

export function TurnoverReportPage() {
  const navigate = useNavigate();
  const report = useTurnover();

  const columns: ColumnsType<TurnoverRow> = [
    {
      title: 'Mã đơn',
      dataIndex: 'orderCode',
      key: 'orderCode',
      render: (v: string, row) => (
        <Button type="link" style={{ padding: 0 }} onClick={() => navigate(`/orders/${row.orderId}`)}>
          {v}
        </Button>
      ),
    },
    { title: 'Doanh thu', dataIndex: 'revenue', key: 'revenue', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', render: (v: number) => money(v) },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', render: (v: number) => money(v) },
  ];

  return (
    <>
      <Typography.Title level={3}>Báo cáo doanh thu – lợi nhuận</Typography.Title>
      <Table
        rowKey="orderId"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
    </>
  );
}
