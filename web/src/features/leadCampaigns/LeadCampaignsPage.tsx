import { App, Button, Card, Col, Input, Progress, Row, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { TextAreaField, TextField } from '../../shared/ui/Field';
import { z } from 'zod';
import {
  LEAD_CAMPAIGN_STATUS,
  useCreateLeadCampaign,
  useLeadCampaigns,
  useLeadCampaignStats,
} from './leadCampaignsApi';
import type { LeadCampaign, LeadCampaignFilter } from './leadCampaignsApi';

const createSchema = z.object({ name: z.string().min(1, 'Bắt buộc'), note: z.string().nullable() });
type CreateForm = z.infer<typeof createSchema>;

const STATUS_COLOR: Record<number, string> = { 0: 'processing', 1: 'green' };

export function LeadCampaignsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('lead.create');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [applied, setApplied] = useState<LeadCampaignFilter>({});
  const applyFilters = () => {
    setApplied({ q: search || undefined });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setApplied({});
    setPage(1);
  };

  const list = useLeadCampaigns(page, size, applied);
  const stats = useLeadCampaignStats();
  const [creating, setCreating] = useState(false);
  const create = useCreateLeadCampaign();

  async function onCreate(values: CreateForm) {
    try {
      await create.mutateAsync({ name: values.name, note: values.note ?? null });
      message.success('Đã tạo chiến dịch');
      setCreating(false);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<LeadCampaign> = [
    { title: 'Chiến dịch', dataIndex: 'name', key: 'name' },
    { title: 'Người tạo', dataIndex: 'createdByName', key: 'createdByName', width: 160, render: (v: string | null) => v ?? '—' },
    { title: 'Ngày tạo', dataIndex: 'createdAt', key: 'createdAt', width: 120, render: (v: string) => new Date(v).toLocaleDateString('vi-VN') },
    { title: 'Leads', dataIndex: 'totalLeads', key: 'totalLeads', width: 90, align: 'right' },
    {
      title: 'Tiến độ',
      key: 'progress',
      width: 160,
      render: (_: unknown, r: LeadCampaign) => (
        <div>
          <Progress percent={Number(r.progress)} size="small" />
          <span style={{ fontSize: 12, color: '#888' }}>Chăm sóc {r.caredCount}/{r.totalLeads}</span>
        </div>
      ),
    },
    {
      title: 'Đơn chốt',
      key: 'closed',
      width: 130,
      align: 'right',
      render: (_: unknown, r: LeadCampaign) => (
        <div>
          <b style={{ color: '#3f8600' }}>{r.closedCount}</b>
          <div style={{ fontSize: 12, color: '#888' }}>{r.closeRate}%</div>
        </div>
      ),
    },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 120, render: (v: number) => <Tag color={STATUS_COLOR[v]}>{statusText(LEAD_CAMPAIGN_STATUS, v)}</Tag> },
  ];

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>Chia số Sale</Typography.Title>
        {canManage ? <Button type="primary" onClick={() => setCreating(true)}>Tạo chiến dịch mới</Button> : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Tổng chiến dịch', value: stats.data?.totalCampaigns ?? 0, suffix: '' },
          { title: 'Tổng Leads', value: stats.data?.totalLeads ?? 0, suffix: '' },
          { title: 'Tỷ lệ chốt TB', value: stats.data?.avgCloseRate ?? 0, suffix: '%' },
          { title: 'Hoàn thành', value: stats.data?.completed ?? 0, suffix: '' },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={12} lg={6} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} suffix={c.suffix} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search allowClear placeholder="Tên chiến dịch" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>Tìm kiếm</Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 'max-content' }}
        pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
      />

      {creating ? (
        <CrudFormModal
          open
          title="Tạo chiến dịch chia số"
          schema={createSchema}
          defaultValues={{ name: '', note: null }}
          submitting={create.isPending}
          onCancel={() => setCreating(false)}
          onSubmit={onCreate}
        >
          <TextField name="name" label="Tên chiến dịch" required />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      ) : null}
    </>
  );
}
