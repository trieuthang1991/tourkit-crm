import { App, Button, Card, Col, Input, Modal, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import {
  useAssignFlightTicket,
  useCreateFlightTicket,
  useDeleteFlightTicket,
  useFlightTickets,
  useFlightTicketStats,
  useMarketOptions,
  useProviderOptions,
} from './flightsApi';
import type { FlightFilter } from './flightsApi';
import { FLIGHT_TOUR_TYPE, createFlightTicketFormSchema } from './types';
import type { CreateFlightTicketForm, FlightSegment, FlightTicket } from './types';

const TOUR_TYPE_OPTS = Object.entries(FLIGHT_TOUR_TYPE).map(([value, label]) => ({ value, label }));

function Itinerary({ segments }: { segments: FlightSegment[] }) {
  if (!segments.length) return <>—</>;
  return (
    <div style={{ whiteSpace: 'nowrap', lineHeight: 1.5 }}>
      {segments.map((s, i) => (
        <div key={i} style={{ fontSize: 12 }}>
          <span style={{ color: '#888' }}>{s.date}</span> <b>{s.flightNo}</b> {s.from}→{s.to} {s.depTime}
        </div>
      ))}
    </div>
  );
}

export function FlightTicketsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('ticketfund.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [marketRef, setMarketRef] = useState<string | undefined>();
  const [providerRef, setProviderRef] = useState<string | undefined>();
  const [tourType, setTourType] = useState<string | undefined>();
  const [assigned, setAssigned] = useState<boolean | undefined>();
  const [applied, setApplied] = useState<FlightFilter>({});
  const applyFilters = () => {
    setApplied({ q: search || undefined, marketRef, providerRef, tourType });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setMarketRef(undefined);
    setProviderRef(undefined);
    setTourType(undefined);
    setAssigned(undefined);
    setApplied({});
    setPage(1);
  };

  const filter: FlightFilter = { ...applied, assigned };
  const list = useFlightTickets(page, size, filter);
  const stats = useFlightTicketStats(filter);
  const markets = useMarketOptions();
  const providers = useProviderOptions();
  const marketOpts = (markets.data ?? []).map((m) => ({ label: m.name, value: m.id }));
  const providerOpts = (providers.data ?? []).map((p) => ({ label: p.name, value: p.id }));

  const [creating, setCreating] = useState(false);
  const [assignRow, setAssignRow] = useState<FlightTicket | null>(null);
  const [assignVal, setAssignVal] = useState('');

  const create = useCreateFlightTicket();
  const assign = useAssignFlightTicket();
  const remove = useDeleteFlightTicket();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onCreate(values: CreateFlightTicketForm) {
    await run(async () => {
      await create.mutateAsync(values);
      setCreating(false);
    }, 'Đã thêm vé đoàn');
  }

  const columns: ColumnsType<FlightTicket> = [
    { title: 'PNR', dataIndex: 'pnr', key: 'pnr', width: 110, fixed: 'left' },
    {
      title: 'Gán Tour',
      key: 'order',
      width: 170,
      render: (_: unknown, r: FlightTicket) =>
        r.orderRef ? (
          <div>
            <div><b>{r.orderCode ?? '—'}</b></div>
            <div style={{ fontSize: 12, color: '#888' }}>{r.orderName ?? ''}</div>
          </div>
        ) : canManage ? (
          <Button size="small" type="primary" onClick={() => { setAssignRow(r); setAssignVal(''); }}>+ Gán tour</Button>
        ) : <Tag>Chưa gán</Tag>,
    },
    { title: 'Loại hình', dataIndex: 'tourType', key: 'tourType', width: 100, render: (v: string | null) => (v ? (FLIGHT_TOUR_TYPE[v] ?? v) : '—') },
    { title: 'Thị trường', dataIndex: 'marketName', key: 'marketName', width: 110, render: (v: string | null) => v ?? '—' },
    { title: 'NCC', dataIndex: 'providerName', key: 'providerName', width: 140, render: (v: string | null) => v ?? '—' },
    { title: 'Số ngày', dataIndex: 'days', key: 'days', width: 80, align: 'right' },
    { title: 'Ngày đi', dataIndex: 'departureDate', key: 'departureDate', width: 110, render: (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—') },
    { title: 'Hành trình', key: 'itin', width: 220, render: (_: unknown, r: FlightTicket) => <Itinerary segments={r.segments} /> },
    {
      title: 'Vé (SL/Dùng/Còn)',
      key: 'qty',
      width: 150,
      render: (_: unknown, r: FlightTicket) => (
        <Space size={4}>
          <Tag>{r.quantity}</Tag>
          <Tag color="orange">{r.usedQuantity}</Tag>
          <Tag color="green">{r.remainingQuantity}</Tag>
        </Space>
      ),
    },
    { title: 'Tổng chi', dataIndex: 'totalCost', key: 'totalCost', width: 130, align: 'right', render: (v: number) => money(v) },
    { title: 'Đã TT', dataIndex: 'paidAmount', key: 'paidAmount', width: 130, align: 'right', render: (v: number) => money(v) },
    { title: 'Còn lại', dataIndex: 'remainingCost', key: 'remainingCost', width: 130, align: 'right', render: (v: number) => <span style={{ color: v > 0 ? '#cf1322' : undefined }}>{money(v)}</span> },
    { title: 'Bảo lưu', dataIndex: 'reservedAmount', key: 'reservedAmount', width: 120, align: 'right', render: (v: number) => money(v) },
    {
      title: '',
      key: '__actions',
      width: 90,
      fixed: 'right',
      render: (_: unknown, r: FlightTicket) =>
        canManage ? (
          <Popconfirm title="Xoá vé đoàn này?" onConfirm={() => run(() => remove.mutateAsync(r.id), 'Đã xoá')}>
            <Button size="small" danger>Xoá</Button>
          </Popconfirm>
        ) : null,
    },
  ];

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Quản lý Vé Đoàn
        </Typography.Title>
        {canManage ? <Button type="primary" onClick={() => setCreating(true)}>Tạo mới</Button> : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Số lượng vé', value: stats.data?.totalQuantity ?? 0, money: false },
          { title: 'Đã sử dụng', value: stats.data?.totalUsed ?? 0, money: false },
          { title: 'Vé còn lại', value: stats.data?.totalRemaining ?? 0, money: false },
          { title: 'Tổng chi', value: stats.data?.totalCost ?? 0, money: true },
          { title: 'Đã thanh toán', value: stats.data?.totalPaid ?? 0, money: true },
          { title: 'Còn lại', value: stats.data?.totalRemainingCost ?? 0, money: true },
          { title: 'Tiền bảo lưu', value: stats.data?.totalReserved ?? 0, money: true },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={3} flex="1">
            <Card styles={{ body: { padding: 12 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} valueStyle={{ fontSize: 18 }} formatter={c.money ? (v) => money(Number(v)) : undefined} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Mã PNR" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Thị trường" options={marketOpts} value={marketRef} onChange={(v) => setMarketRef(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="NCC" options={providerOpts} value={providerRef} onChange={(v) => setProviderRef(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="Loại hình" options={TOUR_TYPE_OPTS} value={tourType} onChange={(v) => setTourType(v ?? undefined)} />
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
          value={assigned === undefined ? 'all' : assigned ? 'assigned' : 'unassigned'}
          onChange={(val) => {
            setAssigned(val === 'all' ? undefined : val === 'assigned');
            setPage(1);
          }}
          options={[
            { label: `Tất cả code (${stats.data?.total ?? 0})`, value: 'all' },
            { label: `Đã gán tour (${stats.data?.assigned ?? 0})`, value: 'assigned' },
            { label: `Chưa gán tour (${stats.data?.unassigned ?? 0})`, value: 'unassigned' },
          ]}
        />
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 'max-content' }}
        pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
      />

      {creating ? (
        <CrudFormModal
          open
          title="Tạo vé đoàn"
          schema={createFlightTicketFormSchema}
          defaultValues={{ pnr: '', marketRef: null, providerRef: null, tourType: 'outbound', days: 0, departureDate: null, quantity: 0, totalCost: 0, reservedAmount: 0, note: null }}
          submitting={create.isPending}
          onCancel={() => setCreating(false)}
          onSubmit={onCreate}
        >
          <TextField name="pnr" label="Mã PNR" required />
          <SelectField name="tourType" label="Loại hình" options={TOUR_TYPE_OPTS} allowClear />
          <SelectField name="marketRef" label="Thị trường" options={marketOpts} allowClear />
          <SelectField name="providerRef" label="Nhà cung cấp" options={providerOpts} allowClear />
          <NumberField name="days" label="Số ngày" required />
          <DatePickerField name="departureDate" label="Ngày đi" />
          <NumberField name="quantity" label="Số lượng vé" required />
          <NumberField name="totalCost" label="Tổng chi" required />
          <NumberField name="reservedAmount" label="Tiền bảo lưu" required />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      ) : null}

      <Modal
        open={!!assignRow}
        title={`Gán tour cho ${assignRow?.pnr ?? ''}`}
        okText="Gán"
        confirmLoading={assign.isPending}
        onCancel={() => setAssignRow(null)}
        onOk={() =>
          run(async () => {
            await assign.mutateAsync({ id: assignRow!.id, orderRef: assignVal.trim() || null });
            setAssignRow(null);
          }, 'Đã gán tour')
        }
      >
        <Typography.Paragraph type="secondary">Nhập ID/mã đơn (order) cần gán vé đoàn này vào.</Typography.Paragraph>
        <Input placeholder="Order ID / mã đơn" value={assignVal} onChange={(e) => setAssignVal(e.target.value)} />
      </Modal>
    </>
  );
}
