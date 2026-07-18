import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Table, Tag, Typography } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { StatGrid } from '../../ui/kit';
import { DataCard } from '../../shared/ui';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { dateText, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { CellStack, CellEntity, CellText, CellDate } from '../../shared/ui/TableCells';
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
    {
      title: 'Khách hàng',
      key: 'customer',
      width: 200,
      render: (_: unknown, item: CustomerCare) => <CellEntity name={item.customerName} />,
    },
    {
      title: 'Nội dung',
      key: 'content',
      width: 300,
      render: (_: unknown, item: CustomerCare) => <CellStack main={item.title} sub={item.detail} />,
    },
    {
      title: 'Người phụ trách',
      dataIndex: 'assigneeName',
      key: 'assigneeName',
      width: 160,
      render: (v: string | null) => <CellText>{v}</CellText>,
    },
    {
      title: 'Nhắc hẹn',
      dataIndex: 'remindAt',
      key: 'remindAt',
      width: 150,
      render: (v: string | null) => <CellDate value={dateText(v)} />,
    },
    {
      title: 'Phản hồi',
      dataIndex: 'feedback',
      key: 'feedback',
      width: 210,
      render: (v: string | null) => <CellText tone="muted">{v}</CellText>,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      render: (v: number) => <Tag color={STATUS_COLOR[v] ?? 'default'}>{statusText(CARE_STATUS, v)}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 150,
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

      <StatGrid
        style={{ marginBottom: 16 }}
        items={[
          { label: 'Tổng lịch', value: stats.data?.total ?? 0 },
          { label: 'Mới', value: stats.data?.new ?? 0 },
          { label: 'Đang xử lý', value: stats.data?.inProgress ?? 0 },
          { label: 'Hoàn thành', value: stats.data?.done ?? 0 },
          { label: 'Quá hạn', value: stats.data?.overdue ?? 0 },
        ]}
      />

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

      <DataCard title="Danh sách lịch chăm sóc">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          scroll={{ x: 1080 }}
          pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
        />
      </DataCard>

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
