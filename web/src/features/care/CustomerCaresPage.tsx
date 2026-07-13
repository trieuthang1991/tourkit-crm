import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { dateText, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { customerCaresCrud } from './customerCaresCrud';
import {
  useCustomerCares,
  useCustomerCareStats,
  useCustomerOptions,
  useUserOptions,
} from './customerCaresApi';
import type { CustomerCareFilter } from './customerCaresApi';
import { CARE_STATUS, customerCareCreateSchema, customerCareUpdateSchema } from './customerCareTypes';
import type { CustomerCare, CustomerCareForm } from './customerCareTypes';

const STATUS_COLOR: Record<number, string> = { 0: 'orange', 1: 'processing', 2: 'green' };

export function CustomerCaresPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('care.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [customerId, setCustomerId] = useState<string | undefined>();
  const [assignedToUserId, setAssignedToUserId] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [applied, setApplied] = useState<CustomerCareFilter>({});
  const applyFilters = () => {
    setApplied({ q: search || undefined, customerId, assignedToUserId });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setCustomerId(undefined);
    setAssignedToUserId(undefined);
    setStatus(undefined);
    setApplied({});
    setPage(1);
  };

  const list = useCustomerCares(page, size, { ...applied, status });
  const stats = useCustomerCareStats();
  const customers = useCustomerOptions();
  const users = useUserOptions();
  const customerOpts = (customers.data ?? []).map((c) => ({ label: c.fullName, value: c.id }));
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));

  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<CustomerCare | null>(null);

  const create = customerCaresCrud.useCreate();
  const update = customerCaresCrud.useUpdate();
  const remove = customerCaresCrud.useRemove();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onSubmit(values: CustomerCareForm) {
    await run(async () => {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: values });
        setEditing(null);
      } else {
        await create.mutateAsync(values);
        setCreating(false);
      }
    }, editing ? 'Đã cập nhật' : 'Đã thêm lịch chăm sóc');
  }

  const columns: ColumnsType<CustomerCare> = [
    { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName', render: (v: string | null) => v ?? '—' },
    { title: 'Tiêu đề', dataIndex: 'title', key: 'title' },
    { title: 'Người phụ trách', dataIndex: 'assigneeName', key: 'assigneeName', render: (v: string | null) => v ?? '—' },
    { title: 'Nhắc hẹn', dataIndex: 'remindAt', key: 'remindAt', width: 160, render: (v: string | null) => dateText(v) },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 130,
      render: (v: number) => <Tag color={STATUS_COLOR[v] ?? 'default'}>{statusText(CARE_STATUS, v)}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 160,
      render: (_: unknown, item: CustomerCare) =>
        canManage ? (
          <Space>
            <Button size="small" onClick={() => setEditing(item)}>
              Sửa
            </Button>
            <Popconfirm title="Xoá lịch chăm sóc này?" onConfirm={() => run(() => remove.mutateAsync(item.id), 'Đã xoá')}>
              <Button size="small" danger>
                Xoá
              </Button>
            </Popconfirm>
          </Space>
        ) : null,
    },
  ];

  const statusOpts = Object.entries(CARE_STATUS).map(([v, label]) => ({ label, value: Number(v) }));

  const defaultValues: CustomerCareForm = editing
    ? {
        customerId: editing.customerId,
        title: editing.title,
        detail: editing.detail,
        remindAt: editing.remindAt,
        assignedToUserId: editing.assignedToUserId,
        feedback: editing.feedback,
        status: editing.status,
      }
    : { customerId: '', title: '', detail: null, remindAt: null, assignedToUserId: null, feedback: null, status: 0 };

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Chăm sóc khách hàng
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => setCreating(true)}>
            Thêm lịch chăm sóc
          </Button>
        ) : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Tổng lịch', value: stats.data?.total ?? 0 },
          { title: 'Mới', value: stats.data?.new ?? 0 },
          { title: 'Đang xử lý', value: stats.data?.inProgress ?? 0 },
          { title: 'Hoàn thành', value: stats.data?.done ?? 0 },
          { title: 'Quá hạn', value: stats.data?.overdue ?? 0 },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Tiêu đề" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Khách hàng"
              options={customerOpts} value={customerId} onChange={(v) => setCustomerId(v ?? undefined)} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Người phụ trách"
              options={userOpts} value={assignedToUserId} onChange={(v) => setAssignedToUserId(v ?? undefined)} />
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
          onChange={(val) => { setStatus(val === 'all' ? undefined : Number(val)); setPage(1); }}
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...statusOpts.map((o) => ({ label: o.label, value: String(o.value) }))]}
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

      {creating || editing ? (
        <CrudFormModal
          open
          title={editing ? 'Sửa chăm sóc khách hàng' : 'Thêm chăm sóc khách hàng'}
          schema={editing ? customerCareUpdateSchema : customerCareCreateSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => { setCreating(false); setEditing(null); }}
          onSubmit={onSubmit}
        >
          {editing ? null : <SelectField name="customerId" label="Khách hàng" required options={customerOpts} />}
          <TextField name="title" label="Tiêu đề" required />
          <TextAreaField name="detail" label="Nội dung" />
          <DatePickerField name="remindAt" label="Nhắc hẹn" />
          <SelectField name="assignedToUserId" label="Người phụ trách" options={userOpts} allowClear />
          <SelectField name="status" label="Trạng thái" required options={statusOpts} />
          {editing ? <TextAreaField name="feedback" label="Phản hồi" /> : null}
        </CrudFormModal>
      ) : null}
    </>
  );
}
