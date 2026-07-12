import { App, Button, Card, Col, Input, Popconfirm, Row, Select, Space, Statistic, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { customersCrud } from './customersCrud';
import {
  CUSTOMER_TYPE_OPTIONS,
  GENDER_OPTIONS,
  customerFormSchema,
  customerSchema,
  customerTypeLabel,
} from './types';
import type { Customer, CustomerForm } from './types';

const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });
const statsSchema = z.object({
  total: z.number(),
  newToday: z.number(),
  newThisMonth: z.number(),
  firstTimeBuyers: z.number(),
  repeatBuyers: z.number(),
});

const dash = (v: string | null | undefined) => (v ? v : '—');
const dateVi = (v: string | null | undefined) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');
const arr = (v: string[] | undefined | null) => (Array.isArray(v) ? v : []);

function AssignedToField() {
  const list = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
  const options = (list.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));
  return <SelectField name="assignedTo" label="NV phụ trách" options={options} mode="multiple" />;
}

export function CustomersPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [typeFilter, setTypeFilter] = useState<number | undefined>();
  const [editing, setEditing] = useState<{ mode: 'create' | 'edit'; item: Customer | null } | null>(null);

  const canCreate = has('customer.create');
  const canUpdate = has('customer.update');
  const canRemove = has('customer.delete');

  const stats = useQuery({
    queryKey: ['customers', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customers/stats');
      return statsSchema.parse(data);
    },
  });

  const list = useQuery({
    queryKey: ['customers', 'list', page.page, page.size, q, typeFilter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customers', {
        params: { page: page.page, size: page.size, q: q || undefined, customerType: typeFilter },
      });
      return pagedSchema(customerSchema).parse(data);
    },
  });

  const create = customersCrud.useCreate();
  const update = customersCrud.useUpdate();
  const remove = customersCrud.useRemove();

  async function submit(values: CustomerForm) {
    try {
      if (editing?.mode === 'edit' && editing.item) {
        await update.mutateAsync({ id: editing.item.id, body: values });
      } else {
        await create.mutateAsync(values);
      }
      message.success('Đã lưu');
      setEditing(null);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<Customer> = [
    { title: 'Mã KH', dataIndex: 'code', key: 'code', width: 130, fixed: 'left', render: dash },
    { title: 'Họ và tên', dataIndex: 'fullName', key: 'fullName', width: 180, fixed: 'left' },
    { title: 'Số điện thoại', dataIndex: 'phone', key: 'phone', width: 120, render: dash },
    { title: 'Email', dataIndex: 'email', key: 'email', width: 170, render: dash },
    { title: 'Tỉnh thành', dataIndex: 'city', key: 'city', width: 120, render: dash },
    {
      title: 'Phân nhóm',
      dataIndex: 'segments',
      key: 'segments',
      width: 200,
      render: (v: string[]) => (arr(v).length ? arr(v).map((s) => <Tag key={s}>{s}</Tag>) : '—'),
    },
    { title: 'Ngày sinh', dataIndex: 'dateOfBirth', key: 'dateOfBirth', width: 110, render: dateVi },
    { title: 'Ngày tạo', dataIndex: 'createdAt', key: 'createdAt', width: 110, render: dateVi },
    {
      title: 'CSKH gần nhất',
      key: 'lastCare',
      width: 180,
      render: (_: unknown, c: Customer) =>
        c.lastCareAt ? (
          <span>
            {dateVi(c.lastCareAt)}
            {c.lastCareContent ? <div style={{ fontSize: 12, color: '#999' }}>{c.lastCareContent}</div> : null}
          </span>
        ) : (
          '—'
        ),
    },
    { title: 'Số lần mua', dataIndex: 'purchaseCount', key: 'purchaseCount', width: 100, align: 'center' },
    { title: 'Doanh thu', dataIndex: 'revenue', key: 'revenue', width: 130, align: 'right', render: (v: number) => money(v ?? 0) },
    { title: 'Loại KH', dataIndex: 'customerType', key: 'customerType', width: 110, render: (v: number) => customerTypeLabel(v) },
    {
      title: 'Người tạo',
      key: 'createdBy',
      width: 140,
      render: (_: unknown, c: Customer) => c.createdByName ?? dash(c.createdBy),
    },
    {
      title: 'NV phụ trách',
      dataIndex: 'assignedToNames',
      key: 'assignedTo',
      width: 160,
      render: (v: string[]) => (arr(v).length ? arr(v).map((n) => <Tag key={n}>{n}</Tag>) : '—'),
    },
    {
      title: 'Thẻ',
      dataIndex: 'tags',
      key: 'tags',
      width: 140,
      render: (v: string[]) => (arr(v).length ? arr(v).map((t) => <Tag key={t} color="blue">{t}</Tag>) : '—'),
    },
    ...(canUpdate || canRemove
      ? [
          {
            title: '',
            key: '__actions',
            width: 150,
            fixed: 'right' as const,
            render: (_: unknown, c: Customer) => (
              <Space>
                {canUpdate ? (
                  <Button size="small" onClick={() => setEditing({ mode: 'edit', item: c })}>
                    Sửa
                  </Button>
                ) : null}
                {canRemove ? (
                  <Popconfirm
                    title="Xoá khách hàng này?"
                    onConfirm={async () => {
                      try {
                        await remove.mutateAsync(c.id);
                        message.success('Đã xoá');
                      } catch (e) {
                        message.error(errorMessage(e));
                      }
                    }}
                  >
                    <Button size="small" danger>
                      Xoá
                    </Button>
                  </Popconfirm>
                ) : null}
              </Space>
            ),
          } as ColumnsType<Customer>[number],
        ]
      : []),
  ];

  const item = editing?.item ?? null;
  const defaultValues: CustomerForm = {
    fullName: item?.fullName ?? '',
    phone: item?.phone ?? null,
    customerType: item?.customerType ?? 0,
    source: item?.source ?? null,
    tag: item?.tag ?? null,
    tempBalance: item?.tempBalance ?? 0,
    email: item?.email ?? null,
    address: item?.address ?? null,
    dateOfBirth: item?.dateOfBirth ?? null,
    idCardNumber: item?.idCardNumber ?? null,
    passportNumber: item?.passportNumber ?? null,
    passportExpiry: item?.passportExpiry ?? null,
    nationality: item?.nationality ?? null,
    gender: item?.gender ?? null,
    city: item?.city ?? null,
    marketGroup: item?.marketGroup ?? null,
    initialNeed: item?.initialNeed ?? null,
    collaboratorName: item?.collaboratorName ?? null,
    campaign: item?.campaign ?? null,
    segments: arr(item?.segments),
    tags: arr(item?.tags),
    assignedTo: arr(item?.assignedTo),
  };

  const statCards = [
    { title: 'Tổng số khách hàng', value: stats.data?.total ?? 0 },
    { title: 'Tạo hôm nay', value: stats.data?.newToday ?? 0 },
    { title: 'Tạo trong tháng', value: stats.data?.newThisMonth ?? 0 },
    { title: 'Mua lần đầu', value: stats.data?.firstTimeBuyers ?? 0 },
    { title: 'Mua lại nhiều lần', value: stats.data?.repeatBuyers ?? 0 },
  ];

  return (
    <>
      <PageHeader
        title="Data khách hàng"
        extra={
          canCreate ? (
            <Button type="primary" onClick={() => setEditing({ mode: 'create', item: null })}>
              Thêm mới
            </Button>
          ) : null
        }
      />

      {/* Thẻ thống kê (bám hệ cũ) */}
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {statCards.map((s) => (
          <Col key={s.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={s.title} value={s.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      {/* Thanh tìm kiếm + lọc */}
      <Space wrap style={{ marginBottom: 12 }}>
        <Input.Search
          allowClear
          placeholder="Tìm theo mã / tên / SĐT / email"
          style={{ width: 320 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onSearch={(v) => {
            setQ(v);
            setPage({ ...page, page: 1 });
          }}
        />
        <Select
          allowClear
          placeholder="Loại khách hàng"
          style={{ width: 180 }}
          options={CUSTOMER_TYPE_OPTIONS}
          value={typeFilter}
          onChange={(v) => {
            setTypeFilter(v);
            setPage({ ...page, page: 1 });
          }}
        />
      </Space>

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
          onChange: (p, s) => setPage({ page: p, size: s }),
        }}
      />

      {editing ? (
        <CrudFormModal
          open
          title={editing.mode === 'edit' ? 'Sửa khách hàng' : 'Thêm khách hàng'}
          schema={customerFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          <TextField name="fullName" label="Họ tên" required />
          <TextField name="phone" label="Điện thoại" />
          <SelectField name="customerType" label="Loại khách hàng" options={CUSTOMER_TYPE_OPTIONS} required />
          <SelectField name="gender" label="Giới tính" options={GENDER_OPTIONS} allowClear />
          <TextField name="email" label="Email" />
          <TextField name="city" label="Tỉnh thành" />
          <TextField name="address" label="Địa chỉ" />
          <DatePickerField name="dateOfBirth" label="Ngày sinh" />
          <SelectField name="segments" label="Phân nhóm" options={[]} mode="tags" />
          <SelectField name="tags" label="Thẻ KH" options={[]} mode="tags" />
          <AssignedToField />
          <TextField name="source" label="Nguồn khách" />
          <TextField name="marketGroup" label="Nhóm/Thị trường" />
          <TextField name="campaign" label="Chiến dịch" />
          <TextField name="collaboratorName" label="CTV" />
          <TextAreaField name="initialNeed" label="Nhu cầu ban đầu" />
          <NumberField name="tempBalance" label="Tạm ứng" required />
          <TextField name="idCardNumber" label="CMND/CCCD" />
          <TextField name="nationality" label="Quốc tịch" />
          <TextField name="passportNumber" label="Số hộ chiếu" />
          <DatePickerField name="passportExpiry" label="Hộ chiếu hết hạn" />
        </CrudFormModal>
      ) : null}
    </>
  );
}
