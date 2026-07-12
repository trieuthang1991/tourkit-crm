import { App, Button, Card, Col, DatePicker, Input, InputNumber, Popconfirm, Row, Segmented, Space, Statistic, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
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
import { receiptListItemSchema, VOUCHER_STATUS, voucherStatusColor } from './listTypes';
import type { ReceiptListItem } from './listTypes';

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
  const { message } = App.useApp();
  const { has } = useAuth();
  const canApprove = has('receipt.approve');
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
  const [adv, setAdv] = useState<{ paymentMethod?: string; amountFrom?: number; amountTo?: number }>({});

  const applyFilters = () => {
    setQ(search);
    setRangeApplied(range);
    setAdv({ paymentMethod: pm || undefined, amountFrom: amtFrom, amountTo: amtTo });
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
    setAdv({});
    setPage({ ...page, page: 1 });
  };

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

  const columns: ColumnsType<ReceiptListItem> = [
    {
      title: 'STT',
      key: '__stt',
      width: 60,
      fixed: 'left',
      align: 'center',
      render: (_: unknown, __: ReceiptListItem, index: number) => (page.page - 1) * page.size + index + 1,
    },
    { title: 'Mã phiếu', dataIndex: 'code', key: 'code', fixed: 'left', width: 140 },
    { title: 'Mã đơn', dataIndex: 'orderCode', key: 'orderCode', width: 130, render: (v: string | null) => v ?? '—' },
    { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName', width: 170, render: (v: string | null) => v ?? '—' },
    { title: 'Số tiền', dataIndex: 'amount', key: 'amount', width: 140, align: 'right', render: (v: number) => money(v) },
    { title: 'Hình thức', dataIndex: 'paymentMethod', key: 'paymentMethod', width: 110 },
    { title: 'Ngày', dataIndex: 'issuedAt', key: 'issuedAt', width: 110, render: dateVi },
    { title: 'Người nộp', dataIndex: 'partner', key: 'partner', width: 150, render: (v: string | null) => v ?? '—' },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (s: number) => <Tag color={voucherStatusColor(s)}>{VOUCHER_STATUS[s] ?? s}</Tag>,
    },
    ...(canApprove
      ? [
          {
            title: '',
            key: '__actions',
            width: 170,
            fixed: 'right' as const,
            render: (_: unknown, r: ReceiptListItem) =>
              r.status === 0 ? (
                <Space>
                  <Popconfirm title="Duyệt phiếu thu này?" onConfirm={() => run(r.id, 'approve')}>
                    <Button size="small" type="primary">
                      Duyệt
                    </Button>
                  </Popconfirm>
                  <Popconfirm title="Từ chối phiếu này?" onConfirm={() => run(r.id, 'reject')}>
                    <Button size="small" danger>
                      Từ chối
                    </Button>
                  </Popconfirm>
                </Space>
              ) : null,
          } as ColumnsType<ReceiptListItem>[number],
        ]
      : []),
  ];

  const s = stats.data;
  const statCards = [
    { title: 'Tổng số phiếu', value: s?.total ?? 0, money: false },
    { title: 'Tổng tiền', value: s?.totalAmount ?? 0, money: true },
    { title: 'Chờ duyệt', value: s?.pending ?? 0, money: false },
    { title: 'Đã duyệt', value: s?.approved ?? 0, money: false },
    { title: 'Từ chối', value: s?.rejected ?? 0, money: false },
  ];

  return (
    <>
      <PageHeader title="Phiếu thu" />

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {statCards.map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} formatter={c.money ? (v) => money(Number(v)) : undefined} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search
              allowClear
              placeholder="Tìm theo mã phiếu / mã đơn / khách / người nộp"
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
          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>
                Tìm kiếm
              </Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => {
            setStatus(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: 'Tất cả', value: 'all' }, ...Object.entries(VOUCHER_STATUS).map(([value, label]) => ({ label, value }))]}
        />
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 'max-content' }}
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
    </>
  );
}
