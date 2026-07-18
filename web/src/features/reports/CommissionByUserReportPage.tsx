import { useMemo } from 'react';
import { Table } from '../../shared/ui/antd';
import { DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import type { ColumnsType } from '../../shared/ui/antd';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useCommissionByUser } from './commissionByUserApi';
import type { CommissionByUserRow } from './commissionByUserApi';
import { useUserOptions } from '../commission/commissionRulesApi';

export function CommissionByUserReportPage() {
  const report = useCommissionByUser();
  const users = useUserOptions();
  const nameById = useMemo(() => {
    const m = new Map<string, string>();
    (users.data ?? []).forEach((u) => m.set(u.id, u.fullName || u.email));
    return m;
  }, [users.data]);
  const userName = (id: string) => nameById.get(id) ?? id;

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-hoa-hong.csv',
      ['Nhân viên', 'Doanh thu', 'Chi phí', 'Lợi nhuận', 'Tỉ lệ hoa hồng (%)', 'Hoa hồng'],
      (report.data ?? []).map((r) => [userName(r.userId), r.turnover, r.cost, r.profit, r.commissionRate, r.commissionAmount]),
    );

  const columns: ColumnsType<CommissionByUserRow> = [
    { title: 'Nhân viên', dataIndex: 'userId', key: 'userId', render: (v: string) => userName(v) },
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
      <PageHeader
        title="Báo cáo hoa hồng theo nhân viên"
        extra={<ExportButton filename="bao-cao-hoa-hong.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
      <Table
        rowKey="userId"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
      </DataCard>
    </>
  );
}
