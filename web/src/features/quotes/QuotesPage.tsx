import { App, Button, Popconfirm, Space, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { QuoteLinesField } from './QuoteLinesField';
import { useCreateQuote, useDeleteQuote, useQuote, useQuotes, useUpdateQuote } from './quotesApi';
import { quoteFormSchema } from './types';
import type { QuoteForm, QuoteSummary } from './types';

const QUOTE_STATUS: Record<number, string> = {
  0: 'Nháp',
  1: 'Đã gửi',
  2: 'Chấp nhận',
  3: 'Từ chối',
};

const EMPTY_FORM: QuoteForm = {
  code: '',
  customerName: '',
  title: '',
  validUntil: null,
  status: 0,
  note: null,
  lines: [{ description: '', quantity: 1, unitPrice: 0 }],
};

export function QuotesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('quote.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const list = useQuotes(page, size);

  const [editingId, setEditingId] = useState<string | 'new' | null>(null);
  const isEdit = editingId !== null && editingId !== 'new';
  const detail = useQuote(isEdit ? editingId : '');

  const create = useCreateQuote();
  const update = useUpdateQuote();
  const remove = useDeleteQuote();

  const modalOpen = editingId === 'new' || (isEdit && !!detail.data);
  const defaultValues: QuoteForm =
    isEdit && detail.data
      ? {
          code: detail.data.code,
          customerName: detail.data.customerName,
          title: detail.data.title,
          validUntil: detail.data.validUntil,
          status: detail.data.status,
          note: detail.data.note,
          lines: detail.data.lines.map((l) => ({
            description: l.description,
            quantity: l.quantity,
            unitPrice: l.unitPrice,
          })),
        }
      : EMPTY_FORM;

  async function onSubmit(values: QuoteForm) {
    try {
      if (isEdit) {
        await update.mutateAsync({ id: editingId, body: values });
      } else {
        await create.mutateAsync(values);
      }
      setEditingId(null);
      message.success('Đã lưu báo giá');
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

  const columns: ColumnsType<QuoteSummary> = [
    { title: 'Mã', dataIndex: 'code', key: 'code' },
    { title: 'Khách hàng', dataIndex: 'customerName', key: 'customerName' },
    { title: 'Tiêu đề', dataIndex: 'title', key: 'title' },
    { title: 'Tổng tiền', dataIndex: 'totalAmount', key: 'totalAmount', render: (v: number) => money(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => statusText(QUOTE_STATUS, v) },
    {
      title: '',
      key: '__actions',
      width: 160,
      render: (_: unknown, item: QuoteSummary) =>
        canManage ? (
          <Space>
            <Button size="small" onClick={() => setEditingId(item.id)}>
              Sửa
            </Button>
            <Popconfirm title="Xoá báo giá này?" onConfirm={() => onDelete(item.id)}>
              <Button size="small" danger>
                Xoá
              </Button>
            </Popconfirm>
          </Space>
        ) : null,
    },
  ];

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Báo giá
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => setEditingId('new')}>
            Thêm báo giá
          </Button>
        ) : null}
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        pagination={{
          current: page,
          pageSize: size,
          total: list.data?.total ?? 0,
          onChange: setPage,
          showSizeChanger: false,
        }}
      />

      {modalOpen ? (
        <CrudFormModal
          open
          title={isEdit ? 'Sửa báo giá' : 'Thêm báo giá'}
          schema={quoteFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditingId(null)}
          onSubmit={onSubmit}
        >
          <TextField name="code" label="Mã" required />
          <TextField name="title" label="Tiêu đề" required />
          <TextField name="customerName" label="Khách hàng" />
          <DatePickerField name="validUntil" label="Hiệu lực đến" />
          <NumberField name="status" label="Trạng thái (0 nháp/1 gửi/2 chấp nhận/3 từ chối)" required />
          <TextAreaField name="note" label="Ghi chú" />
          <Typography.Text strong>Các dòng báo giá</Typography.Text>
          <QuoteLinesField />
        </CrudFormModal>
      ) : null}
    </>
  );
}
