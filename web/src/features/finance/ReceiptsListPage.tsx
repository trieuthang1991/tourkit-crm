import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { exportRowsToCsv } from '../../shared/exportCsv';
import { voucherTone, ExportButton } from '../../shared/ui';
import { Button, DataCard, SegmentTabs, StatGrid, StatusTag } from '../../ui/kit';
import { DateRangeInput, Input, NumberInput, SearchInput, Select } from '../../ui/inputs';
import { Popconfirm } from '../../ui/overlay';
import { useToast } from '../../ui/message';
import { Pagination, Table } from '../../ui/Table';
import type { Column } from '../../ui/Table';
import { CellStack, CellEntity, CellMoney } from '../../shared/ui/TableCells';
import { useAuth } from '../auth/AuthContext';
import { receiptListItemSchema, VOUCHER_STATUS } from './listTypes';
import type { ReceiptListItem } from './listTypes';
import { ReceiptDetailModal } from './ReceiptDetailModal';

/* Màn "Phiếu thu" (/receipts) — hệ Refined. KHÔNG antd.
   Dữ liệu/bộ lọc/quyền duyệt giữ NGUYÊN; chỉ đổi lớp trình bày. */

const KEY = ['receipts-all'];
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

export function ReceiptsListPage() {
  const message = useToast();
  const { has } = useAuth();
  const canApprove = has('receipt.approve');
  const qc = useQueryClient();
  const [detail, setDetail] = useState<ReceiptListItem | null>(null); // phiếu đang xem chi tiết (kích dòng)
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
    queryKey: ['receipts', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/receipts/stats');
      return statsSchema.parse(data);
    },
  });

  const list = useQuery({
    queryKey: [...KEY, page.page, page.size, q, status, rangeApplied, adv],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/receipts', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, status, from: rangeApplied.from, to: rangeApplied.to, ...adv }),
      });
      return pagedSchema(receiptListItemSchema).parse(data);
    },
  });

  const act = useMutation({
    mutationFn: async ({ id, action }: { id: string; action: 'approve' | 'reject' }) => {
      await httpClient.post(`/api/v1/receipts/${id}/${action}`);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: KEY });
      qc.invalidateQueries({ queryKey: ['receipts', 'stats'] });
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
  const rows = list.data?.items ?? [];

  // Xuất CSV trang hiện tại.
  const exportCsv = () =>
    exportRowsToCsv(
      'phieu-thu.csv',
      ['STT', 'Mã phiếu', 'Ngày', 'Khách hàng', 'Mã đơn', 'Người nộp', 'Hình thức', 'Số tiền', 'Trạng thái'],
      rows.map((r, i) => [
        (page.page - 1) * page.size + i + 1,
        r.code,
        dateVi(r.issuedAt),
        r.customerName ?? '',
        r.orderCode ?? '',
        r.partner ?? '',
        r.paymentMethod,
        r.amount,
        VOUCHER_STATUS[r.status] ?? r.status,
      ]),
    );

  const columns: Column<ReceiptListItem>[] = [
    { key: '__stt', title: '#', width: 46, align: 'center', mono: true, render: (_r, i) => (page.page - 1) * page.size + i + 1 },
    { key: 'code', title: 'Phiếu thu', width: 152, render: (r) => <CellStack main={r.code} sub={dateVi(r.issuedAt)} tone="accent" mono subMono /> },
    { key: 'customer', title: 'Khách / Đơn', width: 210, render: (r) => <CellEntity name={r.customerName} code={r.orderCode} /> },
    { key: 'partner', title: 'Người nộp / Hình thức', width: 194, render: (r) => <CellStack main={r.partner} sub={r.paymentMethod} /> },
    { key: 'amount', title: 'Số tiền', width: 158, align: 'right', render: (r) => <CellMoney value={r.amount} tone="success" sub={r.isRecognized ? 'Đã ghi nhận' : undefined} subTone="muted" /> },
    { key: 'status', title: 'Trạng thái', width: 122, render: (r) => <StatusTag tone={voucherTone(r.status)}>{VOUCHER_STATUS[r.status] ?? r.status}</StatusTag> },
    ...(canApprove
      ? [
          {
            key: '__actions',
            title: '',
            width: 164,
            render: (r: ReceiptListItem) =>
              r.status === 0 ? (
                <span style={{ display: 'inline-flex', gap: 6 }} onClick={(e) => e.stopPropagation()}>
                  <Popconfirm title="Duyệt phiếu thu này?" okText="Duyệt" onConfirm={() => run(r.id, 'approve')}>
                    <Button variant="primary" size="sm" icon="check">
                      Duyệt
                    </Button>
                  </Popconfirm>
                  <Popconfirm title="Từ chối phiếu này?" okText="Từ chối" onConfirm={() => run(r.id, 'reject')}>
                    <Button variant="danger" size="sm" icon="close">
                      Từ chối
                    </Button>
                  </Popconfirm>
                </span>
              ) : null,
          } as Column<ReceiptListItem>,
        ]
      : []),
  ];

  // Tổng cộng trang hiện tại.
  const sumAmount = rows.reduce((a, r) => a + (r.amount ?? 0), 0);
  const summary = (
    <td colSpan={columns.length}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px 22px', alignItems: 'baseline' }}>
        <strong style={{ color: 'var(--tk-heading)' }}>Tổng cộng (trang này)</strong>
        <span style={{ color: 'var(--tk-muted-2)', fontSize: 12 }}>
          Tổng tiền: <strong style={{ fontFamily: 'var(--tk-font-mono)', color: 'var(--tk-heading)' }}>{money(sumAmount)}</strong>
        </span>
      </div>
    </td>
  );

  const s = stats.data;

  return (
    <>
      <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 16 }}>
        <h1 className="rf-page__title" style={{ margin: 0 }}>Phiếu thu</h1>
        <ExportButton filename="phieu-thu.csv" onExport={exportCsv} />
      </div>

      <StatGrid
        style={{ marginTop: 16 }}
        items={[
          { label: 'Tổng số phiếu', value: (s?.total ?? 0).toLocaleString('vi-VN') },
          { label: 'Tổng tiền', value: money(s?.totalAmount ?? 0) },
          { label: 'Chờ duyệt', value: (s?.pending ?? 0).toLocaleString('vi-VN') },
          { label: 'Đã duyệt', value: (s?.approved ?? 0).toLocaleString('vi-VN') },
          { label: 'Từ chối', value: (s?.rejected ?? 0).toLocaleString('vi-VN') },
        ]}
      />

      <div className="rf-card" style={{ padding: 12, marginTop: 16 }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: 10 }}>
          <SearchInput value={search} onChange={setSearch} onEnter={applyFilters} placeholder="Tìm theo mã phiếu / mã đơn / khách / người nộp" style={{ gridColumn: 'span 2' }} />
          <DateRangeInput from={range.from} to={range.to} placeholder={['Từ ngày', 'đến ngày']} onChange={(f, t) => setRange({ from: f, to: t })} />
          <Input value={pm} onChange={setPm} onEnter={applyFilters} placeholder="PT thanh toán" />
          <NumberInput value={amtFrom ?? null} min={0} placeholder="Số tiền từ" onChange={(v) => setAmtFrom(v ?? undefined)} />
          <NumberInput value={amtTo ?? null} min={0} placeholder="đến" onChange={(v) => setAmtTo(v ?? undefined)} />
          <Select allowClear showSearch placeholder="Chi nhánh" options={branchOpts} value={brId} onChange={(v) => setBrId((v as string) ?? undefined)} />
          <Select allowClear showSearch placeholder="NV phụ trách" options={userOpts} value={suId} onChange={(v) => setSuId((v as string) ?? undefined)} />
        </div>
        <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
          <Button variant="primary" icon="search" onClick={applyFilters}>
            Tìm kiếm
          </Button>
          <Button onClick={resetFilters}>Đặt lại</Button>
        </div>
      </div>

      <div style={{ marginTop: 14, overflowX: 'auto' }}>
        <SegmentTabs
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => {
            setStatus(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: 'Tất cả', value: 'all' }, ...Object.entries(VOUCHER_STATUS).map(([value, label]) => ({ label, value }))]}
        />
      </div>

      <div style={{ marginTop: 14 }}>
        <DataCard title="Danh sách phiếu thu" bodyless>
          <Table columns={columns} data={rows} rowKey={(r) => r.id} loading={list.isLoading} minWidth={1060} summary={rows.length ? summary : undefined} empty="Không có phiếu thu" onRowClick={setDetail} />
          <div style={{ padding: '10px 16px' }}>
            <Pagination page={page.page} pageSize={page.size} total={list.data?.total ?? 0} unit="phiếu" onChange={(p) => setPage({ ...page, page: p })} />
          </div>
        </DataCard>
      </div>

      <ReceiptDetailModal
        receipt={detail}
        onClose={() => setDetail(null)}
        canApprove={canApprove}
        onAct={run}
      />
    </>
  );
}
