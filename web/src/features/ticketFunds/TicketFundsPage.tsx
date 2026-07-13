import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { DEFAULT_PAGE, pagedSchema } from '../../shared/api/paged';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import { providersCrud } from '../providers/providersCrud';
import { ticketFundsCrud } from './ticketFundsCrud';
import { ticketFundFormSchema, ticketFundSchema } from './types';
import type { TicketFund, TicketFundForm } from './types';

const statsSchema = z.object({ total: z.number(), closed: z.number(), open: z.number() });

function clean(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function TicketFundsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('ticketfund.manage');
  const create = ticketFundsCrud.useCreate();
  const update = ticketFundsCrud.useUpdate();
  const remove = ticketFundsCrud.useRemove();

  const [page, setPage] = useState(DEFAULT_PAGE);
  const [search, setSearch] = useState('');
  const [q, setQ] = useState('');
  const [providerId, setProviderId] = useState<string | undefined>();
  const [closed, setClosed] = useState<boolean | undefined>();
  const [advProvider, setAdvProvider] = useState<string | undefined>();
  const [editing, setEditing] = useState<TicketFund | 'new' | null>(null);

  const applyFilters = () => {
    setQ(search);
    setAdvProvider(providerId);
    setPage({ ...page, page: 1 });
  };
  const resetFilters = () => {
    setSearch('');
    setQ('');
    setProviderId(undefined);
    setClosed(undefined);
    setAdvProvider(undefined);
    setPage({ ...page, page: 1 });
  };

  const stats = useQuery({
    queryKey: ['ticket-funds', 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/ticket-funds/stats');
      return statsSchema.parse(data);
    },
  });
  const providers = providersCrud.useList({ page: 1, size: 500 });
  const providerOpts = (providers.data?.items ?? []).map((p) => ({ label: `${p.name} (${p.code})`, value: p.id }));

  const list = useQuery({
    queryKey: ['ticket-funds', 'list', page.page, page.size, q, advProvider, closed],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/ticket-funds', {
        params: clean({ page: page.page, size: page.size, q: q || undefined, providerId: advProvider, isClosed: closed }),
      });
      return pagedSchema(ticketFundSchema).parse(data);
    },
  });

  async function submit(values: TicketFundForm) {
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

  const columns: ColumnsType<TicketFund> = [
    { title: 'Mã đơn', dataIndex: 'orderCode', key: 'orderCode', width: 140, render: (v: string | null) => v ?? '—' },
    { title: 'Mã vé', dataIndex: 'ticketCode', key: 'ticketCode', width: 160 },
    { title: 'Nhà cung cấp', dataIndex: 'providerName', key: 'providerName', width: 180, render: (v: string | null) => v ?? '—' },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 100, align: 'center', render: (v: number) => <Tag>{v}</Tag> },
    { title: 'Đóng quỹ', dataIndex: 'isClosed', key: 'isClosed', width: 120, render: (v: boolean) => <Tag color={v ? 'green' : 'orange'}>{v ? 'Đã đóng' : 'Chưa đóng'}</Tag> },
    ...(canManage
      ? [
          {
            title: '',
            key: '__actions',
            width: 130,
            fixed: 'right' as const,
            render: (_: unknown, r: TicketFund) => (
              <Space>
                <Button size="small" onClick={() => setEditing(r)}>Sửa</Button>
                <Popconfirm title="Xoá quỹ vé này?" onConfirm={() => onDelete(r.id)}>
                  <Button size="small" danger>Xoá</Button>
                </Popconfirm>
              </Space>
            ),
          } as ColumnsType<TicketFund>[number],
        ]
      : []),
  ];

  const s = stats.data;
  const statCards = [
    { title: 'Tổng quỹ vé', value: s?.total ?? 0 },
    { title: 'Đã đóng', value: s?.closed ?? 0 },
    { title: 'Chưa đóng', value: s?.open ?? 0 },
  ];

  const isEdit = editing && editing !== 'new';
  const defaultValues: TicketFundForm = isEdit
    ? { orderId: editing.orderId, providerId: editing.providerId, providerServiceId: editing.providerServiceId, ticketCode: editing.ticketCode, status: editing.status, isClosed: editing.isClosed }
    : { orderId: '', providerId: null, providerServiceId: null, ticketCode: null, status: 0, isClosed: false };

  return (
    <>
      <PageHeader
        title="Quỹ vé ứng"
        extra={canManage ? <Button type="primary" onClick={() => setEditing('new')}>Thêm quỹ vé</Button> : undefined}
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

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search allowClear placeholder="Mã vé" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhà cung cấp"
              options={providerOpts} value={providerId} onChange={(v) => setProviderId(v ?? undefined)} />
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
          options={[{ label: `Tất cả (${s?.total ?? 0})`, value: 'all' }, { label: `Chưa đóng (${s?.open ?? 0})`, value: 'open' }, { label: `Đã đóng (${s?.closed ?? 0})`, value: 'closed' }]}
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

      {editing && (
        <CrudFormModal
          open
          title={isEdit ? 'Sửa quỹ vé' : 'Thêm quỹ vé'}
          schema={ticketFundFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditing(null)}
          onSubmit={submit}
        >
          <TextField name="orderId" label="Mã đơn (orderId)" required />
          <TextField name="providerId" label="Mã NCC (providerId)" />
          <TextField name="providerServiceId" label="Mã giá dịch vụ (providerServiceId)" />
          <TextField name="ticketCode" label="Mã vé" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    </>
  );
}
