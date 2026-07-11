import { App, Button, Popconfirm, Space, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import {
  useAgentQuotes,
  useConfirmAgentQuote,
  useCreateAgentQuote,
  useQuoteAgentRequest,
  useRejectAgentQuote,
} from './agentQuotesApi';
import { AGENT_QUOTE_STATUS, createAgentQuoteFormSchema, quoteActionFormSchema } from './types';
import type { AgentQuote, CreateAgentQuoteForm, QuoteActionForm } from './types';

const REQUESTED = 1;
const QUOTED = 2;

export function AgentQuotesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('agentquote.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const list = useAgentQuotes(page, size);

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
    { title: 'Đại lý', dataIndex: 'agentId', key: 'agentId' },
    { title: 'Sản phẩm', dataIndex: 'productName', key: 'productName' },
    { title: 'Số khách', dataIndex: 'paxCount', key: 'paxCount' },
    {
      title: 'Giá chào',
      dataIndex: 'quotedAmount',
      key: 'quotedAmount',
      render: (v: number | null) => (v == null ? '—' : money(v)),
    },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => statusText(AGENT_QUOTE_STATUS, v) },
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

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
      />

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
