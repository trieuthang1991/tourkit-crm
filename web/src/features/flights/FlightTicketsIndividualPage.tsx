import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Table, Tag, Typography } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { useProviderOptions } from './flightsApi';
import {
  useCreateFlightIndividual,
  useDeleteFlightIndividual,
  useFlightIndividuals,
  useFlightIndividualStats,
  useUpdateFlightIndividual,
} from './flightsIndividualApi';
import type { FlightIndividualFilter } from './flightsIndividualApi';
import {
  FI_STATUS_COLOR,
  FI_STATUS_LABEL,
  FI_STATUS_OPTIONS,
  FI_TABS,
  TRIP_TYPE_LABEL,
  TRIP_TYPE_OPTIONS,
  flightIndividualFormSchema,
} from './individualTypes';
import type { FlightIndividual, FlightIndividualForm } from './individualTypes';
import { CellEntity, CellMoney, CellStack } from '../../shared/ui/TableCells';
import { DataCard } from '../../shared/ui';
import { StatGrid } from '../../ui/kit';

const vnDate = (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');

const EMPTY_FORM: FlightIndividualForm = {
  code: '',
  ticketCode: null,
  pnr: '',
  customerName: '',
  orderRef: null,
  providerRef: null,
  tripType: 0,
  route: null,
  departDate: null,
  returnDate: null,
  sellAmount: 0,
  receivedAmount: 0,
  totalCost: 0,
  paidAmount: 0,
  paymentDueDate: null,
  status: 0,
  assigneeRef: null,
  note: null,
};

export function FlightTicketsIndividualPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('ticketfund.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [providerRef, setProviderRef] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [tab, setTab] = useState<string | undefined>();
  const [applied, setApplied] = useState<FlightIndividualFilter>({});
  const applyFilters = () => {
    setApplied({ q: search || undefined, providerRef, status });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setProviderRef(undefined);
    setStatus(undefined);
    setTab(undefined);
    setApplied({});
    setPage(1);
  };

  const filter: FlightIndividualFilter = { ...applied, tab };
  const list = useFlightIndividuals(page, size, filter);
  const stats = useFlightIndividualStats(filter);
  const providers = useProviderOptions();
  const providerOpts = (providers.data ?? []).map((p) => ({ label: p.name, value: p.id }));

  const [editingId, setEditingId] = useState<string | 'new' | null>(null);
  const editing = list.data?.items.find((t) => t.id === editingId) ?? null;
  const isEdit = editingId !== null && editingId !== 'new';

  const create = useCreateFlightIndividual();
  const update = useUpdateFlightIndividual();
  const remove = useDeleteFlightIndividual();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onSubmit(values: FlightIndividualForm) {
    await run(async () => {
      if (isEdit) {
        await update.mutateAsync({ id: editingId as string, body: values });
      } else {
        await create.mutateAsync(values);
      }
      setEditingId(null);
    }, 'Đã lưu vé lẻ');
  }

  const defaultValues: FlightIndividualForm =
    isEdit && editing
      ? {
          code: editing.code,
          ticketCode: editing.ticketCode,
          pnr: editing.pnr,
          customerName: editing.customerName,
          orderRef: editing.orderRef,
          providerRef: editing.providerRef,
          tripType: editing.tripType,
          route: editing.route,
          departDate: editing.departDate,
          returnDate: editing.returnDate,
          sellAmount: editing.sellAmount,
          receivedAmount: editing.receivedAmount,
          totalCost: editing.totalCost,
          paidAmount: editing.paidAmount,
          paymentDueDate: editing.paymentDueDate,
          status: editing.status,
          assigneeRef: editing.assigneeRef,
          note: editing.note,
        }
      : EMPTY_FORM;

  const columns: ColumnsType<FlightIndividual> = [
    {
      title: 'Vé / Khách',
      key: 'code',
      width: 210,
      render: (_: unknown, r: FlightIndividual) => {
        const bits = [r.ticketCode ? `Vé ${r.ticketCode}` : null, r.pnr ? `PNR ${r.pnr}` : null].filter(Boolean).join(' · ');
        return <CellEntity name={r.customerName} code={r.code} meta={bits || undefined} />;
      },
    },
    {
      title: 'NCC / Đơn hàng',
      key: 'provider',
      width: 170,
      render: (_: unknown, r: FlightIndividual) => <CellEntity name={r.providerName} meta={r.orderCode} />,
    },
    {
      title: 'Hành trình',
      key: 'route',
      width: 210,
      render: (_: unknown, r: FlightIndividual) => (
        <CellStack
          main={r.route}
          sub={`${TRIP_TYPE_LABEL[r.tripType] ?? r.tripType} · CI ${vnDate(r.departDate)} · CO ${vnDate(r.returnDate)}`}
        />
      ),
    },
    {
      title: 'Thu / còn nợ',
      key: 'sell',
      width: 150,
      align: 'right',
      render: (_: unknown, r: FlightIndividual) => (
        <CellMoney value={r.sellAmount} sub={r.receivableRemaining} subLabel="còn" subTone={r.receivableRemaining > 0 ? 'danger' : 'muted'} />
      ),
    },
    {
      title: 'Chi / phải chi',
      key: 'cost',
      width: 150,
      align: 'right',
      render: (_: unknown, r: FlightIndividual) => (
        <CellMoney value={r.totalCost} sub={r.payableRemaining} subLabel="phải chi" subTone={r.payableRemaining > 0 ? 'danger' : 'muted'} />
      ),
    },
    {
      title: 'Lợi nhuận',
      dataIndex: 'profit',
      key: 'profit',
      width: 130,
      align: 'right',
      render: (v: number) => <CellMoney value={v} tone={v < 0 ? 'danger' : 'success'} />,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 150,
      render: (_: unknown, r: FlightIndividual) => (
        <CellStack
          main={<Tag color={FI_STATUS_COLOR[r.status]}>{FI_STATUS_LABEL[r.status] ?? r.status}</Tag>}
          sub={r.paymentDueDate ? `Hạn chi ${vnDate(r.paymentDueDate)}` : undefined}
        />
      ),
    },
    {
      title: '',
      key: '__actions',
      width: 120,
      render: (_: unknown, r: FlightIndividual) =>
        canManage ? (
          <Space>
            <Button size="small" onClick={() => setEditingId(r.id)}>Sửa</Button>
            <Popconfirm title="Xoá vé lẻ này?" onConfirm={() => run(() => remove.mutateAsync(r.id), 'Đã xoá')}>
              <Button size="small" danger>Xoá</Button>
            </Popconfirm>
          </Space>
        ) : null,
    },
  ];

  const s = stats.data;

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Vé máy bay lẻ
        </Typography.Title>
        {canManage ? <Button type="primary" onClick={() => setEditingId('new')}>Thêm vé lẻ</Button> : null}
      </div>

      {/* Footer P/L đưa lên thẻ thống kê */}
      <StatGrid
        style={{ marginBottom: 16 }}
        items={[
          { label: 'Tổng thu', value: money(s?.totalSell ?? 0) },
          { label: 'Thực thu', value: money(s?.totalReceived ?? 0) },
          { label: 'Còn nợ thu', value: money(s?.totalReceivable ?? 0) },
          { label: 'Tổng chi', value: money(s?.totalCost ?? 0) },
          { label: 'Thực chi', value: money(s?.totalPaid ?? 0) },
          { label: 'Phải chi', value: money(s?.totalPayable ?? 0) },
          { label: 'Lợi nhuận', value: money(s?.totalProfit ?? 0) },
        ]}
      />

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search allowClear placeholder="Mã / PNR / khách / số vé" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhà cung cấp / hãng" options={providerOpts} value={providerRef} onChange={(v) => setProviderRef(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={5}>
            <Select allowClear style={{ width: '100%' }} placeholder="Trạng thái duyệt" options={FI_STATUS_OPTIONS} value={status} onChange={(v) => setStatus(v ?? undefined)} />
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
          value={tab ?? 'all'}
          onChange={(val) => {
            setTab(val === 'all' ? undefined : String(val));
            setPage(1);
          }}
          options={[
            { label: `Tất cả (${s?.total ?? 0})`, value: 'all' },
            ...FI_TABS.map((t) => ({ label: `${t.label} (${s?.[t.statKey] ?? 0})`, value: t.value })),
          ]}
        />
      </div>

      <DataCard title="Danh sách vé máy bay lẻ">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
        />
      </DataCard>

      {editingId !== null ? (
        <CrudFormModal
          open
          title={isEdit ? 'Sửa vé lẻ' : 'Tạo vé lẻ'}
          schema={flightIndividualFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditingId(null)}
          onSubmit={onSubmit}
          width={640}
        >
          <TextField name="code" label="Mã hệ thống" required />
          <TextField name="ticketCode" label="Số vé" />
          <TextField name="pnr" label="PNR" />
          <TextField name="customerName" label="Khách đi vé" required />
          <SelectField name="providerRef" label="Nhà cung cấp / hãng" options={providerOpts} allowClear />
          <SelectField name="tripType" label="Loại vé" options={TRIP_TYPE_OPTIONS} />
          <TextField name="route" label="Hành trình" placeholder="VD: SGN-HAN-SGN" />
          <DatePickerField name="departDate" label="Ngày đi (CI)" />
          <DatePickerField name="returnDate" label="Ngày về (CO)" />
          <NumberField name="sellAmount" label="Tổng thu" />
          <NumberField name="receivedAmount" label="Thực thu" />
          <NumberField name="totalCost" label="Tổng chi" />
          <NumberField name="paidAmount" label="Thực chi" />
          <DatePickerField name="paymentDueDate" label="Hạn chi" />
          <SelectField name="status" label="Trạng thái" options={FI_STATUS_OPTIONS} />
          <TextField name="orderRef" label="Đơn hàng (ID/mã)" />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      ) : null}
    </>
  );
}
