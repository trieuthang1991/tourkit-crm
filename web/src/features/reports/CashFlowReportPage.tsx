import { Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { money } from '../../shared/format';
import { useCashFlow } from './cashFlowApi';
import type { CashFlowRow } from './cashFlowApi';

export function CashFlowReportPage() {
  const report = useCashFlow();

  const columns: ColumnsType<CashFlowRow> = [
    { title: 'Phương thức', dataIndex: 'paymentMethod', key: 'paymentMethod' },
    { title: 'Thu vào', dataIndex: 'inflow', key: 'inflow', render: (v: number) => money(v) },
    { title: 'Chi ra', dataIndex: 'outflow', key: 'outflow', render: (v: number) => money(v) },
    { title: 'Ròng', dataIndex: 'net', key: 'net', render: (v: number) => money(v) },
  ];

  return (
    <>
      <Typography.Title level={3}>Báo cáo dòng tiền</Typography.Title>
      <Table
        rowKey="paymentMethod"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
    </>
  );
}
