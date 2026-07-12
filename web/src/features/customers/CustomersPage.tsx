import {
  App,
  Button,
  Card,
  Col,
  DatePicker,
  Input,
  InputNumber,
  Popconfirm,
  Row,
  Segmented,
  Select,
  Space,
  Statistic,
  Table,
  Tag,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
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

const MONTH_OPTIONS = Array.from({ length: 12 }, (_, i) => ({ label: `Tháng ${i + 1}`, value: i + 1 }));

// Bộ lọc mở rộng (bám thanh "Xem thêm bộ lọc" hệ cũ). Rỗng = không lọc.
type AdvFilters = {
  revenueFrom?: number;
  revenueTo?: number;
  createdFrom?: string;
  createdTo?: string;
  careFrom?: string;
  careTo?: string;
  branch?: string;
  group?: string;
  source?: string;
  marketGroup?: string;
  city?: string;
  department?: string;
  gender?: string;
  collaborator?: string;
  campaign?: string;
  tag?: string;
  assignedTo?: string;
  createdBy?: string;
  birthdayMonth?: number;
};

// Bỏ field rỗng để không gửi param thừa.
function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(
    Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''),
  );
}

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
  const [moreOpen, setMoreOpen] = useState(false);
  const [draft, setDraft] = useState<AdvFilters>({});
  const [adv, setAdv] = useState<AdvFilters>({});
  const [editing, setEditing] = useState<{ mode: 'create' | 'edit'; item: Customer | null } | null>(null);

  const setD = (patch: Partial<AdvFilters>) => setDraft((d) => ({ ...d, ...patch }));
  const applyFilters = () => {
    setQ(search);
    setAdv(draft);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setDraft({});
    setAdv({});
    setTypeFilter(undefined);
    setPage({ ...page, page: 1 });
  };

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
    queryKey: ['customers', 'list', page.page, page.size, q, typeFilter, adv],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customers', {
        params: cleanParams({
          page: page.page,
          size: page.size,
          q: q || undefined,
          customerType: typeFilter,
          ...adv,
        }),
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
    {
      title: 'STT',
      key: '__stt',
      width: 60,
      fixed: 'left',
      align: 'center',
      render: (_: unknown, __: Customer, index: number) => (page.page - 1) * page.size + index + 1,
    },
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
    { title: 'Ngày CSKH gần nhất', dataIndex: 'lastCareAt', key: 'lastCareAt', width: 150, render: dateVi },
    { title: 'Nội dung CSKH mới nhất', dataIndex: 'lastCareContent', key: 'lastCareContent', width: 200, render: dash },
    { title: 'Nhu cầu ban đầu', dataIndex: 'initialNeed', key: 'initialNeed', width: 200, render: dash },
    { title: 'Số lần mua', dataIndex: 'purchaseCount', key: 'purchaseCount', width: 100, align: 'center' },
    { title: 'Doanh thu', dataIndex: 'revenue', key: 'revenue', width: 130, align: 'right', render: (v: number) => money(v ?? 0) },
    { title: 'Loại KH', dataIndex: 'customerType', key: 'customerType', width: 110, render: (v: number) => customerTypeLabel(v) },
    {
      title: 'Người tạo',
      key: 'createdBy',
      width: 140,
      render: (_: unknown, c: Customer) => c.createdByName ?? dash(c.createdBy),
    },
    { title: 'CTV', dataIndex: 'collaboratorName', key: 'collaboratorName', width: 140, render: dash },
    { title: 'Chiến dịch', dataIndex: 'campaign', key: 'campaign', width: 150, render: dash },
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
    branch: item?.branch ?? null,
    group: item?.group ?? null,
    department: item?.department ?? null,
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

      {/* Thanh lọc đầy đủ (bám "Xem thêm bộ lọc" hệ cũ) */}
      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search
              allowClear
              placeholder="Nhập tên, SĐT, Email…"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              onSearch={applyFilters}
            />
          </Col>
          <Col xs={12} sm={6} lg={4}>
            <InputNumber
              style={{ width: '100%' }}
              placeholder="Doanh thu từ"
              min={0}
              value={draft.revenueFrom}
              onChange={(v) => setD({ revenueFrom: v ?? undefined })}
            />
          </Col>
          <Col xs={12} sm={6} lg={4}>
            <InputNumber
              style={{ width: '100%' }}
              placeholder="Doanh thu đến"
              min={0}
              value={draft.revenueTo}
              onChange={(v) => setD({ revenueTo: v ?? undefined })}
            />
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <DatePicker.RangePicker
              style={{ width: '100%' }}
              placeholder={['Thời gian tạo từ', 'đến']}
              value={draft.createdFrom && draft.createdTo ? [dayjs(draft.createdFrom), dayjs(draft.createdTo)] : null}
              onChange={(d) =>
                setD({
                  createdFrom: d?.[0]?.startOf('day').toISOString(),
                  createdTo: d?.[1]?.endOf('day').toISOString(),
                })
              }
            />
          </Col>

          {moreOpen ? (
            <>
              <Col xs={24} sm={12} lg={8}>
                <DatePicker.RangePicker
                  style={{ width: '100%' }}
                  placeholder={['Thời gian chăm sóc từ', 'đến']}
                  value={draft.careFrom && draft.careTo ? [dayjs(draft.careFrom), dayjs(draft.careTo)] : null}
                  onChange={(d) =>
                    setD({
                      careFrom: d?.[0]?.startOf('day').toISOString(),
                      careTo: d?.[1]?.endOf('day').toISOString(),
                    })
                  }
                />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Chi nhánh" value={draft.branch} onChange={(e) => setD({ branch: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Nhóm" value={draft.group} onChange={(e) => setD({ group: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Nguồn khách" value={draft.source} onChange={(e) => setD({ source: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Người tạo" value={draft.createdBy} onChange={(e) => setD({ createdBy: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Thị trường" value={draft.marketGroup} onChange={(e) => setD({ marketGroup: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Tỉnh thành" value={draft.city} onChange={(e) => setD({ city: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Phòng ban" value={draft.department} onChange={(e) => setD({ department: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Select
                  allowClear
                  style={{ width: '100%' }}
                  placeholder="Giới tính"
                  options={GENDER_OPTIONS}
                  value={draft.gender}
                  onChange={(v) => setD({ gender: v ?? undefined })}
                />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Tên CTV" value={draft.collaborator} onChange={(e) => setD({ collaborator: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Tag" value={draft.tag} onChange={(e) => setD({ tag: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="Chiến dịch" value={draft.campaign} onChange={(e) => setD({ campaign: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Input placeholder="NV phụ trách" value={draft.assignedTo} onChange={(e) => setD({ assignedTo: e.target.value })} />
              </Col>
              <Col xs={12} sm={8} lg={4}>
                <Select
                  allowClear
                  style={{ width: '100%' }}
                  placeholder="Sinh nhật (tháng)"
                  options={MONTH_OPTIONS}
                  value={draft.birthdayMonth}
                  onChange={(v) => setD({ birthdayMonth: v ?? undefined })}
                />
              </Col>
            </>
          ) : null}

          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>
                Tìm kiếm
              </Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
              <Button type="link" onClick={() => setMoreOpen((o) => !o)}>
                {moreOpen ? 'Thu gọn bộ lọc' : 'Xem thêm bộ lọc'}
              </Button>
            </Space>
          </Col>
        </Row>
      </Card>

      {/* Tabs loại khách hàng (bám staging: Tất cả · Cá nhân · Doanh nghiệp · Đối tác · CTV) */}
      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={typeFilter === undefined ? 'all' : String(typeFilter)}
          onChange={(val) => {
            setTypeFilter(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[
            { label: 'Tất cả', value: 'all' },
            ...CUSTOMER_TYPE_OPTIONS.map((o) => ({ label: o.label, value: String(o.value) })),
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
          onChange: (p, s) => setPage({ page: p, size: s }),
        }}
        summary={(pageData) => {
          const purchases = pageData.reduce((s, c) => s + (c.purchaseCount ?? 0), 0);
          const revenueSum = pageData.reduce((s, c) => s + (c.revenue ?? 0), 0);
          return (
            <Table.Summary fixed>
              <Table.Summary.Row>
                <Table.Summary.Cell index={0} colSpan={columns.length}>
                  <Space size="large">
                    <strong>Tổng cộng (trang này)</strong>
                    <span>
                      Tổng số lần mua: <strong>{purchases}</strong>
                    </span>
                    <span>
                      Tổng doanh thu: <strong>{money(revenueSum)}</strong>
                    </span>
                  </Space>
                </Table.Summary.Cell>
              </Table.Summary.Row>
            </Table.Summary>
          );
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
          <TextField name="branch" label="Chi nhánh" />
          <TextField name="group" label="Nhóm khách" />
          <TextField name="department" label="Phòng ban" />
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
