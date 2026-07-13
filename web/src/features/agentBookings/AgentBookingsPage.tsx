import { App, Button, Card, Col, Input, Modal, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { TextAreaField, TextField } from '../../shared/ui/Field';
import { agentsCrud } from '../agents/agentsCrud';
import {
  useAddPassenger,
  useAgentBooking,
  useAgentBookings,
  useAgentBookingStats,
  useCreateAgentBooking,
  useRemovePassenger,
} from './agentBookingsApi';
import type { AgentBookingFilter } from './agentBookingsApi';
import { AGENT_BOOKING_STATUS, addPassengerFormSchema, createAgentBookingFormSchema } from './types';
import type { AddPassengerForm, AgentBookingSummary, AgentPassenger, CreateAgentBookingForm } from './types';

const AB_STATUS_COLOR: Record<number, string> = { 0: 'orange', 1: 'blue', 2: 'red', 3: 'green' };

export function AgentBookingsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('agentquote.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [agentId, setAgentId] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [filter, setFilter] = useState<AgentBookingFilter>({});
  const applyFilters = () => setFilter({ q: search || undefined, agentId });
  const resetFilters = () => {
    setSearch('');
    setAgentId(undefined);
    setStatus(undefined);
    setFilter({});
    setPage(1);
  };
  const list = useAgentBookings(page, size, { ...filter, status });
  const stats = useAgentBookingStats();
  const agents = agentsCrud.useList({ page: 1, size: 500 });
  const agentOpts = (agents.data?.items ?? []).map((a) => ({ label: a.name, value: a.id }));

  const [creating, setCreating] = useState(false);
  const [paxBookingId, setPaxBookingId] = useState<string | null>(null);
  const [addingPax, setAddingPax] = useState(false);

  const create = useCreateAgentBooking();
  const addPax = useAddPassenger();
  const removePax = useRemovePassenger();
  const detail = useAgentBooking(paxBookingId ?? '');

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onCreate(values: CreateAgentBookingForm) {
    await run(async () => {
      await create.mutateAsync(values);
      setCreating(false);
    }, 'Đã tạo booking');
  }

  async function onAddPax(values: AddPassengerForm) {
    if (!paxBookingId) return;
    await run(async () => {
      await addPax.mutateAsync({ bookingId: paxBookingId, body: values });
      setAddingPax(false);
    }, 'Đã thêm hành khách');
  }

  const columns: ColumnsType<AgentBookingSummary> = [
    { title: 'Mã', dataIndex: 'code', key: 'code', width: 150 },
    { title: 'Đại lý', dataIndex: 'agentName', key: 'agentName', width: 200, render: (v: string | null) => v ?? '—' },
    { title: 'Tổng tiền', dataIndex: 'totalAmount', key: 'totalAmount', width: 150, align: 'right', render: (v: number) => money(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 130, render: (v: number) => <Tag color={AB_STATUS_COLOR[v]}>{statusText(AGENT_BOOKING_STATUS, v)}</Tag> },
    {
      title: '',
      key: '__actions',
      width: 140,
      render: (_: unknown, item: AgentBookingSummary) => (
        <Button size="small" onClick={() => setPaxBookingId(item.id)}>
          Hành khách
        </Button>
      ),
    },
  ];

  const paxColumns: ColumnsType<AgentPassenger> = [
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
    { title: 'Hộ chiếu', dataIndex: 'passportNo', key: 'passportNo' },
    { title: 'Quốc tịch', dataIndex: 'nationality', key: 'nationality' },
    {
      title: '',
      key: '__x',
      width: 80,
      render: (_: unknown, p: AgentPassenger) =>
        canManage ? (
          <Popconfirm
            title="Xoá hành khách?"
            onConfirm={() => run(() => removePax.mutateAsync({ bookingId: paxBookingId!, passengerId: p.id }), 'Đã xoá')}
          >
            <Button size="small" danger>
              Xoá
            </Button>
          </Popconfirm>
        ) : null,
    },
  ];

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Đặt chỗ Đại lý (B2B)
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => setCreating(true)}>
            Tạo từ báo giá
          </Button>
        ) : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Tổng đặt chỗ', value: stats.data?.total ?? 0, money: false },
          { title: 'Tổng tiền', value: stats.data?.totalAmount ?? 0, money: true },
          { title: 'Chờ', value: stats.data?.pending ?? 0, money: false },
          { title: 'Xác nhận', value: stats.data?.confirmed ?? 0, money: false },
          { title: 'Hoàn tất', value: stats.data?.done ?? 0, money: false },
          { title: 'Huỷ', value: stats.data?.cancelled ?? 0, money: false },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} formatter={c.money ? (v) => money(Number(v)) : undefined} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Mã booking" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Đại lý"
              options={agentOpts} value={agentId} onChange={(v) => setAgentId(v ?? undefined)} />
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
            setPage(1);
          }}
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...Object.entries(AGENT_BOOKING_STATUS).map(([v, label]) => ({ label, value: v }))]}
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
          title="Tạo booking từ báo giá đã xác nhận"
          schema={createAgentBookingFormSchema}
          defaultValues={{ quoteRequestId: '', code: '', note: null }}
          submitting={create.isPending}
          onCancel={() => setCreating(false)}
          onSubmit={onCreate}
        >
          <TextField name="quoteRequestId" label="Mã yêu cầu báo giá (Confirmed)" required />
          <TextField name="code" label="Mã booking" />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      ) : null}

      <Modal
        open={!!paxBookingId}
        title="Hành khách"
        footer={null}
        onCancel={() => setPaxBookingId(null)}
        width={640}
      >
        {canManage ? (
          <Button style={{ marginBottom: 12 }} onClick={() => setAddingPax(true)}>
            + Thêm hành khách
          </Button>
        ) : null}
        <Table
          rowKey="id"
          size="small"
          columns={paxColumns}
          dataSource={detail.data?.passengers ?? []}
          loading={detail.isLoading}
          pagination={false}
        />
      </Modal>

      {addingPax ? (
        <CrudFormModal
          open
          title="Thêm hành khách"
          schema={addPassengerFormSchema}
          defaultValues={{ fullName: '', dateOfBirth: null, passportNo: null, nationality: null, note: null }}
          submitting={addPax.isPending}
          onCancel={() => setAddingPax(false)}
          onSubmit={onAddPax}
        >
          <TextField name="fullName" label="Họ tên" required />
          <TextField name="passportNo" label="Số hộ chiếu" />
          <TextField name="nationality" label="Quốc tịch" />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      ) : null}
    </>
  );
}
