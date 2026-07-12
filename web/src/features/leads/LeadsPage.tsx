import {
  App, Button, Card, Col, DatePicker, Input, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography,
} from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import dayjs from 'dayjs';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { pagedSchema } from '../../shared/api/paged';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { SelectField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { leadsCrud } from './leadsCrud';
import { LEAD_STATUS, leadCreateSchema, leadSchema, leadUpdateSchema } from './types';
import type { Lead, LeadForm } from './types';

const LEAD_STATUS_OPTIONS = Object.entries(LEAD_STATUS).map(([value, label]) => ({ value: Number(value), label }));
const STATUS_COLOR: Record<number, string> = { 1: 'blue', 2: 'gold', 3: 'cyan', 4: 'green', 5: 'red' };
const dash = (v: string | null | undefined) => (v ? v : '—');

const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });
const statsSchema = z.object({
  total: z.number(), new: z.number(), contacted: z.number(), qualified: z.number(),
  won: z.number(), lost: z.number(), converted: z.number(),
});
const filterOptionsSchema = z.object({ sources: z.array(z.string()) });

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

type Filters = {
  status?: number;
  source?: string;
  assignedToUserId?: string;
  createdFrom?: string;
  createdTo?: string;
};

export function LeadsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const qc = useQueryClient();
  const [view, setView] = useState<'kanban' | 'list'>('kanban');
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [draft, setDraft] = useState<Filters>({});
  const [applied, setApplied] = useState<Filters>({});
  const [editing, setEditing] = useState<{ mode: 'create' | 'edit'; item: Lead | null } | null>(null);

  const canCreate = has('lead.create');
  const canUpdate = has('lead.update');
  const canRemove = has('lead.delete');
  const canConvert = has('lead.convert');

  const setD = (patch: Partial<Filters>) => setDraft((d) => ({ ...d, ...patch }));
  const applyFilters = () => {
    setQ(search);
    setApplied(draft);
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setDraft({});
    setApplied({});
  };

  const stats = useQuery({
    queryKey: ['leads', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/leads/stats');
      return statsSchema.parse(data);
    },
  });
  const filterOptions = useQuery({
    queryKey: ['leads', 'filter-options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/leads/filter-options');
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

  // Nạp toàn bộ lead theo bộ lọc (size lớn) → dùng cho cả Kanban (gom theo giai đoạn) và List.
  const list = useQuery({
    queryKey: ['leads', 'board', q, applied],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/leads', {
        params: clean({ page: 1, size: 500, q: q || undefined, ...applied }),
      });
      return pagedSchema(leadSchema).parse(data);
    },
  });

  const create = leadsCrud.useCreate();
  const update = leadsCrud.useUpdate();
  const remove = leadsCrud.useRemove();
  const convert = useMutation({
    mutationFn: async (id: string) => {
      await httpClient.post(`/api/v1/leads/${id}/convert`);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['leads'] });
      qc.invalidateQueries({ queryKey: ['customers'] });
    },
  });
  // Chuyển giai đoạn (kéo cột) — gửi PUT với dữ liệu lead hiện tại + status mới.
  const moveStage = useMutation({
    mutationFn: async ({ lead, status }: { lead: Lead; status: number }) => {
      await httpClient.put(`/api/v1/leads/${lead.id}`, {
        fullName: lead.fullName, phone: lead.phone, email: lead.email, source: lead.source,
        assignedToUserId: lead.assignedToUserId, status,
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['leads'] }),
  });

  const sourceOpts = (filterOptions.data?.sources ?? []).map((s) => ({ label: s, value: s }));
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));

  async function submit(values: LeadForm) {
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

  async function doConvert(lead: Lead) {
    try {
      await convert.mutateAsync(lead.id);
      message.success('Đã chuyển thành khách hàng');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const leads = list.data?.items ?? [];
  const s = stats.data;
  const statCards = [
    { title: 'Tổng cơ hội', value: s?.total ?? 0 },
    { title: 'Mới', value: s?.new ?? 0 },
    { title: 'Đã liên hệ', value: s?.contacted ?? 0 },
    { title: 'Tiềm năng', value: s?.qualified ?? 0 },
    { title: 'Chốt', value: s?.won ?? 0 },
    { title: 'Mất', value: s?.lost ?? 0 },
    { title: 'Đã chuyển KH', value: s?.converted ?? 0 },
  ];

  const columns: ColumnsType<Lead> = [
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName', width: 180 },
    { title: 'Điện thoại', dataIndex: 'phone', key: 'phone', width: 130, render: dash },
    { title: 'Email', dataIndex: 'email', key: 'email', width: 180, render: dash },
    { title: 'Nguồn', dataIndex: 'source', key: 'source', width: 150, render: dash },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 130, render: (v: number) => <Tag color={STATUS_COLOR[v]}>{statusText(LEAD_STATUS, v)}</Tag> },
    {
      title: '',
      key: '__actions',
      width: 260,
      render: (_: unknown, lead: Lead) => (
        <Space>
          {canConvert && lead.convertedCustomerId === null ? (
            <Button size="small" onClick={() => doConvert(lead)}>Chuyển thành KH</Button>
          ) : null}
          {canUpdate ? <Button size="small" onClick={() => setEditing({ mode: 'edit', item: lead })}>Sửa</Button> : null}
          {canRemove ? (
            <Popconfirm title="Xoá lead này?" onConfirm={async () => {
              try { await remove.mutateAsync(lead.id); message.success('Đã xoá'); } catch (e) { message.error(errorMessage(e)); }
            }}>
              <Button size="small" danger>Xoá</Button>
            </Popconfirm>
          ) : null}
        </Space>
      ),
    },
  ];

  const item = editing?.item ?? null;
  const defaultValues: LeadForm = {
    fullName: item?.fullName ?? '', phone: item?.phone ?? null, email: item?.email ?? null,
    source: item?.source ?? null, assignedToUserId: item?.assignedToUserId ?? null, status: item?.status ?? 1,
  };

  return (
    <>
      <PageHeader
        title="Cơ hội bán hàng (Lead)"
        extra={
          <Space>
            <Segmented
              value={view}
              onChange={(v) => setView(v as 'kanban' | 'list')}
              options={[{ label: 'Kanban', value: 'kanban' }, { label: 'List', value: 'list' }]}
            />
            {canCreate ? <Button type="primary" onClick={() => setEditing({ mode: 'create', item: null })}>Thêm mới</Button> : null}
          </Space>
        }
      />

      {/* Thẻ pipeline */}
      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {statCards.map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={3} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      {/* Thanh lọc (bám staging: 9 ô) */}
      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Tên dự án / tên KH / SĐT" value={search}
              onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="Trạng thái" options={LEAD_STATUS_OPTIONS}
              value={draft.status} onChange={(v) => setD({ status: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nguồn khách hàng"
              options={sourceOpts} value={draft.source} onChange={(v) => setD({ source: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="NV phụ trách"
              options={userOpts} value={draft.assignedToUserId} onChange={(v) => setD({ assignedToUserId: v ?? undefined })} />
          </Col>
          <Col xs={12} sm={8} lg={3}>
            <Select allowClear style={{ width: '100%' }} placeholder="Người tạo" options={userOpts} disabled />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Ngày lập từ', 'đến']}
              value={draft.createdFrom && draft.createdTo ? [dayjs(draft.createdFrom), dayjs(draft.createdTo)] : null}
              onChange={(d) => setD({ createdFrom: d?.[0]?.startOf('day').toISOString(), createdTo: d?.[1]?.endOf('day').toISOString() })} />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Ngày tạo đơn', '']} disabled />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="Chi nhánh" options={[]} disabled />
          </Col>
          <Col xs={12} sm={8} lg={4}>
            <Select allowClear style={{ width: '100%' }} placeholder="Thẻ tag" options={[]} disabled />
          </Col>
          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>Tìm kiếm</Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
              <Typography.Text type="secondary" style={{ fontSize: 12 }}>
                (Người tạo / Ngày tạo đơn / Chi nhánh / Thẻ tag: chờ bổ sung model)
              </Typography.Text>
            </Space>
          </Col>
        </Row>
      </Card>

      {view === 'kanban' ? (
        <div style={{ display: 'flex', gap: 12, overflowX: 'auto', paddingBottom: 8, alignItems: 'flex-start' }}>
          {LEAD_STATUS_OPTIONS.map((col) => {
            const cards = leads.filter((l) => l.status === col.value);
            return (
              <Card
                key={col.value}
                size="small"
                style={{ minWidth: 280, maxWidth: 280, flex: '0 0 auto' }}
                title={<Space><Tag color={STATUS_COLOR[col.value]}>{col.label}</Tag><span>{cards.length}</span></Space>}
                loading={list.isLoading}
              >
                <Space direction="vertical" style={{ width: '100%' }} size={8}>
                  {cards.map((lead) => (
                    <Card key={lead.id} size="small" styles={{ body: { padding: 8 } }}>
                      <Typography.Text strong>{lead.fullName}</Typography.Text>
                      <div style={{ fontSize: 12, color: '#888' }}>{dash(lead.phone)}{lead.source ? ` · ${lead.source}` : ''}</div>
                      <Space style={{ marginTop: 6 }} wrap>
                        {canUpdate ? (
                          <Select
                            size="small"
                            style={{ width: 120 }}
                            value={lead.status}
                            options={LEAD_STATUS_OPTIONS}
                            onChange={(status) => { if (status !== lead.status) moveStage.mutate({ lead, status }); }}
                          />
                        ) : null}
                        {canConvert && lead.convertedCustomerId === null ? (
                          <Button size="small" onClick={() => doConvert(lead)}>Tạo đơn</Button>
                        ) : null}
                      </Space>
                    </Card>
                  ))}
                  {cards.length === 0 && !list.isLoading ? <Typography.Text type="secondary">—</Typography.Text> : null}
                </Space>
              </Card>
            );
          })}
        </div>
      ) : (
        <Table
          rowKey="id"
          columns={columns}
          dataSource={leads}
          loading={list.isLoading}
          scroll={{ x: 'max-content' }}
          pagination={{ pageSize: 20, showSizeChanger: true }}
        />
      )}

      {editing ? (
        <CrudFormModal
          open
          title={editing.mode === 'edit' ? 'Sửa lead' : 'Thêm lead'}
          schema={editing.mode === 'edit' ? leadUpdateSchema : leadCreateSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          <TextField name="fullName" label="Họ tên" required />
          <TextField name="phone" label="Điện thoại" />
          <TextField name="email" label="Email" />
          <TextField name="source" label="Nguồn" />
          <TextField name="assignedToUserId" label="Người phụ trách (userId)" />
          {editing.mode === 'edit' ? (
            <SelectField name="status" label="Trạng thái" options={LEAD_STATUS_OPTIONS} required />
          ) : null}
        </CrudFormModal>
      ) : null}
    </>
  );
}
