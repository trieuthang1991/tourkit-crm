import { App, Button, Card, Col, Input, Modal, Popconfirm, Row, Segmented, Select, Space, Table, Tag, Typography } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { StatGrid } from '../../ui/kit';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { CellEntity, CellMoney, CellStack } from '../../shared/ui/TableCells';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { DataCard } from '../../shared/ui';
import { OrderSelect } from '../booking/OrderSelect';
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
          <span style={{ color: 'var(--tk-muted)' }}>{s.date}</span> <b>{s.flightNo}</b> {s.from}→{s.to} {s.depTime}
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
    {
      title: 'PNR / Loại hình',
      key: 'pnr',
      width: 130,
      render: (_: unknown, r: FlightTicket) => (
        <CellStack main={r.pnr} mono sub={r.tourType ? (FLIGHT_TOUR_TYPE[r.tourType] ?? r.tourType) : undefined} />
      ),
    },
    {
      title: 'Gán Tour',
      key: 'order',
      width: 170,
      render: (_: unknown, r: FlightTicket) =>
        r.orderRef ? (
          <CellStack main={r.orderCode ?? '—'} sub={r.orderName ?? undefined} />
        ) : canManage ? (
          <Button size="small" type="primary" onClick={() => { setAssignRow(r); setAssignVal(''); }}>+ Gán tour</Button>
        ) : <Tag>Chưa gán</Tag>,
    },
    {
      title: 'NCC / Thị trường',
      key: 'provider',
      width: 160,
      render: (_: unknown, r: FlightTicket) => <CellEntity name={r.providerName ?? '—'} meta={r.marketName ?? undefined} />,
    },
    {
      title: 'Lịch trình',
      key: 'schedule',
      width: 110,
      render: (_: unknown, r: FlightTicket) => (
        <CellStack
          main={r.departureDate ? new Date(r.departureDate).toLocaleDateString('vi-VN') : '—'}
          mono
          sub={r.days ? `${r.days} ngày` : undefined}
        />
      ),
    },
    { title: 'Hành trình', key: 'itin', width: 210, render: (_: unknown, r: FlightTicket) => <Itinerary segments={r.segments} /> },
    {
      title: 'Vé (SL / Dùng / Còn)',
      key: 'qty',
      width: 140,
      render: (_: unknown, r: FlightTicket) => (
        <Space size={4}>
          <Tag>{r.quantity}</Tag>
          <Tag color="orange">{r.usedQuantity}</Tag>
          <Tag color="green">{r.remainingQuantity}</Tag>
        </Space>
      ),
    },
    {
      title: 'Tổng chi / Đã TT',
      key: 'cost',
      width: 150,
      align: 'right',
      render: (_: unknown, r: FlightTicket) => <CellMoney value={r.totalCost} sub={r.paidAmount} subLabel="Đã TT" />,
    },
    {
      title: 'Còn lại / Bảo lưu',
      key: 'remainingCost',
      width: 150,
      align: 'right',
      render: (_: unknown, r: FlightTicket) => (
        <CellMoney value={r.remainingCost} tone={r.remainingCost > 0 ? 'danger' : 'heading'} sub={r.reservedAmount} subLabel="Bảo lưu" />
      ),
    },
    {
      title: '',
      key: '__actions',
      width: 80,
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
        {canManage ? <Button type="primary" onClick={() => setCreating(true)}>Thêm vé đoàn</Button> : null}
      </div>

      <StatGrid
        style={{ marginBottom: 16 }}
        items={[
          { label: 'Số lượng vé', value: stats.data?.totalQuantity ?? 0 },
          { label: 'Đã sử dụng', value: stats.data?.totalUsed ?? 0 },
          { label: 'Vé còn lại', value: stats.data?.totalRemaining ?? 0 },
          { label: 'Tổng chi', value: money(stats.data?.totalCost ?? 0) },
          { label: 'Đã thanh toán', value: money(stats.data?.totalPaid ?? 0) },
          { label: 'Còn lại', value: money(stats.data?.totalRemainingCost ?? 0) },
          { label: 'Tiền bảo lưu', value: money(stats.data?.totalReserved ?? 0) },
        ]}
      />

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

      <DataCard title="Danh sách vé máy bay đoàn">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
        />
      </DataCard>

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
        <Typography.Paragraph type="secondary">Chọn đơn (order) cần gán vé đoàn này vào — gõ mã đơn hoặc tên khách để tìm.</Typography.Paragraph>
        <OrderSelect value={assignVal || null} onChange={(v) => setAssignVal(v ?? '')} />
      </Modal>
    </>
  );
}
