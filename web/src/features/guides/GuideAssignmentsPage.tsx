import { App, Button, Card, Col, DatePicker, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { dateText } from '../../shared/format';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import { providersCrud } from '../providers/providersCrud';
import { departuresCrud } from '../booking/departuresApi';
import { guideAssignmentsCrud } from './guideAssignmentsCrud';
import { GuideTransactionCell } from './GuideTransactionCell';
import { GuideHandoverCell } from './GuideHandoverCell';
import { guideAssignmentCreateSchema, guideAssignmentSchema, guideAssignmentUpdateSchema } from './guideAssignmentTypes';
import type { GuideAssignment, GuideAssignmentForm } from './guideAssignmentTypes';

const GUIDE_STATUS: Record<number, string> = { 1: 'Mới', 2: 'Đang chạy', 4: 'Đã xoá' };
const GUIDE_STATUS_COLOR: Record<number, string> = { 1: 'default', 2: 'blue', 4: 'red' };

const statsSchema = z.object({ total: z.number(), created: z.number(), active: z.number(), guideCount: z.number() });

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function GuideAssignmentsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('guide.manage');
  const create = guideAssignmentsCrud.useCreate();
  const update = guideAssignmentsCrud.useUpdate();
  const remove = guideAssignmentsCrud.useRemove();

  const [page, setPage] = useState(DEFAULT_PAGE);
  const [providerId, setProviderId] = useState<string | undefined>();
  const [departureId, setDepartureId] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [range, setRange] = useState<{ from?: string; to?: string }>({});
  const [rangeApplied, setRangeApplied] = useState<{ from?: string; to?: string }>({});
  const [advProvider, setAdvProvider] = useState<string | undefined>();
  const [advDeparture, setAdvDeparture] = useState<string | undefined>();
  const [editing, setEditing] = useState<GuideAssignment | 'new' | null>(null);

  const applyFilters = () => {
    setAdvProvider(providerId);
    setAdvDeparture(departureId);
    setRangeApplied(range);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setProviderId(undefined);
    setDepartureId(undefined);
    setStatus(undefined);
    setRange({});
    setRangeApplied({});
    setAdvProvider(undefined);
    setAdvDeparture(undefined);
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['guide-assignments', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/guide-assignments/stats');
      return statsSchema.parse(data);
    },
  });
  const providers = providersCrud.useList({ page: 1, size: 500 });
  const guideOpts = (providers.data?.items ?? []).filter((p) => p.type === 4).map((p) => ({ label: p.name, value: p.id }));
  const departures = departuresCrud.useList({ page: 1, size: 300 });
  const departureOpts = (departures.data?.items ?? []).map((d) => ({ label: `${d.code} — ${d.title}`, value: d.id }));

  const list = useQuery({
    queryKey: ['guide-assignments', 'list', page.page, page.size, advProvider, advDeparture, status, rangeApplied],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/guide-assignments', {
        params: clean({ page: page.page, size: page.size, providerId: advProvider, departureId: advDeparture, status, dateFrom: rangeApplied.from, dateTo: rangeApplied.to }),
      });
      return pagedSchema(guideAssignmentSchema).parse(data);
    },
  });

  async function submit(values: GuideAssignmentForm) {
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

  const columns: ColumnsType<GuideAssignment> = [
    {
      title: 'Chuyến',
      key: '__dep',
      width: 220,
      ellipsis: true,
      render: (_: unknown, r: GuideAssignment) => (r.departureCode ? `${r.departureCode} — ${r.departureTitle ?? ''}` : r.tourDepartureId.slice(0, 8)),
    },
    { title: 'HDV', dataIndex: 'providerName', key: 'providerName', width: 180, render: (v: string | null) => v ?? '—' },
    { title: 'Giờ đi', dataIndex: 'timeGo', key: 'timeGo', width: 150, render: (v: string | null) => dateText(v) },
    { title: 'Giờ về', dataIndex: 'timeCome', key: 'timeCome', width: 150, render: (v: string | null) => dateText(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 110, render: (v: number) => <Tag color={GUIDE_STATUS_COLOR[v]}>{GUIDE_STATUS[v] ?? v}</Tag> },
    { title: 'Thu-chi', key: '__tx', width: 100, render: (_: unknown, item: GuideAssignment) => <GuideTransactionCell assignmentId={item.id} /> },
    { title: 'Bàn giao', key: '__handover', width: 110, render: (_: unknown, item: GuideAssignment) => <GuideHandoverCell item={item} /> },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 130,
            fixed: 'right' as const,
            render: (_: unknown, r: GuideAssignment) => (
              <Space>
                <Button size="small" onClick={() => setEditing(r)}>Sửa</Button>
                <Popconfirm title="Xoá phân công này?" onConfirm={() => onDelete(r.id)}>
                  <Button size="small" danger>Xoá</Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<GuideAssignment>[number],
        ]
      : []),
  ];

  const s = stats.data;
  const statCards = [
    { title: 'Tổng phân công', value: s?.total ?? 0 },
    { title: 'Mới', value: s?.created ?? 0 },
    { title: 'Đang chạy', value: s?.active ?? 0 },
    { title: 'Số HDV', value: s?.guideCount ?? 0 },
  ];

  const isEdit = editing && editing !== 'new';
  const defaultValues: GuideAssignmentForm = isEdit
    ? { tourDepartureId: editing.tourDepartureId, providerId: editing.providerId, timeGo: editing.timeGo, timeCome: editing.timeCome, timeReturn: editing.timeReturn, note: editing.note, status: editing.status }
    : { tourDepartureId: '', providerId: '', timeGo: null, timeCome: null, timeReturn: null, note: null, status: 1 };

  return (
    <>
      <PageHeader
        title="Lịch điều HDV"
        extra={canManage ? <Button type="primary" onClick={() => setEditing('new')}>Thêm phân công</Button> : undefined}
      />

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {statCards.map((c) => (
          <Col key={c.title} xs={12} sm={12} lg={6} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Hướng dẫn viên"
              options={guideOpts} value={providerId} onChange={(v) => setProviderId(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Chuyến"
              options={departureOpts} value={departureId} onChange={(v) => setDepartureId(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Giờ đi từ', 'đến']}
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
          title={isEdit ? 'Sửa phân công HDV' : 'Thêm phân công HDV'}
          schema={isEdit ? guideAssignmentUpdateSchema : guideAssignmentCreateSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          {!isEdit ? <TextField name="tourDepartureId" label="Mã chuyến (departureId)" required /> : null}
          <TextField name="providerId" label="Mã HDV (providerId)" required />
          <DatePickerField name="timeGo" label="Giờ đi" />
          <DatePickerField name="timeCome" label="Giờ về" />
          <DatePickerField name="timeReturn" label="Giờ trả tour" />
          <TextAreaField name="note" label="Ghi chú" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    </>
  );
}
