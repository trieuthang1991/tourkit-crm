import { Button, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useNavigate } from 'react-router-dom';
import { money } from '../../shared/format';
import { useOrderDebt } from './reportApi';
import type { OrderDebtRow } from './reportApi';

export function OrderDebtReportPage() {
  const navigate = useNavigate();
  const report = useOrderDebt();

  const columns: ColumnsType<OrderDebtRow> = [
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
    { title: 'Tổng tiền', dataIndex: 'total', key: 'total', render: (v: number) => money(v) },
    { title: 'Đã thu', dataIndex: 'paid', key: 'paid', render: (v: number) => money(v) },
    { title: 'Còn nợ', dataIndex: 'outstanding', key: 'outstanding', render: (v: number) => money(v) },
  ];

  return (
    <>
      <Typography.Title level={3}>Báo cáo công nợ</Typography.Title>
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
