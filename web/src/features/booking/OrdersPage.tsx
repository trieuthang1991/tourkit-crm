import { Button, Card, Col, DatePicker, Input, Row, Segmented, Select, Space, Statistic, Table, Tag, TreeSelect, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money, statusText } from '../../shared/format';
import { PageHeader } from '../../shared/ui/PageHeader';
import { ORDER_STATUS, orderSchema } from './seatTypes';
import type { Order } from './seatTypes';

const dateVi = (v?: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');
const dash = (v?: string | null) => v ?? '—';

const statsSchema = z.object({
  total: z.number(),
  totalRevenue: z.number(),
  totalPaid: z.number(),
  totalOutstanding: z.number(),
  draft: z.number(),
  confirmed: z.number(),
  cancelled: z.number(),
  unpaid: z.number(),
  deposit: z.number(),
  paid: z.number(),
});

const branchSchema = z.object({ id: z.string().uuid(), name: z.string() });
const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });
const ORDER_STATUS_OPTIONS = Object.entries(ORDER_STATUS).map(([value, label]) => ({ value: Number(value), label }));

type OrderAdv = {
  status?: number;
  salesUserId?: string;
  createdByUserId?: string;
  branchId?: string;
  departmentId?: string;
  createdFrom?: string;
  createdTo?: string;
  departureFrom?: string;
  departureTo?: string;
  tourType?: string;
  providerId?: string;
  marketTypeId?: string;
  tourGroupId?: string;
  bookingType?: number;
  commissionSettled?: boolean;
};

// Loại tour (legacy BookingType): FIT/GIT/LandTour/Booking/Dịch vụ/Visa/Xe.
const BOOKING_TYPE_OPTIONS = [
  { value: 0, label: 'Tour FIT' },
  { value: 1, label: 'Tour GIT' },
  { value: 2, label: 'LandTour/Combo' },
  { value: 3, label: 'Booking phòng' },
  { value: 4, label: 'Dịch vụ lẻ' },
  { value: 5, label: 'Visa' },
  { value: 6, label: 'Xe' },
];
const COMMISSION_OPTIONS = [
  { value: 'true', label: 'Đã chốt hoa hồng' },
  { value: 'false', label: 'Chưa chốt hoa hồng' },
];

// Bỏ field rỗng để không gửi param thừa.
function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function OrdersPage({ title = 'Đơn hàng' }: { title?: string } = {}) {
  const navigate = useNavigate();
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [payStatus, setPayStatus] = useState<number | undefined>();
  const [draft, setDraft] = useState<OrderAdv>({});
  const [adv, setAdv] = useState<OrderAdv>({});

  const setD = (patch: Partial<OrderAdv>) => setDraft((d) => ({ ...d, ...patch }));
  const applyFilters = () => {
    setQ(search);
    setAdv(draft);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setPayStatus(undefined);
    setDraft({});
    setAdv({});
    setPage({ ...page, page: 1 });
  };
  // Chip lọc nhanh thị trường — áp dụng ngay (không cần bấm Tìm kiếm).
  const pickMarket = (marketTypeId?: string) => {
    setDraft((d) => ({ ...d, marketTypeId }));
    setAdv((a) => ({ ...a, marketTypeId }));
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['orders', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/orders/stats');
      return statsSchema.parse(data);
    },
  });
  const branches = useQuery({
    queryKey: ['branches'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/branches');
      return z.array(branchSchema).parse(data);
    },
  });
  const users = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
  const departments = useQuery({
    queryKey: ['departments'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/departments');
      return z.array(z.object({ id: z.string().uuid(), name: z.string() })).parse(data);
    },
  });
  const filterOptions = useQuery({
    queryKey: ['orders', 'filter-options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/orders/filter-options');
      return z
        .object({
          tourTypes: z.array(z.string()),
          providers: z.array(z.object({ id: z.string().uuid(), name: z.string() })),
        })
        .parse(data);
    },
  });
  const marketTypes = useQuery({
    queryKey: ['market-types'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/market-types');
      return z.array(z.object({ id: z.string().uuid(), name: z.string(), parentId: z.string().uuid().nullable() })).parse(data);
    },
  });
  const tourGroups = useQuery({
    queryKey: ['tour-groups'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/tour-groups');
      return z.array(z.object({ id: z.string().uuid(), name: z.string() })).parse(data);
    },
  });
  const branchOpts = (branches.data ?? []).map((b) => ({ label: b.name, value: b.id }));
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));
  const deptOpts = (departments.data ?? []).map((d) => ({ label: d.name, value: d.id }));
  const tourTypeOpts = (filterOptions.data?.tourTypes ?? []).map((t) => ({ label: t, value: t }));
  const providerOpts = (filterOptions.data?.providers ?? []).map((p) => ({ label: p.name, value: p.id }));
  // Thị trường phân cấp: cây cho TreeSelect + chỉ cấp cha (parentId=null) cho chip lọc nhanh.
  const marketList = marketTypes.data ?? [];
  const marketTree = marketList
    .filter((m) => !m.parentId)
    .map((root) => ({
      title: root.name,
      value: root.id,
      children: marketList.filter((c) => c.parentId === root.id).map((c) => ({ title: c.name, value: c.id })),
    }));
  const topMarkets = marketList.filter((m) => !m.parentId).map((m) => ({ label: m.name, value: m.id }));
  const groupOpts = (tourGroups.data ?? []).map((g) => ({ label: g.name, value: g.id }));

  const list = useQuery({
    queryKey: ['orders', 'list', page.page, page.size, q, payStatus, adv],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/orders', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, paymentStatus: payStatus, ...adv }),
      });
      return pagedSchema(orderSchema).parse(data);
    },
  });

  const columns: ColumnsType<Order> = [
    {
      title: 'STT',
      key: '__stt',
      width: 60,
      fixed: 'left',
      align: 'center',
      render: (_: unknown, __: Order, index: number) => (page.page - 1) * page.size + index + 1,
    },
    { title: 'Mã đơn', dataIndex: 'code', key: 'code', fixed: 'left', width: 130 },
    { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName', width: 170, render: dash },
    { title: 'Tour', dataIndex: 'tourTitle', key: 'tourTitle', width: 200, ellipsis: true, render: dash },
    { title: 'Ngày đi', dataIndex: 'departureDate', key: 'departureDate', width: 110, render: dateVi },
    {
      title: 'Thu tiền',
      key: '__thu',
      width: 150,
      align: 'right',
      render: (_: unknown, o: Order) => (
        <div style={{ fontSize: 12, lineHeight: 1.4 }}>
          <div>Tổng thu: <strong>{money(o.totalRevenue)}</strong></div>
          <div style={{ color: '#3f8600' }}>Thực thu: {money(o.amountPaid ?? 0)}</div>
        </div>
      ),
    },
    {
      title: 'Chi tiền',
      key: '__chi',
      width: 150,
      align: 'right',
      render: (_: unknown, o: Order) => (
        <div style={{ fontSize: 12, lineHeight: 1.4 }}>
          <div>Tổng chi: <strong>{money(o.totalCost)}</strong></div>
          <div style={{ color: '#cf1322' }}>Thực chi: {money(o.actualCost ?? 0)}</div>
        </div>
      ),
    },
    {
      title: 'Lợi nhuận',
      key: '__loi',
      width: 130,
      align: 'right',
      render: (_: unknown, o: Order) => {
        const profit = o.totalRevenue - o.totalCost;
        return <span style={{ color: profit < 0 ? '#cf1322' : '#3f8600', fontWeight: 600 }}>{money(profit)}</span>;
      },
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 110,
      render: (s: number) => <Tag color={s === 2 ? 'green' : s === 3 ? 'red' : 'default'}>{statusText(ORDER_STATUS, s)}</Tag>,
    },
    {
      title: '',
      key: '__detail',
      width: 100,
      fixed: 'right',
      render: (_: unknown, item: Order) => (
        <Button size="small" onClick={() => navigate(`/orders/${item.id}`, { state: { order: item } })}>
          Chi tiết
        </Button>
      ),
    },
  ];

  const s = stats.data;
  const statCards = [
    { title: 'Tổng số đơn', value: s?.total ?? 0, money: false },
    { title: 'Doanh thu', value: s?.totalRevenue ?? 0, money: true },
    { title: 'Đã thu', value: s?.totalPaid ?? 0, money: true },
    { title: 'Còn nợ', value: s?.totalOutstanding ?? 0, money: true },
    { title: 'Đã chốt', value: s?.confirmed ?? 0, money: false },
    { title: 'Đã huỷ', value: s?.cancelled ?? 0, money: false },
  ];

  return (
    <>
      <PageHeader title={title} />

      {/* Thẻ thống kê (bám hệ cũ) */}
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {statCards.map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic
                title={c.title}
                value={c.value}
                loading={stats.isLoading}
                formatter={c.money ? (v) => money(Number(v)) : undefined}
              />
            </Card>
          </Col>
        ))}
      </Row>

      {/* Thanh lọc đầy đủ (bám staging /all-orders) */}
      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Mã, tên tour, SĐT, tên KH" value={search}
              onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="Tình trạng đơn" options={ORDER_STATUS_OPTIONS}
              value={draft.status} onChange={(v) => setD({ status: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="NV phụ trách"
              options={userOpts} value={draft.salesUserId} onChange={(v) => setD({ salesUserId: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Chi nhánh"
              options={branchOpts} value={draft.branchId} onChange={(v) => setD({ branchId: v ?? undefined })} />
          </Col>
          <Col xs={24} sm={12} lg={7}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Ngày tạo từ', 'đến']}
              value={draft.createdFrom && draft.createdTo ? [dayjs(draft.createdFrom), dayjs(draft.createdTo)] : null}
              onChange={(d) => setD({ createdFrom: d?.[0]?.startOf('day').toISOString(), createdTo: d?.[1]?.endOf('day').toISOString() })} />
          </Col>
          <Col xs={24} sm={12} lg={7}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Ngày khởi hành từ', 'đến']}
              value={draft.departureFrom && draft.departureTo ? [dayjs(draft.departureFrom), dayjs(draft.departureTo)] : null}
              onChange={(d) => setD({ departureFrom: d?.[0]?.startOf('day').toISOString(), departureTo: d?.[1]?.endOf('day').toISOString() })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Người tạo"
              options={userOpts} value={draft.createdByUserId} onChange={(v) => setD({ createdByUserId: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Phòng ban"
              options={deptOpts} value={draft.departmentId} onChange={(v) => setD({ departmentId: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Loại hình"
              options={tourTypeOpts} value={draft.tourType} onChange={(v) => setD({ tourType: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhà cung cấp"
              options={providerOpts} value={draft.providerId} onChange={(v) => setD({ providerId: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="Loại tour" options={BOOKING_TYPE_OPTIONS}
              value={draft.bookingType} onChange={(v) => setD({ bookingType: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <TreeSelect showSearch allowClear treeDefaultExpandAll style={{ width: '100%' }} placeholder="Thị trường"
              treeData={marketTree} treeNodeFilterProp="title"
              value={draft.marketTypeId} onChange={(v) => setD({ marketTypeId: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhóm"
              options={groupOpts} value={draft.tourGroupId} onChange={(v) => setD({ tourGroupId: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="TT hoa hồng" options={COMMISSION_OPTIONS}
              value={draft.commissionSettled === undefined ? undefined : String(draft.commissionSettled)}
              onChange={(v) => setD({ commissionSettled: v === undefined ? undefined : v === 'true' })} />
          </Col>
          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>
                Tìm kiếm
              </Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                (CTV / TT hóa đơn / Tình trạng vận hành: đang bổ sung ở phase sau)
              </Typography.Text>
            </Space>
          </Col>
        </Row>
      </Card>

      {/* Lọc nhanh theo thị trường (chip) — bám staging: chỉ cấp CHA, lọc gồm cả con cháu */}
      {topMarkets.length > 0 && (
        <div style={{ marginBottom: 12, overflowX: 'auto' }}>
          <Space size={4} wrap>
            <Typography.Text type="secondary" style={{ fontSize: 12 }}>Lọc nhanh thị trường:</Typography.Text>
            <Tag.CheckableTag checked={adv.marketTypeId === undefined} onChange={() => pickMarket(undefined)}>
              Tất cả
            </Tag.CheckableTag>
            {topMarkets.map((m) => (
              <Tag.CheckableTag key={m.value} checked={adv.marketTypeId === m.value} onChange={() => pickMarket(m.value)}>
                {m.label}
              </Tag.CheckableTag>
            ))}
          </Space>
        </div>
      )}

      {/* Tabs trạng thái thanh toán (bám staging: Chưa TT · Đã cọc · TT hết) */}
      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={payStatus === undefined ? 'all' : String(payStatus)}
          onChange={(val) => {
            setPayStatus(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[
            { label: `Tất cả (${s?.total ?? 0})`, value: 'all' },
            { label: `Chưa thanh toán (${s?.unpaid ?? 0})`, value: '0' },
            { label: `Đã cọc (${s?.deposit ?? 0})`, value: '1' },
            { label: `Thanh toán hết (${s?.paid ?? 0})`, value: '2' },
          ]}
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
          const rev = pageData.reduce((a, o) => a + (o.totalRevenue ?? 0), 0);
          const paid = pageData.reduce((a, o) => a + (o.amountPaid ?? 0), 0);
          const cost = pageData.reduce((a, o) => a + (o.totalCost ?? 0), 0);
          const actualCost = pageData.reduce((a, o) => a + (o.actualCost ?? 0), 0);
          const owe = pageData.reduce((a, o) => a + (o.outstanding ?? 0), 0);
          const profit = rev - cost;
          return (
            <Table.Summary fixed>
              <Table.Summary.Row>
                <Table.Summary.Cell index={0} colSpan={columns.length}>
                  <Space size="large" wrap>
                    <strong>Tổng cộng (trang này)</strong>
                    <span>Tổng thu: <strong>{money(rev)}</strong></span>
                    <span>Thực thu: <strong style={{ color: '#3f8600' }}>{money(paid)}</strong></span>
                    <span>Tổng chi: <strong>{money(cost)}</strong></span>
                    <span>Thực chi: <strong style={{ color: '#cf1322' }}>{money(actualCost)}</strong></span>
                    <span>Lợi nhuận: <strong style={{ color: profit < 0 ? '#cf1322' : '#3f8600' }}>{money(profit)}</strong></span>
                    <span>Phải thu: <strong>{money(owe)}</strong></span>
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
