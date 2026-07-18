import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Table, Tag, Typography } from '../../shared/ui/antd';
import { StatGrid } from '../../ui/kit';
import { DataCard } from '../../shared/ui';
import type { ColumnsType } from '../../shared/ui/antd';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { agentsCrud } from '../agents/agentsCrud';
import { CellEntity, CellMoney, CellText, CellDate } from '../../shared/ui/TableCells';
import {
  useAgentQuotes,
  useAgentQuoteStats,
  useConfirmAgentQuote,
  useCreateAgentQuote,
  useQuoteAgentRequest,
  useRejectAgentQuote,
} from './agentQuotesApi';
import type { AgentQuoteFilter } from './agentQuotesApi';
import { AGENT_QUOTE_STATUS, createAgentQuoteFormSchema, quoteActionFormSchema } from './types';
import type { AgentQuote, CreateAgentQuoteForm, QuoteActionForm } from './types';

const REQUESTED = 1;
const QUOTED = 2;
const AQ_STATUS_COLOR: Record<number, string> = { 1: 'orange', 2: 'blue', 3: 'green', 4: 'red' };

export function AgentQuotesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('agentquote.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [agentId, setAgentId] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [filter, setFilter] = useState<AgentQuoteFilter>({});
  const applyFilters = () => {
    setFilter({ q: search || undefined, agentId });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setAgentId(undefined);
    setStatus(undefined);
    setFilter({});
    setPage(1);
  };
  const list = useAgentQuotes(page, size, { ...filter, status });
  const stats = useAgentQuoteStats();
  const agents = agentsCrud.useList({ page: 1, size: 500 });
  const agentOpts = (agents.data?.items ?? []).map((a) => ({ label: a.name, value: a.id }));

  const [creating, setCreating] = useState(false);
  const [quotingId, setQuotingId] = useState<string | null>(null);

  const create = useCreateAgentQuote();
  const quote = useQuoteAgentRequest();
  const confirm = useConfirmAgentQuote();
  const reject = useRejectAgentQuote();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onCreate(values: CreateAgentQuoteForm) {
    await run(async () => {
      await create.mutateAsync(values);
      setCreating(false);
    }, 'Đã gửi yêu cầu báo giá');
  }

  async function onQuote(values: QuoteActionForm) {
    if (!quotingId) return;
    await run(async () => {
      await quote.mutateAsync({ id: quotingId, body: values });
      setQuotingId(null);
    }, 'Đã chào giá');
  }

  const columns: ColumnsType<AgentQuote> = [
    // Đại lý / Tour: tên đại lý (đậm) + sản phẩm/tour (mờ)
    {
      title: 'Đại lý / Tour',
      key: '__agent',
      width: 280,
      render: (_: unknown, r: AgentQuote) => (
        <CellEntity name={r.agentName ?? '—'} meta={r.productName} title={r.specialRequests ?? r.productName} />
      ),
    },
    // Số khách
    {
      title: 'Số khách',
      key: '__pax',
      width: 110,
      align: 'center',
      render: (_: unknown, r: AgentQuote) => <CellText mono>{`${r.paxCount} khách`}</CellText>,
    },
    // Ngày đi: ngày khởi hành (chính) + ngày về (phụ)
    {
      title: 'Ngày đi',
      key: '__dates',
      width: 150,
      render: (_: unknown, r: AgentQuote) => (
        <CellDate
          value={r.travelDate ? new Date(r.travelDate).toLocaleDateString('vi-VN') : undefined}
          sub={r.returnDate ? `→ ${new Date(r.returnDate).toLocaleDateString('vi-VN')}` : undefined}
        />
      ),
    },
    // Giá chào: giá (chính, mono) + ghi chú chào giá (phụ)
    {
      title: 'Giá chào',
      key: '__quoted',
      width: 180,
      align: 'right',
      render: (_: unknown, r: AgentQuote) => (
        <CellMoney value={r.quotedAmount ?? undefined} sub={r.quotedNote ?? undefined} />
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 130,
      render: (v: number) => <Tag color={AQ_STATUS_COLOR[v]}>{statusText(AGENT_QUOTE_STATUS, v)}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 240,
      render: (_: unknown, item: AgentQuote) => {
        if (!canManage) return null;
        return (
          <Space>
            {item.status === REQUESTED ? (
              <Button size="small" type="primary" onClick={() => setQuotingId(item.id)}>
                Chào giá
              </Button>
            ) : null}
            {item.status === QUOTED ? (
              <>
                <Popconfirm title="Xác nhận báo giá này?" onConfirm={() => run(() => confirm.mutateAsync(item.id), 'Đã xác nhận')}>
                  <Button size="small" type="primary">
                    Xác nhận
                  </Button>
                </Popconfirm>
                <Popconfirm title="Từ chối báo giá này?" onConfirm={() => run(() => reject.mutateAsync(item.id), 'Đã từ chối')}>
                  <Button size="small" danger>
                    Từ chối
                  </Button>
                </Popconfirm>
              </>
            ) : null}
          </Space>
        );
      },
    },
  ];

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Yêu cầu báo giá (Đại lý B2B)
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => setCreating(true)}>
            Gửi yêu cầu
          </Button>
        ) : null}
      </div>

      <StatGrid
        items={[
          { label: 'Tổng yêu cầu', value: stats.data?.total ?? 0 },
          { label: 'Chờ chào', value: stats.data?.requested ?? 0 },
          { label: 'Đã chào', value: stats.data?.quoted ?? 0 },
          { label: 'Xác nhận', value: stats.data?.confirmed ?? 0 },
          { label: 'Từ chối', value: stats.data?.rejected ?? 0 },
          { label: 'Tổng giá chào', value: money(stats.data?.totalQuoted ?? 0) },
        ]}
        style={{ marginBottom: 16 }}
      />

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Input.Search allowClear placeholder="Sản phẩm/Tour" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Đại lý"
              options={agentOpts} value={agentId} onChange={(v) => setAgentId(v ?? undefined)} />
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
            setPage(1);
          }}
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...Object.entries(AGENT_QUOTE_STATUS).map(([v, label]) => ({ label, value: v }))]}
        />
      </div>

      <DataCard title="Danh sách báo giá đại lý">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
        />
      </DataCard>

      {creating ? (
        <CrudFormModal
          open
          title="Gửi yêu cầu báo giá"
          schema={createAgentQuoteFormSchema}
          defaultValues={{
            agentId: '',
            productName: '',
            travelDate: null,
            returnDate: null,
            paxCount: 1,
            specialRequests: null,
          }}
          submitting={create.isPending}
          onCancel={() => setCreating(false)}
          onSubmit={onCreate}
        >
          <TextField name="agentId" label="Mã đại lý (agentId)" required />
          <TextField name="productName" label="Sản phẩm/Tour" required />
          <NumberField name="paxCount" label="Số khách" required />
          <TextAreaField name="specialRequests" label="Yêu cầu riêng" />
        </CrudFormModal>
      ) : null}

      {quotingId ? (
        <CrudFormModal
          open
          title="Chào giá"
          schema={quoteActionFormSchema}
          defaultValues={{ quotedAmount: 0, quotedNote: null }}
          submitting={quote.isPending}
          onCancel={() => setQuotingId(null)}
          onSubmit={onQuote}
        >
          <NumberField name="quotedAmount" label="Giá chào" required />
          <TextAreaField name="quotedNote" label="Ghi chú" />
        </CrudFormModal>
      ) : null}
    </>
  );
}
