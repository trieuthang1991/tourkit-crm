import { Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { money } from '../../shared/format';
import { useCommissionByUser } from './commissionByUserApi';
import type { CommissionByUserRow } from './commissionByUserApi';

export function CommissionByUserReportPage() {
  const report = useCommissionByUser();

  const columns: ColumnsType<CommissionByUserRow> = [
    { title: 'ID người dùng', dataIndex: 'userId', key: 'userId' },
    { title: 'Doanh thu', dataIndex: 'turnover', key: 'turnover', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', render: (v: number) => money(v) },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', render: (v: number) => money(v) },
    {
      title: 'Tỉ lệ hoa hồng',
      dataIndex: 'commissionRate',
      key: 'commissionRate',
      render: (v: number) => `${v}%`,
    },
    {
      title: 'Hoa hồng',
      dataIndex: 'commissionAmount',
      key: 'commissionAmount',
      render: (v: number) => money(v),
    },
  ];

  return (
    <>
      <Typography.Title level={3}>Báo cáo hoa hồng theo nhân viên</Typography.Title>
      <Table
        rowKey="userId"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
    </>
  );
}
