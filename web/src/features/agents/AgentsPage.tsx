import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Space, Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { StatGrid } from '../../ui/kit';
import { DataCard } from '../../shared/ui';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { money } from '../../shared/format';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import { CellEntity, CellMoney, CellStack, CellText } from '../../shared/ui/TableCells';
import { agentsCrud } from './agentsCrud';
import { agentFormSchema, agentSchema } from './types';
import type { Agent, AgentForm } from './types';

const statsSchema = z.object({ total: z.number(), active: z.number(), inactive: z.number(), totalCreditLimit: z.number() });

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function AgentsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('agent.manage');
  const create = agentsCrud.useCreate();
  const update = agentsCrud.useUpdate();
  const remove = agentsCrud.useRemove();

  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [status, setStatus] = useState<number | undefined>();
  const [editing, setEditing] = useState<Agent | 'new' | null>(null);

  const applyFilters = () => {
    setQ(search);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setStatus(undefined);
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['agents', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agents/stats');
      return statsSchema.parse(data);
    },
  });

  const list = useQuery({
    queryKey: ['agents', 'list', page.page, page.size, q, status],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agents', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, status }),
      });
      return pagedSchema(agentSchema).parse(data);
    },
  });

  async function submit(values: AgentForm) {
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

  const columns: ColumnsType<Agent> = [
    {
      title: 'Đại lý',
      key: 'agent',
      width: 250,
      render: (_: unknown, r: Agent) => <CellEntity name={r.name} code={r.code} />,
    },
    {
      title: 'Liên hệ',
      key: 'contact',
      width: 240,
      render: (_: unknown, r: Agent) => (
        <CellStack
          main={r.contactPerson ?? '—'}
          sub={
            r.phone || r.email ? (
              <>
                {r.phone}
                {r.phone && r.email ? ' · ' : ''}
                {r.email}
              </>
            ) : undefined
          }
        />
      ),
    },
    {
      title: 'MST',
      key: 'taxCode',
      width: 130,
      render: (_: unknown, r: Agent) => <CellText mono>{r.taxCode}</CellText>,
    },
    {
      title: 'Địa chỉ',
      key: 'address',
      width: 260,
      render: (_: unknown, r: Agent) => <CellText title={r.address ?? undefined}>{r.address}</CellText>,
    },
    {
      title: 'Hạn mức',
      key: 'creditLimit',
      width: 150,
      align: 'right',
      render: (_: unknown, r: Agent) => <CellMoney value={r.creditLimit} />,
    },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 120, render: (v: number) => <Tag color={v === 1 ? 'green' : 'default'}>{v === 1 ? 'Hoạt động' : 'Ngừng'}</Tag> },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 130,
            fixed: 'right' as const,
            render: (_: unknown, r: Agent) => (
              <Space>
                <Button size="small" onClick={() => setEditing(r)}>Sửa</Button>
                <Popconfirm title="Xoá đại lý này?" onConfirm={() => onDelete(r.id)}>
                  <Button size="small" danger>Xoá</Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<Agent>[number],
        ]
      : []),
  ];

  const s = stats.data;

  const isEdit = editing && editing !== 'new';
  const defaultValues: AgentForm = isEdit
    ? { code: editing.code, name: editing.name, contactPerson: editing.contactPerson, phone: editing.phone, email: editing.email, taxCode: editing.taxCode, address: editing.address, creditLimit: editing.creditLimit, status: editing.status }
    : { code: '', name: '', contactPerson: null, phone: null, email: null, taxCode: null, address: null, creditLimit: 0, status: 1 };

  return (
    <>
      <PageHeader
        title="Danh sách đại lý (B2B)"
        extra={canManage ? <Button type="primary" onClick={() => setEditing('new')}>Thêm đại lý</Button> : undefined}
      />

      <StatGrid
        items={[
          { label: 'Tổng đại lý', value: s?.total ?? 0 },
          { label: 'Đang hoạt động', value: s?.active ?? 0 },
          { label: 'Ngừng', value: s?.inactive ?? 0 },
          { label: 'Tổng hạn mức', value: money(s?.totalCreditLimit ?? 0) },
        ]}
        style={{ marginBottom: 16 }}
      />

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={16} lg={10}>
            <Input.Search allowClear placeholder="Mã / tên / liên hệ / SĐT" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
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
          options={[{ label: `Tất cả (${s?.total ?? 0})`, value: 'all' }, { label: `Hoạt động (${s?.active ?? 0})`, value: '1' }, { label: `Ngừng (${s?.inactive ?? 0})`, value: '0' }]}
        />
      </div>

      <DataCard title="Danh sách đại lý">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          scroll={{ x: 1080 }}
          pagination={{
            current: page.page,
            pageSize: page.size,
            total: list.data?.total ?? 0,
            showSizeChanger: true,
            onChange: (p, sz) => setPage({ page: p, size: sz }),
          }}
        />
      </DataCard>

      {editing && (
        <CrudFormModal
          open
          title={isEdit ? 'Sửa đại lý' : 'Thêm đại lý'}
          schema={agentFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          <TextField name="code" label="Mã" required />
          <TextField name="name" label="Tên đại lý" required />
          <TextField name="contactPerson" label="Người liên hệ" />
          <TextField name="phone" label="Điện thoại" />
          <TextField name="email" label="Email" />
          <TextField name="taxCode" label="Mã số thuế" />
          <TextField name="address" label="Địa chỉ" />
          <NumberField name="creditLimit" label="Hạn mức tín dụng" required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    </>
  );
}
