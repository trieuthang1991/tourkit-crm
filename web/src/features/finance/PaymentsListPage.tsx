import { App, Card, Col, DatePicker, Input, InputNumber, Popconfirm, Row, Select, Space, Table } from '../../shared/ui/antd';
import { Button, DataCard, ExportButton, SegmentTabs, StatusTag, voucherTone } from '../../shared/ui';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { StatGrid } from '../../ui/kit';
import { CellEntity, CellMoney, CellStack, CellText } from '../../shared/ui/TableCells';
import type { ColumnsType } from '../../shared/ui/antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { paymentListItemSchema, VOUCHER_STATUS } from './listTypes';
import type { PaymentListItem } from './listTypes';
import { PaymentDetailModal } from './PaymentDetailModal';

const KEY = ['payments-all'];
const statsSchema = z.object({
  total: z.number(),
  totalAmount: z.number(),
  pending: z.number(),
  approved: z.number(),
  rejected: z.number(),
});

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function PaymentsListPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canApprove = has('payment.approve');
  const [detail, setDetail] = useState<PaymentListItem | null>(null); // phiếu đang xem chi tiết (kích dòng)
  const qc = useQueryClient();
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [status, setStatus] = useState<number | undefined>();
  const [range, setRange] = useState<{ from?: string; to?: string }>({});
  const [rangeApplied, setRangeApplied] = useState<{ from?: string; to?: string }>({});
  const [pm, setPm] = useState('');
  const [amtFrom, setAmtFrom] = useState<number | undefined>();
  const [amtTo, setAmtTo] = useState<number | undefined>();
  const [brId, setBrId] = useState<string | undefined>();
  const [suId, setSuId] = useState<string | undefined>();
  const [adv, setAdv] = useState<{ paymentMethod?: string; amountFrom?: number; amountTo?: number; branchId?: string; salesUserId?: string }>({});

  const applyFilters = () => {
    setQ(search);
    setRangeApplied(range);
    setAdv({ paymentMethod: pm || undefined, amountFrom: amtFrom, amountTo: amtTo, branchId: brId, salesUserId: suId });
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setStatus(undefined);
    setRange({});
    setRangeApplied({});
    setPm('');
    setAmtFrom(undefined);
    setAmtTo(undefined);
    setBrId(undefined);
    setSuId(undefined);
    setAdv({});
    setPage({ ...page, page: 1 });
  };

  const branches = useQuery({
    queryKey: ['branches'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/branches');
      return z.array(z.object({ id: z.string().uuid(), name: z.string() })).parse(data);
    },
  });
  const users = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(z.object({ id: z.string().uuid(), fullName: z.string() })).parse(data);
    },
  });
  const branchOpts = (branches.data ?? []).map((b) => ({ label: b.name, value: b.id }));
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));

  const stats = useQuery({
    queryKey: ['payments', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payments/stats');
      return statsSchema.parse(data);
    },
  });

  const list = useQuery({
    queryKey: [...KEY, page.page, page.size, q, status, rangeApplied, adv],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payments', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, status, from: rangeApplied.from, to: rangeApplied.to, ...adv }),
      });
      return pagedSchema(paymentListItemSchema).parse(data);
    },
  });

  const act = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'approve' | 'reject' }) => {
      await httpClient.post(`/api/v1/payments/${id}/${action}`);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEY });
      qc.invalidateQueries({ queryKey: ['payments', 'stats'] });
    },
  });

  async function run(id: string, action: 'approve' | 'reject') {
    try {
      await act.mutateAsync({ id, action });
      message.success(action === 'approve' ? 'Đã duyệt' : 'Đã từ chối');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const dateVi = (v: string) => new Date(v).toLocaleDateString('vi-VN');

  // Xuất CSV trang hiện tại.
  const exportCsv = () =>
    exportRowsToCsv(
      'phieu-chi.csv',
      ['STT', 'Mã phiếu', 'Ngày', 'Nhà cung cấp', 'Mã đơn', 'Người nhận', 'Hình thức', 'Số tiền', 'Trạng thái'],
      (list.data?.items ?? []).map((r, i) => [
        (page.page - 1) * page.size + i + 1,
        r.code,
        dateVi(r.issuedAt),
        r.providerName ?? '',
        r.orderCode ?? '',
        r.receiverName ?? '',
        r.paymentMethod,
        r.amount,
        VOUCHER_STATUS[r.status] ?? r.status,
      ]),
    );

  const columns: ColumnsType<PaymentListItem> = [
    {
      title: 'STT',
      key: '__stt',
      width: 60,
      fixed: 'left',
      align: 'center',
      render: (_: unknown, __: PaymentListItem, index: number) => (page.page - 1) * page.size + index + 1,
    },
    {
      title: 'Phiếu chi',
      key: 'code',
      fixed: 'left',
      width: 160,
      render: (_: unknown, r: PaymentListItem) => <CellStack main={r.code} mono sub={dateVi(r.issuedAt)} subMono />,
    },
    {
      title: 'Đối tượng nhận',
      key: 'recipient',
      width: 280,
      render: (_: unknown, r: PaymentListItem) => <CellEntity name={r.providerName} code={r.orderCode} meta={r.receiverName} />,
    },
    {
      title: 'Hình thức',
      key: 'paymentMethod',
      width: 130,
      render: (_: unknown, r: PaymentListItem) => <CellText>{r.paymentMethod}</CellText>,
    },
    {
      title: 'Số tiền',
      key: 'amount',
      width: 170,
      align: 'right',
      render: (_: unknown, r: PaymentListItem) => <CellMoney value={r.amount} tone="danger" />,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 130,
      render: (s: number) => <StatusTag tone={voucherTone(s)}>{VOUCHER_STATUS[s] ?? s}</StatusTag>,
    },
    ...(canApprove
      ? [
          {
            title: '',
            key: '__actions',
            width: 170,
            fixed: 'right' as const,
            render: (_: unknown, r: PaymentListItem) =>
              r.status === 0 ? (
                <Space onClick={(e) => e.stopPropagation()}>
                  <Popconfirm title="Duyệt phiếu chi này?" onConfirm={() => run(r.id, 'approve')}>
                    <Button variant="primary" size="small">
                      Duyệt
                    </Button>
                  </Popconfirm>
                  <Popconfirm title="Từ chối phiếu này?" onConfirm={() => run(r.id, 'reject')}>
                    <Button variant="danger" size="small">
                      Từ chối
                    </Button>
                  </Popconfirm>
                </Space>
              ) : null,
          } as ColumnsType<PaymentListItem>[number],
        ]
      : []),
  ];

  const s = stats.data;

  return (
    <>
      <PageHeader title="Phiếu chi" extra={<ExportButton filename="phieu-chi.csv" onExport={exportCsv} />} />

      <StatGrid
        items={[
          { label: 'Tổng số phiếu', value: s?.total ?? 0 },
          { label: 'Tổng tiền', value: money(s?.totalAmount ?? 0) },
          { label: 'Chờ duyệt', value: s?.pending ?? 0 },
          { label: 'Đã duyệt', value: s?.approved ?? 0 },
          { label: 'Từ chối', value: s?.rejected ?? 0 },
        ]}
        style={{ marginBottom: 16 }}
      />

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search
              allowClear
              placeholder="Tìm theo mã phiếu / mã đơn / NCC / người nhận"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onSearch={applyFilters}
            />
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <DatePicker.RangePicker
              style={{ width: '100%' }}
              placeholder={['Từ ngày', 'đến ngày']}
              value={range.from && range.to ? [dayjs(range.from), dayjs(range.to)] : null}
              onChange={(d) => setRange({ from: d?.[0]?.startOf('day').toISOString(), to: d?.[1]?.endOf('day').toISOString() })}
            />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Input allowClear placeholder="PT thanh toán" value={pm} onChange={(e) => setPm(e.target.value)} onPressEnter={applyFilters} />
          </Col>
          <Col xs={6} sm={4} lg={2}>
            <InputNumber style={{ width: '100%' }} placeholder="Số tiền từ" min={0} value={amtFrom} onChange={(v) => setAmtFrom(v ?? undefined)} />
          </Col>
          <Col xs={6} sm={4} lg={2}>
            <InputNumber style={{ width: '100%' }} placeholder="đến" min={0} value={amtTo} onChange={(v) => setAmtTo(v ?? undefined)} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Chi nhánh"
              options={branchOpts} value={brId} onChange={(v) => setBrId(v ?? undefined)} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="NV phụ trách"
              options={userOpts} value={suId} onChange={(v) => setSuId(v ?? undefined)} />
          </Col>
          <Col span={24}>
            <Space>
              <Button variant="primary" onClick={applyFilters}>
                Tìm kiếm
              </Button>
              <Button variant="ghost" onClick={resetFilters}>Đặt lại</Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <SegmentTabs
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => {
            setStatus(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: 'Tất cả', value: 'all' }, ...Object.entries(VOUCHER_STATUS).map(([value, label]) => ({ label, value }))]}
        />
      </div>

      <DataCard title="Danh sách phiếu chi">
      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        onRow={(record) => ({ onClick: () => setDetail(record), style: { cursor: 'pointer' } })}
        scroll={{ x: 1080 }}
        pagination={{
          current: page.page,
          pageSize: page.size,
          total: list.data?.total ?? 0,
          showSizeChanger: true,
          onChange: (p, sz) => setPage({ page: p, size: sz }),
        }}
        summary={(pageData) => {
          const sum = pageData.reduce((a, r) => a + (r.amount ?? 0), 0);
          return (
            <Table.Summary fixed>
              <Table.Summary.Row>
                <Table.Summary.Cell index={0} colSpan={columns.length}>
                  <Space size="large">
                    <strong>Tổng cộng (trang này)</strong>
                    <span>Tổng tiền: <strong>{money(sum)}</strong></span>
                  </Space>
                </Table.Summary.Cell>
              </Table.Summary.Row>
            </Table.Summary>
          );
        }}
      />
      </DataCard>

      <PaymentDetailModal payment={detail} onClose={() => setDetail(null)} canApprove={canApprove} onAct={run} />
    </>
  );
}
