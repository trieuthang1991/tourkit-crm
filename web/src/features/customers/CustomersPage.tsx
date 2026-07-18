import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { Button, DataCard, FilterChip, SegmentTabs, StatCardIcon } from '../../ui/kit';
import { ExportButton } from '../../shared/ui';
import { downloadBlob } from '../../shared/exportCsv';
import { DateRangeInput, NumberInput, SearchInput, Select } from '../../ui/inputs';
import { Popconfirm } from '../../ui/overlay';
import { CellEntity, CellStack, CellMoney } from '../../shared/ui/TableCells';
import { useToast } from '../../ui/message';
import { Pagination, Table } from '../../ui/Table';
import type { Column } from '../../ui/Table';
import { CrudDrawer, DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../ui/form';
import { useAuth } from '../auth/AuthContext';
import { customersCrud } from './customersCrud';
import { CUSTOMER_TYPE_OPTIONS, GENDER_OPTIONS, customerFormSchema, customerSchema, customerTypeLabel } from './types';
import type { Customer, CustomerForm } from './types';

/* Màn "Data khách hàng" (/customers) — hệ Refined. KHÔNG antd.
   Dữ liệu/bộ lọc/quyền giữ NGUYÊN; chỉ đổi lớp trình bày. */

const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });
const strList = z.array(z.string());
const filterOptionsSchema = z.object({
  sources: strList,
  cities: strList,
  marketGroups: strList,
  campaigns: strList,
  collaborators: strList,
  branches: strList,
  groups: strList,
  departments: strList,
  tags: strList,
  segments: strList,
});
const funnelSchema = z.object({
  total: z.number(),
  segments: z.array(z.object({ name: z.string(), count: z.number() })),
  care: z.object({
    firstTime: z.number(),
    repeat: z.number(),
    notContacted7: z.number(),
    notContacted15: z.number(),
    notContacted30: z.number(),
    notContacted90: z.number(),
  }),
});
const statsSchema = z.object({
  total: z.number(),
  newToday: z.number(),
  newThisMonth: z.number(),
  firstTimeBuyers: z.number(),
  repeatBuyers: z.number(),
});

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
  segment?: string;
  purchaseBucket?: 'first' | 'repeat';
  notContactedBucket?: 'nc7' | 'nc15' | 'nc30' | 'nc90';
};

// Bỏ field rỗng để không gửi param thừa.
export function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
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
  const message = useToast();
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

  // Danh sách giá trị cho dropdown lọc (facets) + nhân viên (NV phụ trách / người tạo).
  const filterOptions = useQuery({
    queryKey: ['customers', 'filter-options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customers/filter-options');
      return filterOptionsSchema.parse(data);
    },
  });
  const users = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
  const funnel = useQuery({
    queryKey: ['customers', 'funnel'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customers/funnel');
      return funnelSchema.parse(data);
    },
  });
  const fn = funnel.data;
  const fo = filterOptions.data;
  const strOpts = (xs?: string[]) => (xs ?? []).map((v) => ({ label: v, value: v }));
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));

  // Chip phễu/chăm sóc: merge vào draft + áp dụng ngay (giữ đồng bộ với thanh lọc).
  const pickChip = (patch: Partial<AdvFilters>) => {
    const next = { ...draft, ...patch };
    setDraft(next);
    setAdv(next);
    setQ(search);
    setPage({ ...page, page: 1 });
  };

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

  const rows = list.data?.items ?? [];

  // Xuất CSV TOÀN BỘ khách khớp bộ lọc (server-side, mọi trang) — không chỉ trang đang xem.
  const exportCsv = async () => {
    const res = await httpClient.get('/api/v1/customers/export', {
      params: cleanParams({ q: q || undefined, customerType: typeFilter, ...adv }),
      responseType: 'blob',
    });
    downloadBlob(res.data as Blob, 'khach-hang.csv');
  };

  // Gom 20 cột phẳng -> 7 ô "giàu" (dùng chung CellStack/CellEntity/CellMoney) để KHÔNG scroll ngang.
  const columns: Column<Customer>[] = [
    { key: '__stt', title: '#', width: 44, align: 'center', mono: true, render: (_c, i) => (page.page - 1) * page.size + i + 1 },
    { key: 'customer', title: 'Khách hàng', width: 208, render: (c) => <CellEntity name={c.fullName} code={c.code} meta={customerTypeLabel(c.customerType)} /> },
    { key: 'contact', title: 'Liên hệ', width: 176, render: (c) => <CellStack main={c.phone ?? '—'} sub={c.email} mono /> },
    { key: 'area', title: 'Khu vực & nhóm', width: 176, render: (c) => <CellStack main={c.city ?? '—'} sub={arr(c.segments).join(' · ')} /> },
    { key: 'care', title: 'CSKH gần nhất', width: 196, render: (c) => <CellStack main={dateVi(c.lastCareAt)} sub={c.lastCareContent} /> },
    { key: 'owner', title: 'Phụ trách', width: 158, render: (c) => <CellStack main={arr(c.assignedToNames).join(', ') || '—'} sub={c.collaboratorName ? `CTV: ${c.collaboratorName}` : (c.createdByName ?? undefined)} /> },
    { key: 'revenue', title: 'Doanh thu', width: 132, align: 'right', render: (c) => <CellMoney value={c.revenue ?? 0} sub={`${(c.purchaseCount ?? 0).toLocaleString('vi-VN')} lần mua`} tone={(c.revenue ?? 0) > 0 ? 'success' : 'heading'} /> },
    ...(canUpdate || canRemove
      ? [
          {
            key: '__actions',
            title: '',
            width: 132,
            render: (c: Customer) => (
              <span style={{ display: 'inline-flex', gap: 6 }}>
                {canUpdate ? (
                  <Button size="sm" icon="edit" onClick={() => setEditing({ mode: 'edit', item: c })}>
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
                    <Button variant="danger" size="sm" icon="delete">
                      Xoá
                    </Button>
                  </Popconfirm>
                ) : null}
              </span>
            ),
          } as Column<Customer>,
        ]
      : []),
  ];

  // Tổng cộng trang hiện tại (giữ đủ 2 chỉ số như bản cũ).
  const purchases = rows.reduce((s, c) => s + (c.purchaseCount ?? 0), 0);
  const revenueSum = rows.reduce((s, c) => s + (c.revenue ?? 0), 0);
  const summary = (
    <td colSpan={columns.length}>
      <div style={{ display: 'flex', flexWrap: 'wrap', gap: '6px 22px', alignItems: 'baseline' }}>
        <strong style={{ color: 'var(--tk-heading)' }}>Tổng cộng (trang này)</strong>
        <span style={{ color: 'var(--tk-muted-2)', fontSize: 12 }}>
          Tổng số lần mua: <strong style={{ fontFamily: 'var(--tk-font-mono)', color: 'var(--tk-heading)' }}>{purchases.toLocaleString('vi-VN')}</strong>
        </span>
        <span style={{ color: 'var(--tk-muted-2)', fontSize: 12 }}>
          Tổng doanh thu: <strong style={{ fontFamily: 'var(--tk-font-mono)', color: 'var(--tk-heading)' }}>{money(revenueSum)}</strong>
        </span>
      </div>
    </td>
  );

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
    { label: 'Tổng số khách hàng', value: (stats.data?.total ?? 0).toLocaleString('vi-VN'), icon: 'groups', tone: 'accent' as const },
    { label: 'Tạo hôm nay', value: (stats.data?.newToday ?? 0).toLocaleString('vi-VN'), icon: 'person_add', tone: 'info' as const },
    { label: 'Tạo trong tháng', value: (stats.data?.newThisMonth ?? 0).toLocaleString('vi-VN'), icon: 'calendar_month', tone: 'warning' as const },
    { label: 'Mua lần đầu', value: (stats.data?.firstTimeBuyers ?? 0).toLocaleString('vi-VN'), icon: 'star', tone: 'success' as const },
    { label: 'Mua lại nhiều lần', value: (stats.data?.repeatBuyers ?? 0).toLocaleString('vi-VN'), icon: 'refresh', tone: 'danger' as const },
  ];

  // Chip "Chăm sóc khách hàng" (bám hệ cũ): mua lần đầu/mua lại + N ngày chưa liên hệ (phân tầng loại trừ).
  const careChips = [
    { key: 'first', label: 'Mua lần đầu', count: fn?.care.firstTime, active: adv.purchaseBucket === 'first',
      patch: { purchaseBucket: adv.purchaseBucket === 'first' ? undefined : ('first' as const), notContactedBucket: undefined } },
    { key: 'repeat', label: 'Khách mua lại', count: fn?.care.repeat, active: adv.purchaseBucket === 'repeat',
      patch: { purchaseBucket: adv.purchaseBucket === 'repeat' ? undefined : ('repeat' as const), notContactedBucket: undefined } },
    { key: 'nc7', label: '7 ngày chưa liên hệ', count: fn?.care.notContacted7, active: adv.notContactedBucket === 'nc7',
      patch: { notContactedBucket: adv.notContactedBucket === 'nc7' ? undefined : ('nc7' as const), purchaseBucket: undefined } },
    { key: 'nc15', label: '15 ngày chưa liên hệ', count: fn?.care.notContacted15, active: adv.notContactedBucket === 'nc15',
      patch: { notContactedBucket: adv.notContactedBucket === 'nc15' ? undefined : ('nc15' as const), purchaseBucket: undefined } },
    { key: 'nc30', label: '30 ngày chưa liên hệ', count: fn?.care.notContacted30, active: adv.notContactedBucket === 'nc30',
      patch: { notContactedBucket: adv.notContactedBucket === 'nc30' ? undefined : ('nc30' as const), purchaseBucket: undefined } },
    { key: 'nc90', label: '90 ngày chưa liên hệ', count: fn?.care.notContacted90, active: adv.notContactedBucket === 'nc90',
      patch: { notContactedBucket: adv.notContactedBucket === 'nc90' ? undefined : ('nc90' as const), purchaseBucket: undefined } },
  ];

  return (
    <>
      <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 16 }}>
        <div>
          <h1 className="rf-page__title">Data khách hàng</h1>
          <div className="rf-page__sub">Quản lý toàn bộ hồ sơ khách hàng, phân nhóm và lịch chăm sóc.</div>
        </div>
        <div style={{ display: 'flex', gap: 10 }}>
          <ExportButton filename="khach-hang.csv" onExport={exportCsv} />
          {canCreate ? (
            <Button variant="primary" icon="add" onClick={() => setEditing({ mode: 'create', item: null })}>
              Thêm khách hàng
            </Button>
          ) : null}
        </div>
      </div>

      {/* Thẻ thống kê (icon-chip + số) — bộ handoff */}
      <div className="rf-grid" style={{ gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', marginTop: 16 }}>
        {statCards.map((s) => (
          <StatCardIcon key={s.label} icon={s.icon} tone={s.tone} value={s.value} label={s.label} />
        ))}
      </div>

      {/* Thanh lọc đầy đủ (bám "Xem thêm bộ lọc" hệ cũ) */}
      <div className="rf-card" style={{ padding: 12, marginTop: 16 }}>
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(190px, 1fr))', gap: 10 }}>
          <SearchInput value={search} onChange={setSearch} onEnter={applyFilters} placeholder="Nhập tên, SĐT, Email…" style={{ gridColumn: 'span 2' }} />
          <NumberInput value={draft.revenueFrom ?? null} min={0} placeholder="Doanh thu từ" onChange={(v) => setD({ revenueFrom: v ?? undefined })} />
          <NumberInput value={draft.revenueTo ?? null} min={0} placeholder="Doanh thu đến" onChange={(v) => setD({ revenueTo: v ?? undefined })} />
          <DateRangeInput from={draft.createdFrom} to={draft.createdTo} placeholder={['Thời gian tạo từ', 'đến']} onChange={(f, t) => setD({ createdFrom: f, createdTo: t })} />

          {moreOpen ? (
            <>
              <DateRangeInput from={draft.careFrom} to={draft.careTo} placeholder={['Thời gian chăm sóc từ', 'đến']} onChange={(f, t) => setD({ careFrom: f, careTo: t })} />
              <Select allowClear showSearch placeholder="Chi nhánh" options={strOpts(fo?.branches)} value={draft.branch} onChange={(v) => setD({ branch: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Nhóm" options={strOpts(fo?.groups)} value={draft.group} onChange={(v) => setD({ group: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Nguồn khách" options={strOpts(fo?.sources)} value={draft.source} onChange={(v) => setD({ source: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Người tạo" options={userOpts} value={draft.createdBy} onChange={(v) => setD({ createdBy: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Thị trường" options={strOpts(fo?.marketGroups)} value={draft.marketGroup} onChange={(v) => setD({ marketGroup: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Tỉnh thành" options={strOpts(fo?.cities)} value={draft.city} onChange={(v) => setD({ city: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Phòng ban" options={strOpts(fo?.departments)} value={draft.department} onChange={(v) => setD({ department: (v as string) ?? undefined })} />
              <Select allowClear placeholder="Giới tính" options={GENDER_OPTIONS} value={draft.gender} onChange={(v) => setD({ gender: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Tên CTV" options={strOpts(fo?.collaborators)} value={draft.collaborator} onChange={(v) => setD({ collaborator: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Tag" options={strOpts(fo?.tags)} value={draft.tag} onChange={(v) => setD({ tag: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="Chiến dịch" options={strOpts(fo?.campaigns)} value={draft.campaign} onChange={(v) => setD({ campaign: (v as string) ?? undefined })} />
              <Select allowClear showSearch placeholder="NV phụ trách" options={userOpts} value={draft.assignedTo} onChange={(v) => setD({ assignedTo: (v as string) ?? undefined })} />
              <Select allowClear placeholder="Sinh nhật (tháng)" options={MONTH_OPTIONS} value={draft.birthdayMonth} onChange={(v) => setD({ birthdayMonth: (v as number) ?? undefined })} />
            </>
          ) : null}
        </div>
        <div style={{ display: 'flex', gap: 8, marginTop: 12 }}>
          <Button variant="primary" icon="search" onClick={applyFilters}>
            Tìm kiếm
          </Button>
          <Button onClick={resetFilters}>Đặt lại</Button>
          <Button variant="text" icon={moreOpen ? 'expand_less' : 'tune'} onClick={() => setMoreOpen((o) => !o)}>
            {moreOpen ? 'Thu gọn bộ lọc' : 'Xem thêm bộ lọc'}
          </Button>
        </div>
      </div>

      {/* Tabs loại khách hàng (bám staging: Tất cả · Cá nhân · Doanh nghiệp · Đối tác · CTV) */}
      <div style={{ marginTop: 14, overflowX: 'auto' }}>
        <SegmentTabs
          value={typeFilter === undefined ? 'all' : String(typeFilter)}
          onChange={(val) => {
            setTypeFilter(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: 'Tất cả', value: 'all' }, ...CUSTOMER_TYPE_OPTIONS.map((o) => ({ label: o.label, value: String(o.value) }))]}
        />
      </div>

      {/* Phễu khách hàng + Chăm sóc khách hàng (chip lọc nhanh, bám hệ cũ) */}
      <div className="rf-card" style={{ padding: 14, marginTop: 14, display: 'flex', flexDirection: 'column', gap: 12 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
          <span className="rf-section" style={{ margin: 0, minWidth: 128 }}>
            Phễu khách hàng
          </span>
          <FilterChip active={adv.segment === undefined} onClick={() => pickChip({ segment: undefined })} count={fn?.total ?? 0}>
            Tất cả
          </FilterChip>
          {(fn?.segments ?? []).map((s) => (
            <FilterChip key={s.name} active={adv.segment === s.name} count={s.count} onClick={() => pickChip({ segment: adv.segment === s.name ? undefined : s.name })}>
              {s.name}
            </FilterChip>
          ))}
        </div>
        <div style={{ display: 'flex', alignItems: 'center', gap: 8, flexWrap: 'wrap' }}>
          <span className="rf-section" style={{ margin: 0, minWidth: 128 }}>
            Chăm sóc khách hàng
          </span>
          {careChips.map((c) => (
            <FilterChip key={c.key} active={c.active} count={c.count ?? 0} onClick={() => pickChip(c.patch)}>
              {c.label}
            </FilterChip>
          ))}
        </div>
      </div>

      <div style={{ marginTop: 14 }}>
        <DataCard title="Danh sách khách hàng" bodyless>
          <Table columns={columns} data={rows} rowKey={(c) => c.id} loading={list.isLoading} minWidth={1080} summary={rows.length ? summary : undefined} empty="Không có khách hàng" />
          <div style={{ padding: '10px 16px' }}>
            <Pagination page={page.page} pageSize={page.size} total={list.data?.total ?? 0} unit="khách hàng" onChange={(p) => setPage({ ...page, page: p })} />
          </div>
        </DataCard>
      </div>

      {editing ? (
        <CrudDrawer
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
          <SelectField name="segments" label="Phân nhóm" options={strOpts(fo?.segments)} mode="tags" />
          <SelectField name="tags" label="Thẻ KH" options={strOpts(fo?.tags)} mode="tags" />
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
        </CrudDrawer>
      ) : null}
    </>
  );
}
