import { App, Button, Card, Col, DatePicker, Popconfirm, Row, Segmented, Select, Space, Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { dateText } from '../../shared/format';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CellDate, CellEntity, CellStack, CellText } from '../../shared/ui/TableCells';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { StatGrid } from '../../ui/kit';
import { DataCard } from '../../shared/ui';
import { useAuth } from '../auth/AuthContext';
import { vehiclesCrud } from '../vehicles/vehiclesCrud';
import { departuresCrud } from '../booking/departuresApi';
import { vehicleAssignmentsCrud } from './vehicleAssignmentsCrud';
import { vehicleAssignmentCreateSchema, vehicleAssignmentSchema, vehicleAssignmentUpdateSchema } from './vehicleAssignmentTypes';
import type { VehicleAssignment, VehicleAssignmentForm } from './vehicleAssignmentTypes';

const VA_STATUS: Record<number, string> = { 1: 'Mới', 2: 'Đang chạy', 4: 'Đã xoá' };
const VA_STATUS_COLOR: Record<number, string> = { 1: 'default', 2: 'blue', 4: 'red' };

const statsSchema = z.object({ total: z.number(), created: z.number(), active: z.number(), vehicleCount: z.number() });

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function VehicleAssignmentsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('vehicle.manage');
  const create = vehicleAssignmentsCrud.useCreate();
  const update = vehicleAssignmentsCrud.useUpdate();
  const remove = vehicleAssignmentsCrud.useRemove();

  const [page, setPage] = useState(DEFAULT_PAGE);
  const [vehicleId, setVehicleId] = useState<string | undefined>();
  const [departureId, setDepartureId] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [range, setRange] = useState<{ from?: string; to?: string }>({});
  const [rangeApplied, setRangeApplied] = useState<{ from?: string; to?: string }>({});
  const [advVehicle, setAdvVehicle] = useState<string | undefined>();
  const [advDeparture, setAdvDeparture] = useState<string | undefined>();
  const [editing, setEditing] = useState<VehicleAssignment | 'new' | null>(null);

  const applyFilters = () => {
    setAdvVehicle(vehicleId);
    setAdvDeparture(departureId);
    setRangeApplied(range);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setVehicleId(undefined);
    setDepartureId(undefined);
    setStatus(undefined);
    setRange({});
    setRangeApplied({});
    setAdvVehicle(undefined);
    setAdvDeparture(undefined);
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['vehicle-assignments', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/vehicle-assignments/stats');
      return statsSchema.parse(data);
    },
  });
  const vehicles = vehiclesCrud.useList({ page: 1, size: 500 });
  const vehicleOpts = (vehicles.data?.items ?? []).map((v) => ({ label: `${v.name} (${v.seatType} chỗ)`, value: v.id }));
  const departures = departuresCrud.useList({ page: 1, size: 300 });
  const departureOpts = (departures.data?.items ?? []).map((d) => ({ label: `${d.code} — ${d.title}`, value: d.id }));

  const list = useQuery({
    queryKey: ['vehicle-assignments', 'list', page.page, page.size, advVehicle, advDeparture, status, rangeApplied],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/vehicle-assignments', {
        params: clean({ page: page.page, size: page.size, vehicleId: advVehicle, departureId: advDeparture, status, dateFrom: rangeApplied.from, dateTo: rangeApplied.to }),
      });
      return pagedSchema(vehicleAssignmentSchema).parse(data);
    },
  });

  async function submit(values: VehicleAssignmentForm) {
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

  const columns: ColumnsType<VehicleAssignment> = [
    {
      title: 'Chuyến',
      key: '__dep',
      width: 280,
      render: (_: unknown, r: VehicleAssignment) =>
        r.departureCode ? (
          <CellEntity name={r.departureTitle ?? r.departureCode} code={r.departureCode} />
        ) : (
          <CellText mono>{r.tourDepartureId.slice(0, 8)}</CellText>
        ),
    },
    {
      title: 'Xe',
      dataIndex: 'vehicleName',
      key: 'vehicleName',
      width: 180,
      render: (v: string | null) => <CellText tone="heading" strong>{v ?? '—'}</CellText>,
    },
    {
      title: 'Tài xế',
      key: '__driver',
      width: 190,
      render: (_: unknown, r: VehicleAssignment) => <CellStack main={r.driverName ?? '—'} sub={r.driverPhone ?? undefined} subMono />,
    },
    {
      title: 'Thời gian',
      key: '__time',
      width: 210,
      render: (_: unknown, r: VehicleAssignment) => (
        <CellDate value={r.timeGo ? dateText(r.timeGo) : '—'} sub={r.timeCome ? `Trả: ${dateText(r.timeCome)}` : undefined} />
      ),
    },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 120, render: (v: number) => <Tag color={VA_STATUS_COLOR[v]}>{VA_STATUS[v] ?? v}</Tag> },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 120,
            fixed: 'right' as const,
            render: (_: unknown, r: VehicleAssignment) => (
              <Space>
                <Button size="small" onClick={() => setEditing(r)}>Sửa</Button>
                <Popconfirm title="Xoá phân xe này?" onConfirm={() => onDelete(r.id)}>
                  <Button size="small" danger>Xoá</Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<VehicleAssignment>[number],
        ]
      : []),
  ];

  const s = stats.data;
  const statCards = [
    { label: 'Tổng phân xe', value: s?.total ?? 0 },
    { label: 'Mới', value: s?.created ?? 0 },
    { label: 'Đang chạy', value: s?.active ?? 0 },
    { label: 'Số xe', value: s?.vehicleCount ?? 0 },
  ];

  const isEdit = editing && editing !== 'new';
  const defaultValues: VehicleAssignmentForm = isEdit
    ? { tourDepartureId: editing.tourDepartureId, vehicleId: editing.vehicleId, driverName: editing.driverName, driverPhone: editing.driverPhone, timeGo: editing.timeGo, timeCome: editing.timeCome, note: editing.note, status: editing.status }
    : { tourDepartureId: '', vehicleId: '', driverName: null, driverPhone: null, timeGo: null, timeCome: null, note: null, status: 1 };

  return (
    <>
      <PageHeader
        title="Lịch điều xe"
        extra={canManage ? <Button type="primary" onClick={() => setEditing('new')}>Thêm phân xe</Button> : undefined}
      />

      <StatGrid items={statCards} style={{ marginBottom: 16 }} />

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Xe"
              options={vehicleOpts} value={vehicleId} onChange={(v) => setVehicleId(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Chuyến"
              options={departureOpts} value={departureId} onChange={(v) => setDepartureId(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Giờ đón từ', 'đến']}
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
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => {
            setStatus(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: `Tất cả (${s?.total ?? 0})`, value: 'all' }, { label: `Mới (${s?.created ?? 0})`, value: '1' }, { label: `Đang chạy (${s?.active ?? 0})`, value: '2' }]}
        />
      </div>

      <DataCard title="Danh sách điều xe">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          scroll={{ x: 1080 }}
          pagination={{
            current: page.page,
            pageSize: page.size,
            total: list.data?.total ?? 0,
            showSizeChanger: true,
            onChange: (p, sz) => setPage({ page: p, size: sz }),
          }}
        />
      </DataCard>

      {editing && (
        <CrudFormModal
          open
          title={isEdit ? 'Sửa phân xe' : 'Thêm phân xe'}
          schema={isEdit ? vehicleAssignmentUpdateSchema : vehicleAssignmentCreateSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          {!isEdit ? <TextField name="tourDepartureId" label="Mã chuyến (departureId)" required /> : null}
          <TextField name="vehicleId" label="Mã xe (vehicleId)" required />
          <TextField name="driverName" label="Tài xế" />
          <TextField name="driverPhone" label="SĐT tài xế" />
          <DatePickerField name="timeGo" label="Giờ đón" />
          <DatePickerField name="timeCome" label="Giờ trả" />
          <TextAreaField name="note" label="Ghi chú" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    </>
  );
}
