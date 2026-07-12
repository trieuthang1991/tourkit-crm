import { App, Button, Card, Col, DatePicker, Input, Row, Segmented, Select, Space, Statistic, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { useNavigate } from 'react-router-dom';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { dateText } from '../../shared/format';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import { departuresCrud } from './departuresApi';
import { BatchDepartureButton } from './BatchDepartureButton';
import { departureFormSchema, departureSchema } from './departureTypes';
import type { Departure, DepartureForm } from './departureTypes';

const statsSchema = z.object({
  total: z.number(),
  upcoming: z.number(),
  closed: z.number(),
  totalSlots: z.number(),
});
const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

type DepAdv = { tourType?: string; assignedToUserId?: string; departureFrom?: string; departureTo?: string };

export function DeparturesPage() {
  const navigate = useNavigate();
  const { message } = App.useApp();
  const { has } = useAuth();
  const canCreate = has('departure.create');
  const create = departuresCrud.useCreate();

  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [closed, setClosed] = useState<boolean | undefined>();
  const [draft, setDraft] = useState<DepAdv>({});
  const [adv, setAdv] = useState<DepAdv>({});
  const [creating, setCreating] = useState(false);

  const setD = (patch: Partial<DepAdv>) => setDraft((d) => ({ ...d, ...patch }));
  const applyFilters = () => {
    setQ(search);
    setAdv(draft);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setClosed(undefined);
    setDraft({});
    setAdv({});
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['departures', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/tour-departures/stats');
      return statsSchema.parse(data);
    },
  });
  const filterOptions = useQuery({
    queryKey: ['departures', 'filter-options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/tour-departures/filter-options');
      return z.object({ tourTypes: z.array(z.string()) }).parse(data);
    },
  });
  const users = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
  const tourTypeOpts = (filterOptions.data?.tourTypes ?? []).map((t) => ({ label: t, value: t }));
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));
  const userName = new Map((users.data ?? []).map((u) => [u.id, u.fullName]));

  const list = useQuery({
    queryKey: ['departures', 'list', page.page, page.size, q, closed, adv],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/tour-departures', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, isClosed: closed, ...adv }),
      });
      return pagedSchema(departureSchema).parse(data);
    },
  });

  async function submitCreate(values: DepartureForm) {
    try {
      await create.mutateAsync(values);
      message.success('Đã tạo chuyến');
      setCreating(false);
    } catch {
      message.error('Tạo chuyến thất bại');
    }
  }

  const columns: ColumnsType<Departure> = [
    {
      title: 'STT',
      key: '__stt',
      width: 60,
      fixed: 'left',
      align: 'center',
      render: (_: unknown, __: Departure, index: number) => (page.page - 1) * page.size + index + 1,
    },
    { title: 'Mã chuyến', dataIndex: 'code', key: 'code', fixed: 'left', width: 140 },
    { title: 'Tên chuyến', dataIndex: 'title', key: 'title', width: 220, ellipsis: true },
    { title: 'Loại tour', dataIndex: 'tourType', key: 'tourType', width: 120, render: (v?: string | null) => v ?? '—' },
    { title: 'Ngày khởi hành', dataIndex: 'departureDate', key: 'departureDate', width: 130, render: (v: string | null) => dateText(v) },
    { title: 'Tổng chỗ', dataIndex: 'totalSlots', key: 'totalSlots', width: 100, align: 'right' },
    {
      title: 'NV điều hành',
      dataIndex: 'assignedToUserId',
      key: 'assignedToUserId',
      width: 160,
      render: (v?: string | null) => (v ? userName.get(v) ?? '—' : '—'),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'isClosed',
      key: 'isClosed',
      width: 120,
      render: (v?: boolean) => <Tag color={v ? 'red' : 'green'}>{v ? 'Đã đóng' : 'Đang mở'}</Tag>,
    },
    {
      title: '',
      key: '__open',
      width: 120,
      fixed: 'right',
      render: (_: unknown, item: Departure) => (
        <Button size="small" onClick={() => navigate(`/departures/${item.id}`)}>
          Mở chuyến
        </Button>
      ),
    },
  ];

  const s = stats.data;
  const statCards = [
    { title: 'Tổng chuyến', value: s?.total ?? 0 },
    { title: 'Sắp khởi hành', value: s?.upcoming ?? 0 },
    { title: 'Đã đóng', value: s?.closed ?? 0 },
    { title: 'Tổng chỗ', value: s?.totalSlots ?? 0 },
  ];

  return (
    <>
      <PageHeader
        title="Chuyến đi (Tour / LKH)"
        extra={
          <Space>
            <BatchDepartureButton />
            {canCreate && (
              <Button type="primary" onClick={() => setCreating(true)}>
                Thêm chuyến
              </Button>
            )}
          </Space>
        }
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
          <Col xs={24} sm={12} lg={8}>
            <Input.Search allowClear placeholder="Mã / tên chuyến" value={search}
              onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={12} sm={8} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Loại tour"
              options={tourTypeOpts} value={draft.tourType} onChange={(v) => setD({ tourType: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="NV điều hành"
              options={userOpts} value={draft.assignedToUserId} onChange={(v) => setD({ assignedToUserId: v ?? undefined })} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Khởi hành từ', 'đến']}
              value={draft.departureFrom && draft.departureTo ? [dayjs(draft.departureFrom), dayjs(draft.departureTo)] : null}
              onChange={(d) => setD({ departureFrom: d?.[0]?.startOf('day').toISOString(), departureTo: d?.[1]?.endOf('day').toISOString() })} />
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
          value={closed === undefined ? 'all' : closed ? 'closed' : 'open'}
          onChange={(val) => {
            setClosed(val === 'all' ? undefined : val === 'closed');
            setPage({ ...page, page: 1 });
          }}
          options={[
            { label: `Tất cả (${s?.total ?? 0})`, value: 'all' },
            { label: 'Đang mở', value: 'open' },
            { label: `Đã đóng (${s?.closed ?? 0})`, value: 'closed' },
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
      />

      {creating && (
        <CrudFormModal
          open={creating}
          title="Thêm chuyến đi"
          schema={departureFormSchema}
          defaultValues={{ templateId: null, code: '', title: '', departureDate: null, endDate: null, totalSlots: 0 }}
          submitting={create.isPending}
          onCancel={() => setCreating(false)}
          onSubmit={submitCreate}
        >
          <TextField name="templateId" label="Tour mẫu (templateId)" />
          <TextField name="code" label="Mã chuyến" required />
          <TextField name="title" label="Tên chuyến" required />
          <DatePickerField name="departureDate" label="Ngày khởi hành" />
          <DatePickerField name="endDate" label="Ngày kết thúc" />
          <NumberField name="totalSlots" label="Tổng số chỗ" required />
        </CrudFormModal>
      )}
    </>
  );
}
