import { Button, Drawer, Space, Table } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { useState } from 'react';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { dateText, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { useAuth } from '../auth/AuthContext';
import { marketingCrud } from './marketingCrud';
import { SendCampaignModal } from './SendCampaignModal';
import { CHANNEL, campaignCreateSchema, campaignLogSchema, campaignUpdateSchema } from './types';
import type { Campaign, CampaignForm } from './types';

const CHANNEL_OPTIONS = Object.entries(CHANNEL).map(([value, label]) => ({ value: Number(value), label }));

// Marketing campaigns have no delete endpoint — omit useRemove so ResourcePage hides the column.
// eslint-disable-next-line @typescript-eslint/no-unused-vars -- useRemove intentionally discarded via destructuring.
const { useRemove: _omit, ...noDeleteCrud } = marketingCrud;

function CampaignLogsDrawer({
  campaignId,
  open,
  onClose,
}: {
  campaignId: string;
  open: boolean;
  onClose: () => void;
}) {
  const logs = useQuery({
    queryKey: ['campaigns', campaignId, 'logs'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/marketing/campaigns/${campaignId}/logs`);
      return z.array(campaignLogSchema).parse(data);
    },
    enabled: open,
  });

  return (
    <Drawer title="Nhật ký gửi" open={open} onClose={onClose} width={480} destroyOnClose>
      <Table
        rowKey="id"
        size="small"
        loading={logs.isLoading}
        dataSource={logs.data ?? []}
        pagination={false}
        columns={[
          { title: 'Người nhận', dataIndex: 'recipient', key: 'recipient' },
          { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
          { title: 'Thời gian', dataIndex: 'sentAt', key: 'sentAt', render: (v: string) => dateText(v) },
        ]}
      />
    </Drawer>
  );
}

function CampaignRowActions({ campaign }: { campaign: Campaign }) {
  const { has } = useAuth();
  const [sendOpen, setSendOpen] = useState(false);
  const [logOpen, setLogOpen] = useState(false);

  return (
    <Space>
      {has('marketing.send') ? (
        <Button size="small" onClick={() => setSendOpen(true)}>
          Gửi
        </Button>
      ) : null}
      <Button size="small" onClick={() => setLogOpen(true)}>
        Log
      </Button>
      <SendCampaignModal campaignId={campaign.id} open={sendOpen} onClose={() => setSendOpen(false)} />
      <CampaignLogsDrawer campaignId={campaign.id} open={logOpen} onClose={() => setLogOpen(false)} />
    </Space>
  );
}

const columns: ColumnsType<Campaign> = [
  { title: 'Tên', dataIndex: 'name', key: 'name' },
  {
    title: 'Kênh',
    dataIndex: 'channel',
    key: 'channel',
    render: (channel: number) => statusText(CHANNEL, channel),
  },
  { title: 'Tiêu đề', dataIndex: 'subject', key: 'subject' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
  {
    title: '',
    key: '__marketingActions',
    width: 160,
    render: (_: unknown, campaign: Campaign) => <CampaignRowActions campaign={campaign} />,
  },
];

export function MarketingPage() {
  return (
    <ResourcePage<Campaign, CampaignForm>
      title="Marketing"
      columns={columns}
      crud={noDeleteCrud}
      perms={{ create: 'marketing.create', update: 'marketing.update' }}
      toForm={(c) => ({
        name: c?.name ?? '',
        channel: c?.channel ?? 1,
        subject: c?.subject ?? null,
        body: c?.body ?? '',
        status: c?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa chiến dịch' : 'Thêm chiến dịch'}
          schema={mode === 'edit' ? campaignUpdateSchema : campaignCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="name" label="Tên" required />
          <SelectField name="channel" label="Kênh" options={CHANNEL_OPTIONS} required />
          <TextField name="subject" label="Tiêu đề" />
          <TextAreaField name="body" label="Nội dung" required />
          {mode === 'edit' ? <NumberField name="status" label="Trạng thái" required /> : null}
        </CrudFormModal>
      )}
    />
  );
}
