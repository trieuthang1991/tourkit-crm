import { Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { money } from '../../shared/format';
import { useProviderDebt } from './providerDebtApi';
import type { ProviderDebtRow } from './providerDebtApi';

export function ProviderDebtReportPage() {
  const report = useProviderDebt();

  const columns: ColumnsType<ProviderDebtRow> = [
    { title: 'Nhà cung cấp', dataIndex: 'providerName', key: 'providerName' },
    { title: 'Tổng chi phí', dataIndex: 'totalCost', key: 'totalCost', render: (v: number) => money(v) },
    { title: 'Đã trả', dataIndex: 'paid', key: 'paid', render: (v: number) => money(v) },
    { title: 'Còn nợ', dataIndex: 'outstanding', key: 'outstanding', render: (v: number) => money(v) },
  ];

  return (
    <>
      <Typography.Title level={3}>Báo cáo công nợ phải trả NCC</Typography.Title>
      <Table
        rowKey="providerId"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
    </>
  );
}
