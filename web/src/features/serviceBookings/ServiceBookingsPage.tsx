import { App, Button, Card, Col, DatePicker, Input, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { dateText, money, statusText } from '../../shared/format';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import { roomClassSchema } from '../roomClasses/types';
import { providersCrud } from '../providers/providersCrud';
import { ordersCrud } from '../booking/bookingApi';
import { serviceBookingsCrud } from './serviceBookingsCrud';
import { SERVICE_BOOKING_TYPE, serviceBookingFormSchema, serviceBookingSchema } from './types';
import type { ServiceBooking, ServiceBookingForm } from './types';

const TYPE_OPTIONS = Object.entries(SERVICE_BOOKING_TYPE).map(([value, label]) => ({ value: Number(value), label }));

const statsSchema = z.object({
  total: z.number(),
  hotel: z.number(),
  flight: z.number(),
  visa: z.number(),
  ticket: z.number(),
  transfer: z.number(),
  other: z.number(),
  totalAmount: z.number(),
});

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

function RoomClassField() {
  const list = useQuery({
    queryKey: ['room-classes'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/room-classes');
      return z.array(roomClassSchema).parse(data);
    },
  });
  const options = (list.data ?? []).map((r) => ({ label: r.name, value: r.id }));
  return <SelectField name="roomClassId" label="Hạng phòng (khi đặt KS)" options={options} allowClear />;
}

function OrderField() {
  const list = ordersCrud.useList({ page: 1, size: 200 });
  const options = (list.data?.items ?? []).map((o) => ({ label: o.code, value: o.id }));
  return <SelectField name="orderId" label="Đơn hàng" options={options} allowClear showSearch />;
}

function ProviderField() {
  const list = providersCrud.useList({ page: 1, size: 200 });
  const options = (list.data?.items ?? []).map((p) => ({ label: `${p.name} (${p.code})`, value: p.id }));
  return <SelectField name="providerId" label="Nhà cung cấp" options={options} allowClear showSearch />;
}

export function ServiceBookingsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('servicebooking.manage');
  const create = serviceBookingsCrud.useCreate();
  const update = serviceBookingsCrud.useUpdate();
  const remove = serviceBookingsCrud.useRemove();

  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [type, setType] = useState<number | undefined>();
  const [providerId, setProviderId] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [range, setRange] = useState<{ from?: string; to?: string }>({});
  const [rangeApplied, setRangeApplied] = useState<{ from?: string; to?: string }>({});
  const [editing, setEditing] = useState<ServiceBooking | 'new' | null>(null);

  const applyFilters = () => {
    setQ(search);
    setRangeApplied(range);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setType(undefined);
    setProviderId(undefined);
    setStatus(undefined);
    setRange({});
    setRangeApplied({});
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['service-bookings', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/service-bookings/stats');
      return statsSchema.parse(data);
    },
  });
  const providers = providersCrud.useList({ page: 1, size: 200 });
  const providerOpts = (providers.data?.items ?? []).map((p) => ({ label: `${p.name} (${p.code})`, value: p.id }));
  const providerName = new Map((providers.data?.items ?? []).map((p) => [p.id, p.name]));

  const list = useQuery({
    queryKey: ['service-bookings', 'list', page.page, page.size, q, type, providerId, status, rangeApplied],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/service-bookings', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, type, providerId, status, dateFrom: rangeApplied.from, dateTo: rangeApplied.to }),
      });
      return pagedSchema(serviceBookingSchema).parse(data);
    },
  });

  async function submit(values: ServiceBookingForm) {
    try {
      if (editing && editing !== 'new') {
        await update.mutateAsync({ id: editing.id, body: values });
        message.success('Đã cập nhật');
      } else {
        await create.mutateAsync(values);
        message.success('Đã thêm');
      }
      setEditing(null);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }
  async function onDelete(id: string) {
    try {
      await remove.mutateAsync(id);
      message.success('Đã xoá');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<ServiceBooking> = [
    { title: 'Mã', dataIndex: 'code', key: 'code', fixed: 'left', width: 120 },
    { title: 'Loại', dataIndex: 'type', key: 'type', width: 110, render: (v: number) => <Tag color="blue">{statusText(SERVICE_BOOKING_TYPE, v)}</Tag> },
    { title: 'Mô tả', dataIndex: 'description', key: 'description', width: 220, ellipsis: true },
    { title: 'Nhà cung cấp', dataIndex: 'providerId', key: 'providerId', width: 170, render: (v: string | null) => (v ? providerName.get(v) ?? '—' : '—') },
    { title: 'Từ ngày', dataIndex: 'startDate', key: 'startDate', width: 110, render: (v: string | null) => dateText(v) },
    { title: 'Đến ngày', dataIndex: 'endDate', key: 'endDate', width: 110, render: (v: string | null) => dateText(v) },
    { title: 'SL', dataIndex: 'quantity', key: 'quantity', width: 70, align: 'center' },
    { title: 'Đơn giá', dataIndex: 'unitPrice', key: 'unitPrice', width: 120, align: 'right', render: (v: number) => money(v) },
    { title: 'Thành tiền', dataIndex: 'totalAmount', key: 'totalAmount', width: 140, align: 'right', render: (v: number) => money(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 100, align: 'center', render: (v: number) => <Tag>{v}</Tag> },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 150,
            fixed: 'right' as const,
            render: (_: unknown, r: ServiceBooking) => (
              <Space>
                <Button size="small" onClick={() => setEditing(r)}>Sửa</Button>
                <Popconfirm title="Xoá dịch vụ này?" onConfirm={() => onDelete(r.id)}>
                  <Button size="small" danger>Xoá</Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<ServiceBooking>[number],
        ]
      : []),
  ];

  const s = stats.data;
  const statCards = [
    { title: 'Tổng dịch vụ', value: s?.total ?? 0, money: false },
    { title: 'Tổng tiền', value: s?.totalAmount ?? 0, money: true },
    { title: 'Khách sạn', value: s?.hotel ?? 0, money: false },
    { title: 'Vé máy bay', value: s?.flight ?? 0, money: false },
    { title: 'Visa', value: s?.visa ?? 0, money: false },
    { title: 'Vé / Khác', value: (s?.ticket ?? 0) + (s?.other ?? 0), money: false },
  ];

  const defaultValues: ServiceBookingForm =
    editing && editing !== 'new'
      ? {
          code: editing.code, type: editing.type, orderId: editing.orderId ?? null, providerId: editing.providerId ?? null,
          description: editing.description, startDate: editing.startDate ?? null, endDate: editing.endDate ?? null,
          quantity: editing.quantity, unitPrice: editing.unitPrice, status: editing.status, note: editing.note ?? null,
          roomClassId: editing.roomClassId ?? null,
        }
      : { code: '', type: 1, orderId: null, providerId: null, description: '', startDate: null, endDate: null, quantity: 1, unitPrice: 0, status: 0, note: null, roomClassId: null };

  return (
    <>
      <PageHeader
        title="Đặt dịch vụ lẻ"
        extra={canManage ? <Button type="primary" onClick={() => setEditing('new')}>Thêm dịch vụ</Button> : undefined}
      />

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
            <Input.Search allowClear placeholder="Mã / mô tả" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={12} sm={8} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhà cung cấp"
              options={providerOpts} value={providerId} onChange={(v) => setProviderId(v ?? undefined)} />
          </Col>
          <Col xs={12} sm={8} lg={3}>
            <Select allowClear style={{ width: '100%' }} placeholder="Trạng thái"
              options={[{ value: 0, label: '0' }, { value: 1, label: '1' }, { value: 2, label: '2' }]}
              value={status} onChange={(v) => setStatus(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Bắt đầu từ', 'đến']}
              value={range.from && range.to ? [dayjs(range.from), dayjs(range.to)] : null}
              onChange={(d) => setRange({ from: d?.[0]?.startOf('day').toISOString(), to: d?.[1]?.endOf('day').toISOString() })} />
          </Col>
          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>Tìm kiếm</Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={type === undefined ? 'all' : String(type)}
          onChange={(val) => {
            setType(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: `Tất cả (${s?.total ?? 0})`, value: 'all' }, ...TYPE_OPTIONS.map((t) => ({ label: t.label, value: String(t.value) }))]}
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

      {editing && (
        <CrudFormModal
          open
          title={editing !== 'new' ? 'Sửa đặt dịch vụ' : 'Thêm đặt dịch vụ'}
          schema={serviceBookingFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          <TextField name="code" label="Mã" />
          <SelectField name="type" label="Loại" options={TYPE_OPTIONS} required />
          <TextField name="description" label="Mô tả" required />
          <OrderField />
          <ProviderField />
          <DatePickerField name="startDate" label="Ngày bắt đầu" />
          <DatePickerField name="endDate" label="Ngày kết thúc" />
          <NumberField name="quantity" label="Số lượng" required />
          <NumberField name="unitPrice" label="Đơn giá" required />
          <NumberField name="status" label="Trạng thái" required />
          <RoomClassField />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      )}
    </>
  );
}
