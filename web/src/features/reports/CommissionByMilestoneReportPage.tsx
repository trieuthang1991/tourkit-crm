import { useMemo, useState } from 'react';
import { Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import { DateRangeInput } from '../../ui/inputs';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useCommissionByMilestone } from './commissionByMilestoneApi';
import type { CommissionByMilestoneRow } from './commissionByMilestoneApi';
import { useUserOptions } from '../commission/commissionRulesApi';

/* Báo cáo "Hoa hồng theo mốc" (/reports/commission-by-milestone): % lấy từ bậc lợi nhuận của chính sách
   hoa hồng (CommissionCampaign) áp cho nhân viên; xuất hoa hồng theo lợi nhuận LẪN theo doanh thu.
   Đối chiếu legacy ReportCommissionByMilestone. */

export function CommissionByMilestoneReportPage() {
  const [from, setFrom] = useState<string | undefined>();
  const [to, setTo] = useState<string | undefined>();
  const report = useCommissionByMilestone({ from, to });

  const users = useUserOptions();
  const nameById = useMemo(() => {
    const m = new Map<string, string>();
    (users.data ?? []).forEach((u) => m.set(u.id, u.fullName || u.email));
    return m;
  }, [users.data]);
  const userName = (id: string) => nameById.get(id) ?? id;

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-hoa-hong-theo-moc.csv',
      ['Nhân viên', 'Chính sách', 'Doanh thu', 'Chi phí', 'Lợi nhuận', 'Tỉ lệ (%)', 'Hoa hồng theo lợi nhuận', 'Hoa hồng theo doanh thu'],
      (report.data ?? []).map((r) => [
        userName(r.userId),
        r.campaignName ?? '',
        r.turnover,
        r.cost,
        r.profit,
        r.commissionRate,
        r.commissionByProfit,
        r.commissionByRevenue,
      ]),
    );

  const columns: ColumnsType<CommissionByMilestoneRow> = [
    { title: 'Nhân viên', dataIndex: 'userId', key: 'userId', render: (v: string) => userName(v) },
    {
      title: 'Chính sách',
      dataIndex: 'campaignName',
      key: 'campaignName',
      render: (v: string | null) => (v ? <Tag color="green">{v}</Tag> : <span style={{ color: 'var(--tk-muted)' }}>—</span>),
    },
    { title: 'Doanh thu', dataIndex: 'turnover', key: 'turnover', render: (v: number) => money(v) },
    { title: 'Chi phí', dataIndex: 'cost', key: 'cost', render: (v: number) => money(v) },
    { title: 'Lợi nhuận', dataIndex: 'profit', key: 'profit', render: (v: number) => money(v) },
    { title: 'Tỉ lệ', dataIndex: 'commissionRate', key: 'commissionRate', render: (v: number) => `${v}%` },
    { title: 'HH theo lợi nhuận', dataIndex: 'commissionByProfit', key: 'commissionByProfit', render: (v: number) => money(v) },
    { title: 'HH theo doanh thu', dataIndex: 'commissionByRevenue', key: 'commissionByRevenue', render: (v: number) => money(v) },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo hoa hồng theo mốc"
        extra={<ExportButton filename="bao-cao-hoa-hong-theo-moc.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
        <div style={{ marginBottom: 12, maxWidth: 360 }}>
          <DateRangeInput
            from={from}
            to={to}
            placeholder={['Từ ngày tạo đơn', 'đến']}
            onChange={(f, t) => {
              setFrom(f);
              setTo(t);
            }}
          />
        </div>
        <Table
          rowKey="userId"
          columns={columns}
          dataSource={report.data ?? []}
          loading={report.isLoading}
          scroll={{ x: 'max-content' }}
          pagination={false}
        />
      </DataCard>
    </>
  );
}
