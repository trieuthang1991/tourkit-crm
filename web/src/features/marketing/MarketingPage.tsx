import { App, Button, Card, Col, Drawer, Input, Popconfirm, Row, Segmented, Select, Space, Table, Tag, Typography } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { dateText, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { useAuth } from '../auth/AuthContext';
import { marketingCrud } from './marketingCrud';
import { useCampaigns, useCampaignStats } from './marketingApi';
import type { CampaignFilter } from './marketingApi';
import { SendCampaignModal } from './SendCampaignModal';
import { CAMPAIGN_STATUS, CHANNEL, campaignCreateSchema, campaignLogSchema, campaignUpdateSchema } from './types';
import type { Campaign, CampaignForm } from './types';
import { CellStack } from '../../shared/ui/TableCells';
import { DataCard } from '../../shared/ui';
import { StatGrid } from '../../ui/kit';

const CHANNEL_OPTIONS = Object.entries(CHANNEL).map(([value, label]) => ({ value: Number(value), label }));
const STATUS_OPTIONS = Object.entries(CAMPAIGN_STATUS).map(([value, label]) => ({ value: Number(value), label }));
const CHANNEL_COLOR: Record<number, string> = { 1: 'blue', 2: 'gold', 3: 'cyan' };
const STATUS_COLOR: Record<number, string> = { 0: 'default', 1: 'green' };

function CampaignLogsDrawer({ campaignId, open, onClose }: { campaignId: string; open: boolean; onClose: () => void }) {
  const logs = useQuery({
    queryKey: ['campaigns', campaignId, 'logs'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/marketing/campaigns/${campaignId}/logs`);
      return z.array(campaignLogSchema).parse(data);
    },
    enabled: open,
  });

  return (
    <Drawer title="Nhật ký gửi" open={open} onClose={onClose} width={480} destroyOnHidden>
      <Table
        rowKey="id"
        size="small"
        loading={logs.isLoading}
        dataSource={logs.data ?? []}
        pagination={false}
        columns={[
          { title: 'Người nhận', dataIndex: 'recipient', key: 'recipient' },
          { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => <Tag color={v === 1 ? 'green' : 'red'}>{v === 1 ? 'Thành công' : 'Lỗi'}</Tag> },
          { title: 'Thời gian', dataIndex: 'sentAt', key: 'sentAt', render: (v: string) => dateText(v) },
        ]}
      />
    </Drawer>
  );
}

export function MarketingPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const qc = useQueryClient();
  const canCreate = has('marketing.create');
  const canUpdate = has('marketing.update');
  const canSend = has('marketing.send');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [channel, setChannel] = useState<number | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [applied, setApplied] = useState<CampaignFilter>({});
  const applyFilters = () => {
    setApplied({ q: search || undefined, channel });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setChannel(undefined);
    setStatus(undefined);
    setApplied({});
    setPage(1);
  };

  const list = useCampaigns(page, size, { ...applied, status });
  const stats = useCampaignStats();

  const [creating, setCreating] = useState(false);
  const [editing, setEditing] = useState<Campaign | null>(null);
  const [sendId, setSendId] = useState<string | null>(null);
  const [logId, setLogId] = useState<string | null>(null);

  const create = useMutation({
    mutationFn: async (body: CampaignForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/marketing/campaigns', body);
      return data;
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['campaigns'] }),
  });
  const update = useMutation({
    mutationFn: async ({ id, body }: { id: string; body: CampaignForm }) => {
      await httpClient.put(`/api/v1/marketing/campaigns/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['campaigns'] }),
  });
  const remove = marketingCrud.useRemove();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onSubmit(values: CampaignForm) {
    await run(async () => {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: values });
        setEditing(null);
      } else {
        await create.mutateAsync(values);
        setCreating(false);
      }
    }, editing ? 'Đã cập nhật' : 'Đã thêm chiến dịch');
  }

  const columns: ColumnsType<Campaign> = [
    {
      title: 'Chiến dịch',
      key: 'campaign',
      render: (_: unknown, c: Campaign) => (
        <CellStack main={c.name} sub={<Tag color={CHANNEL_COLOR[c.channel]}>{statusText(CHANNEL, c.channel)}</Tag>} />
      ),
    },
    {
      title: 'Nội dung',
      key: 'content',
      render: (_: unknown, c: Campaign) => (
        <CellStack
          main={c.subject ?? '—'}
          sub={c.body ? (c.body.length > 80 ? `${c.body.slice(0, 80)}…` : c.body) : undefined}
          title={c.body || undefined}
        />
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (v: number) => <Tag color={STATUS_COLOR[v] ?? 'default'}>{statusText(CAMPAIGN_STATUS, v)}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 260,
      render: (_: unknown, c: Campaign) => (
        <Space>
          {canSend ? <Button size="small" onClick={() => setSendId(c.id)}>Gửi</Button> : null}
          <Button size="small" onClick={() => setLogId(c.id)}>Log</Button>
          {canUpdate ? <Button size="small" onClick={() => setEditing(c)}>Sửa</Button> : null}
          {canCreate ? (
            <Popconfirm title="Xoá chiến dịch này?" onConfirm={() => run(() => remove.mutateAsync(c.id), 'Đã xoá')}>
              <Button size="small" danger>Xoá</Button>
            </Popconfirm>
          ) : null}
        </Space>
      ),
    },
  ];

  const defaultValues: CampaignForm = editing
    ? { name: editing.name, channel: editing.channel, subject: editing.subject, body: editing.body, status: editing.status }
    : { name: '', channel: 1, subject: null, body: '', status: 0 };

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Marketing
        </Typography.Title>
        {canCreate ? (
          <Button type="primary" onClick={() => setCreating(true)}>
            Thêm chiến dịch
          </Button>
        ) : null}
      </div>

      <StatGrid
        style={{ marginBottom: 16 }}
        items={[
          { label: 'Tổng chiến dịch', value: stats.data?.total ?? 0 },
          { label: 'Nháp', value: stats.data?.draft ?? 0 },
          { label: 'Đã gửi', value: stats.data?.sent ?? 0 },
          { label: 'Tin đã gửi', value: stats.data?.messages ?? 0 },
        ]}
      />

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Tên chiến dịch" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select allowClear style={{ width: '100%' }} placeholder="Kênh"
              options={CHANNEL_OPTIONS} value={channel} onChange={(v) => setChannel(v ?? undefined)} />
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
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...STATUS_OPTIONS.map((o) => ({ label: o.label, value: String(o.value) }))]}
        />
      </div>

      <DataCard title="Danh sách chiến dịch">
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
          title={editing ? 'Sửa chiến dịch' : 'Thêm chiến dịch'}
          schema={editing ? campaignUpdateSchema : campaignCreateSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => { setCreating(false); setEditing(null); }}
          onSubmit={onSubmit}
        >
          <TextField name="name" label="Tên" required />
          <SelectField name="channel" label="Kênh" options={CHANNEL_OPTIONS} required />
          <TextField name="subject" label="Tiêu đề" />
          <TextAreaField name="body" label="Nội dung" required />
          {editing ? <SelectField name="status" label="Trạng thái" options={STATUS_OPTIONS} required /> : null}
        </CrudFormModal>
      ) : null}

      {sendId ? <SendCampaignModal campaignId={sendId} open onClose={() => setSendId(null)} /> : null}
      {logId ? <CampaignLogsDrawer campaignId={logId} open onClose={() => setLogId(null)} /> : null}
    </>
  );
}
