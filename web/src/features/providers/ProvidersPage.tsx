import { App, Button, Card, Col, DatePicker, Input, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { paymentTermSchema } from '../paymentTerms/types';
import { providersCrud } from './providersCrud';
import { PROVIDER_TYPE, providerCreateSchema, providerSchema, providerUpdateSchema } from './types';
import type { Provider, ProviderForm } from './types';

const PROVIDER_TYPE_OPTIONS = Object.entries(PROVIDER_TYPE).map(([value, label]) => ({ value: Number(value), label }));
const STATUS_OPTIONS = [
  { value: 1, label: 'Hoạt động' },
  { value: 0, label: 'Ngừng' },
];

const dash = (v: string | null | undefined) => (v ? v : '—');
const statsSchema = z.object({ total: z.number(), active: z.number(), inactive: z.number() });

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

function PaymentTermField() {
  const list = useQuery({
    queryKey: ['payment-terms'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payment-terms');
      return z.array(paymentTermSchema).parse(data);
    },
  });
  const options = (list.data ?? []).map((t) => ({ label: t.name, value: t.id }));
  return <SelectField name="paymentTermId" label="Điều khoản thanh toán" options={options} allowClear />;
}

export function ProvidersPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [typeFilter, setTypeFilter] = useState<number | undefined>();
  const [statusFilter, setStatusFilter] = useState<number | undefined>();
  const [province, setProvince] = useState('');
  const [provinceApplied, setProvinceApplied] = useState('');
  const [branchId, setBranchId] = useState<string | undefined>();
  const [marketTypeId, setMarketTypeId] = useState<string | undefined>();
  const [dateRange, setDateRange] = useState<{ from?: string; to?: string }>({});
  const [editing, setEditing] = useState<{ mode: 'create' | 'edit'; item: Provider | null } | null>(null);

  const canCreate = has('provider.create');
  const canUpdate = has('provider.update');
  const canRemove = has('provider.delete');

  const stats = useQuery({
    queryKey: ['providers', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/providers/stats');
      return statsSchema.parse(data);
    },
  });
  const branches = useQuery({
    queryKey: ['branches'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/branches');
      return z.array(z.object({ id: z.string().uuid(), name: z.string() })).parse(data);
    },
  });
  const marketTypes = useQuery({
    queryKey: ['market-types'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/market-types');
      return z.array(z.object({ id: z.string().uuid(), name: z.string() })).parse(data);
    },
  });
  const branchOpts = (branches.data ?? []).map((b) => ({ label: b.name, value: b.id }));
  const marketOpts = (marketTypes.data ?? []).map((m) => ({ label: m.name, value: m.id }));

  const list = useQuery({
    queryKey: ['providers', 'list', page.page, page.size, q, typeFilter, statusFilter, provinceApplied, branchId, marketTypeId, dateRange],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/providers', {
        params: clean({
          page: page.page, size: page.size, q: q || undefined, type: typeFilter, status: statusFilter,
          province: provinceApplied || undefined, branchId, marketTypeId, createdFrom: dateRange.from, createdTo: dateRange.to,
        }),
      });
      return pagedSchema(providerSchema).parse(data);
    },
  });

  const create = providersCrud.useCreate();
  const update = providersCrud.useUpdate();
  const remove = providersCrud.useRemove();

  async function submit(values: ProviderForm) {
    try {
      if (editing?.mode === 'edit' && editing.item) {
        await update.mutateAsync({ id: editing.item.id, body: values });
      } else {
        await create.mutateAsync(values);
      }
      message.success('Đã lưu');
      setEditing(null);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<Provider> = [
    {
      title: 'STT',
      key: '__stt',
      width: 60,
      fixed: 'left',
      align: 'center',
      render: (_: unknown, __: Provider, index: number) => (page.page - 1) * page.size + index + 1,
    },
    { title: 'Mã', dataIndex: 'code', key: 'code', fixed: 'left', width: 110 },
    { title: 'Tên NCC', dataIndex: 'name', key: 'name', fixed: 'left', width: 200 },
    { title: 'Loại', dataIndex: 'type', key: 'type', width: 130, render: (type: number) => statusText(PROVIDER_TYPE, type) },
    { title: 'Người liên hệ', dataIndex: 'contactPerson', key: 'contactPerson', width: 150, render: dash },
    { title: 'Điện thoại', dataIndex: 'phone', key: 'phone', width: 120, render: dash },
    { title: 'Email', dataIndex: 'email', key: 'email', width: 170, render: dash },
    { title: 'Địa chỉ', dataIndex: 'address', key: 'address', width: 200, ellipsis: true, render: dash },
    { title: 'MST', dataIndex: 'taxCode', key: 'taxCode', width: 120, render: dash },
    { title: 'Tổng mua', dataIndex: 'totalCost', key: 'totalCost', width: 130, align: 'right', render: (v: number) => money(v ?? 0) },
    { title: 'Đã trả', dataIndex: 'paid', key: 'paid', width: 130, align: 'right', render: (v: number) => money(v ?? 0) },
    {
      title: 'Còn nợ',
      dataIndex: 'outstanding',
      key: 'outstanding',
      width: 130,
      align: 'right',
      render: (v: number) => <span style={{ color: (v ?? 0) > 0 ? '#cf1322' : undefined }}>{money(v ?? 0)}</span>,
    },
    { title: 'Đánh giá', dataIndex: 'rate', key: 'rate', width: 90, align: 'center' },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (status: number) => (status === 1 ? <Tag color="green">Hoạt động</Tag> : <Tag>Ngừng</Tag>),
    },
    ...(canUpdate || canRemove
      ? [
          {
            title: '',
            key: '__actions',
            width: 150,
            fixed: 'right' as const,
            render: (_: unknown, p: Provider) => (
              <Space>
                {canUpdate ? (
                  <Button size="small" onClick={() => setEditing({ mode: 'edit', item: p })}>
                    Sửa
                  </Button>
                ) : null}
                {canRemove ? (
                  <Popconfirm
                    title="Xoá nhà cung cấp này?"
                    onConfirm={async () => {
                      try {
                        await remove.mutateAsync(p.id);
                        message.success('Đã xoá');
                      } catch (e) {
                        message.error(errorMessage(e));
                      }
                    }}
                  >
                    <Button size="small" danger>
                      Xoá
                    </Button>
                  </Popconfirm>
                ) : null}
              </Space>
            ),
          } as ColumnsType<Provider>[number],
        ]
      : []),
  ];

  const item = editing?.item ?? null;
  const defaultValues: ProviderForm = {
    code: item?.code ?? '',
    name: item?.name ?? '',
    type: item?.type ?? 1,
    phone: item?.phone ?? null,
    email: item?.email ?? null,
    address: item?.address ?? null,
    taxCode: item?.taxCode ?? null,
    contactPerson: item?.contactPerson ?? null,
    bankAccount: item?.bankAccount ?? null,
    bankName: item?.bankName ?? null,
    paymentTermId: item?.paymentTermId ?? null,
    province: item?.province ?? null,
    branchId: item?.branchId ?? null,
    marketTypeId: item?.marketTypeId ?? null,
    rate: item?.rate ?? 0,
    status: item?.status ?? 1,
  };

  const s = stats.data;
  const statCards = [
    { title: 'Tổng số NCC', value: s?.total ?? 0 },
    { title: 'Đang hoạt động', value: s?.active ?? 0 },
    { title: 'Ngừng hoạt động', value: s?.inactive ?? 0 },
  ];

  return (
    <>
      <PageHeader
        title="Nhà cung cấp"
        extra={
          canCreate ? (
            <Button type="primary" onClick={() => setEditing({ mode: 'create', item: null })}>
              Thêm mới
            </Button>
          ) : null
        }
      />

      {/* Thẻ thống kê */}
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {statCards.map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      {/* Thanh tìm kiếm + lọc */}
      <Space wrap style={{ marginBottom: 12 }}>
        <Input.Search
          allowClear
          placeholder="Tìm theo mã / tên / SĐT / email / người liên hệ"
          style={{ width: 360 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onSearch={(v) => {
            setQ(v);
            setPage({ ...page, page: 1 });
          }}
        />
        <Input
          allowClear
          placeholder="Tỉnh thành"
          style={{ width: 160 }}
          value={province}
          onChange={(e) => setProvince(e.target.value)}
          onPressEnter={() => { setProvinceApplied(province); setPage({ ...page, page: 1 }); }}
          onBlur={() => { setProvinceApplied(province); setPage({ ...page, page: 1 }); }}
        />
        <Select
          showSearch
          allowClear
          optionFilterProp="label"
          placeholder="Chi nhánh"
          style={{ width: 180 }}
          options={branchOpts}
          value={branchId}
          onChange={(v) => { setBranchId(v ?? undefined); setPage({ ...page, page: 1 }); }}
        />
        <Select
          showSearch
          allowClear
          optionFilterProp="label"
          placeholder="Thị trường"
          style={{ width: 180 }}
          options={marketOpts}
          value={marketTypeId}
          onChange={(v) => { setMarketTypeId(v ?? undefined); setPage({ ...page, page: 1 }); }}
        />
        <DatePicker.RangePicker
          placeholder={['Ngày đặt từ', 'đến']}
          value={dateRange.from && dateRange.to ? [dayjs(dateRange.from), dayjs(dateRange.to)] : null}
          onChange={(d) => { setDateRange({ from: d?.[0]?.startOf('day').toISOString(), to: d?.[1]?.endOf('day').toISOString() }); setPage({ ...page, page: 1 }); }}
        />
        <Select
          allowClear
          placeholder="Trạng thái"
          style={{ width: 160 }}
          options={STATUS_OPTIONS}
          value={statusFilter}
          onChange={(v) => {
            setStatusFilter(v);
            setPage({ ...page, page: 1 });
          }}
        />
      </Space>

      {/* Tabs loại NCC */}
      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={typeFilter === undefined ? 'all' : String(typeFilter)}
          onChange={(val) => {
            setTypeFilter(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: 'Tất cả', value: 'all' }, ...PROVIDER_TYPE_OPTIONS.map((o) => ({ label: o.label, value: String(o.value) }))]}
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
      />

      {editing ? (
        <CrudFormModal
          open
          title={editing.mode === 'edit' ? 'Sửa nhà cung cấp' : 'Thêm nhà cung cấp'}
          schema={editing.mode === 'edit' ? providerUpdateSchema : providerCreateSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          {editing.mode === 'create' ? <TextField name="code" label="Mã" required /> : null}
          <TextField name="name" label="Tên" required />
          <SelectField name="type" label="Loại" options={PROVIDER_TYPE_OPTIONS} required />
          <TextField name="phone" label="Điện thoại" />
          <TextField name="email" label="Email" />
          <TextField name="address" label="Địa chỉ" />
          <TextField name="province" label="Tỉnh thành" />
          <SelectField name="branchId" label="Chi nhánh" options={branchOpts} allowClear />
          <SelectField name="marketTypeId" label="Thị trường" options={marketOpts} allowClear />
          <TextField name="taxCode" label="Mã số thuế" />
          <TextField name="contactPerson" label="Người liên hệ" />
          <TextField name="bankAccount" label="Số tài khoản" />
          <TextField name="bankName" label="Ngân hàng" />
          <PaymentTermField />
          <NumberField name="rate" label="Tỉ lệ" required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      ) : null}
    </>
  );
}
