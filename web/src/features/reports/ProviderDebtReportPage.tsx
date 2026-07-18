import { useState } from 'react';
import { Table, Typography } from '../../shared/ui/antd';
import { DataCard, ExportButton } from '../../shared/ui';
import { PageHeader } from '../../shared/ui/PageHeader';
import type { ColumnsType } from '../../shared/ui/antd';
import { CellMoney } from '../../shared/ui/TableCells';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { useProviderDebt } from './providerDebtApi';
import type { ProviderDebtRow } from './providerDebtApi';
import { ProviderTransactionsModal } from './ProviderTransactionsModal';

// Ô tiền tuổi nợ: 0 hiện "—" mờ để bảng dễ đọc.
const aging = (v: number | null | undefined) =>
  v ? <CellMoney value={v} /> : <CellMoney value="—" tone="muted" />;

export function ProviderDebtReportPage() {
  const report = useProviderDebt();
  const [selected, setSelected] = useState<ProviderDebtRow | null>(null);

  const exportCsv = () =>
    exportRowsToCsv(
      'bao-cao-cong-no-ncc.csv',
      ['Nhà cung cấp', 'Tổng chi phí', 'Đã trả', 'Còn nợ', 'Trong hạn (0–30)', '31–60', '61–90', '>90'],
      (report.data ?? []).map((r) => [
        r.providerName,
        r.totalCost,
        r.paid,
        r.outstanding,
        r.current ?? 0,
        r.d30 ?? 0,
        r.d60 ?? 0,
        r.d90Plus ?? 0,
      ]),
    );

  const columns: ColumnsType<ProviderDebtRow> = [
    {
      title: 'Nhà cung cấp',
      dataIndex: 'providerName',
      key: 'providerName',
      render: (_: unknown, r: ProviderDebtRow) => (
        <Typography.Link onClick={() => setSelected(r)}>{r.providerName}</Typography.Link>
      ),
    },
    { title: 'Tổng chi phí', dataIndex: 'totalCost', key: 'totalCost', align: 'right', render: (v: number) => <CellMoney value={v} /> },
    { title: 'Đã trả', dataIndex: 'paid', key: 'paid', align: 'right', render: (v: number) => <CellMoney value={v} tone="success" /> },
    { title: 'Còn nợ', dataIndex: 'outstanding', key: 'outstanding', align: 'right', render: (v: number) => <CellMoney value={v} tone={v > 0 ? 'danger' : 'muted'} /> },
    { title: 'Trong hạn (0–30)', dataIndex: 'current', key: 'current', align: 'right', render: (v: number | null | undefined) => aging(v) },
    { title: '31–60', dataIndex: 'd30', key: 'd30', align: 'right', render: (v: number | null | undefined) => aging(v) },
    { title: '61–90', dataIndex: 'd60', key: 'd60', align: 'right', render: (v: number | null | undefined) => aging(v) },
    { title: '>90', dataIndex: 'd90Plus', key: 'd90Plus', align: 'right', render: (v: number | null | undefined) => aging(v) },
  ];

  return (
    <>
      <PageHeader
        title="Báo cáo công nợ phải trả NCC"
        extra={<ExportButton filename="bao-cao-cong-no-ncc.csv" onExport={exportCsv} title="Xuất toàn bộ báo cáo ra Excel/CSV" />}
      />
      <DataCard>
      <Table
        rowKey="providerId"
        columns={columns}
        dataSource={report.data ?? []}
        loading={report.isLoading}
        pagination={false}
      />
      </DataCard>

      <ProviderTransactionsModal
        providerId={selected?.providerId ?? null}
        providerName={selected?.providerName ?? ''}
        onClose={() => setSelected(null)}
      />
    </>
  );
}
