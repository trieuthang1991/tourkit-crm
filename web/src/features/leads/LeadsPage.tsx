import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Space, Statistic, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { SelectField, TextField } from '../../shared/ui/Field';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { leadsCrud } from './leadsCrud';
import { LEAD_STATUS, leadCreateSchema, leadSchema, leadUpdateSchema } from './types';
import type { Lead, LeadForm } from './types';

const LEAD_STATUS_OPTIONS = Object.entries(LEAD_STATUS).map(([value, label]) => ({ value: Number(value), label }));
const dash = (v: string | null | undefined) => (v ? v : '—');
const statsSchema = z.object({
  total: z.number(),
  new: z.number(),
  contacted: z.number(),
  qualified: z.number(),
  won: z.number(),
  lost: z.number(),
  converted: z.number(),
});

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function LeadsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const qc = useQueryClient();
  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [status, setStatus] = useState<number | undefined>();
  const [editing, setEditing] = useState<{ mode: 'create' | 'edit'; item: Lead | null } | null>(null);

  const canCreate = has('lead.create');
  const canUpdate = has('lead.update');
  const canRemove = has('lead.delete');
  const canConvert = has('lead.convert');

  const stats = useQuery({
    queryKey: ['leads', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/leads/stats');
      return statsSchema.parse(data);
    },
  });

  const list = useQuery({
    queryKey: ['leads', 'list', page.page, page.size, q, status],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/leads', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, status }),
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

  const columns: ColumnsType<Lead> = [
    {
      title: 'STT',
      key: '__stt',
      width: 60,
      fixed: 'left',
      align: 'center',
      render: (_: unknown, __: Lead, index: number) => (page.page - 1) * page.size + index + 1,
    },
    { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName', fixed: 'left', width: 180 },
    { title: 'Điện thoại', dataIndex: 'phone', key: 'phone', width: 130, render: dash },
    { title: 'Email', dataIndex: 'email', key: 'email', width: 180, render: dash },
    { title: 'Nguồn', dataIndex: 'source', key: 'source', width: 150, render: dash },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 130, render: (s: number) => statusText(LEAD_STATUS, s) },
    {
      title: '',
      key: '__actions',
      width: 260,
      fixed: 'right',
      render: (_: unknown, lead: Lead) => (
        <Space>
          {canConvert && lead.convertedCustomerId === null ? (
            <Button
              size="small"
              loading={convert.isPending}
              onClick={async () => {
                try {
                  await convert.mutateAsync(lead.id);
                  message.success('Đã chuyển thành khách hàng');
                } catch (e) {
                  message.error(errorMessage(e));
                }
              }}
            >
              Chuyển thành KH
            </Button>
          ) : null}
          {canUpdate ? (
            <Button size="small" onClick={() => setEditing({ mode: 'edit', item: lead })}>
              Sửa
            </Button>
          ) : null}
          {canRemove ? (
            <Popconfirm
              title="Xoá lead này?"
              onConfirm={async () => {
                try {
                  await remove.mutateAsync(lead.id);
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
    },
  ];

  const item = editing?.item ?? null;
  const defaultValues: LeadForm = {
    fullName: item?.fullName ?? '',
    phone: item?.phone ?? null,
    email: item?.email ?? null,
    source: item?.source ?? null,
    assignedToUserId: item?.assignedToUserId ?? null,
    status: item?.status ?? 1,
  };

  const s = stats.data;
  const statCards = [
    { title: 'Tổng số lead', value: s?.total ?? 0 },
    { title: 'Mới', value: s?.new ?? 0 },
    { title: 'Tiềm năng', value: s?.qualified ?? 0 },
    { title: 'Chốt', value: s?.won ?? 0 },
    { title: 'Mất', value: s?.lost ?? 0 },
    { title: 'Đã chuyển KH', value: s?.converted ?? 0 },
  ];

  return (
    <>
      <PageHeader
        title="Cơ hội bán hàng (Lead)"
        extra={
          canCreate ? (
            <Button type="primary" onClick={() => setEditing({ mode: 'create', item: null })}>
              Thêm mới
            </Button>
          ) : null
        }
      />

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {statCards.map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      <Space wrap style={{ marginBottom: 12 }}>
        <Input.Search
          allowClear
          placeholder="Tìm theo tên / SĐT / email / nguồn"
          style={{ width: 320 }}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          onSearch={(v) => {
            setQ(v);
            setPage({ ...page, page: 1 });
          }}
        />
      </Space>

      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => {
            setStatus(val === 'all' ? undefined : Number(val));
            setPage({ ...page, page: 1 });
          }}
          options={[{ label: 'Tất cả', value: 'all' }, ...LEAD_STATUS_OPTIONS.map((o) => ({ label: o.label, value: String(o.value) }))]}
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
