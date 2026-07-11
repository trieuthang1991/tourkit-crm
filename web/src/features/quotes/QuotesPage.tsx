import { App, Button, Modal, Popconfirm, Select, Space, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useMemo, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { departuresCrud } from '../booking/departuresApi';
import { QuoteLinesField } from './QuoteLinesField';
import { useConvertQuote, useCreateQuote, useDeleteQuote, useQuote, useQuotes, useUpdateQuote } from './quotesApi';
import { quoteFormSchema } from './types';
import type { QuoteForm, QuoteSummary } from './types';

const QUOTE_STATUS: Record<number, string> = {
  0: 'Nháp',
  1: 'Đã gửi',
  2: 'Chấp nhận',
  3: 'Từ chối',
};

const EMPTY_LINE = {
  description: '',
  quantity: 1,
  unitPrice: 0,
  serviceType: 0,
  scope: 1,
  providerServiceId: null,
  unitCost: 0,
  marginPercent: 0,
};

const EMPTY_FORM: QuoteForm = {
  code: '',
  customerName: '',
  title: '',
  validUntil: null,
  status: 0,
  note: null,
  adults: 0,
  children: 0,
  infants: 0,
  childPercent: 75,
  infantPercent: 50,
  lines: [{ ...EMPTY_LINE }],
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

  // Chuyển báo giá chấp nhận → đơn: chọn chuyến khởi hành đích.
  const convert = useConvertQuote();
  const [convertingId, setConvertingId] = useState<string | null>(null);
  const [departureId, setDepartureId] = useState<string | undefined>();
  const departures = departuresCrud.useList({ page: 1, size: 200 });
  const departureOptions = useMemo(
    () => (departures.data?.items ?? []).map((d) => ({ label: `${d.code} — ${d.title}`, value: d.id })),
    [departures.data],
  );

  async function onConvert() {
    if (!convertingId || !departureId) return;
    try {
      const result = await convert.mutateAsync({ id: convertingId, tourDepartureId: departureId });
      setConvertingId(null);
      setDepartureId(undefined);
      message.success(`Đã tạo đơn ${result.orderCode} (+${result.serviceBookingCount} đặt dịch vụ)`);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

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
          adults: detail.data.adults,
          children: detail.data.children,
          infants: detail.data.infants,
          childPercent: detail.data.childPercent,
          infantPercent: detail.data.infantPercent,
          lines: detail.data.lines.map((l) => ({
            description: l.description,
            quantity: l.quantity,
            unitPrice: l.unitPrice,
            serviceType: l.serviceType,
            scope: l.scope,
            providerServiceId: l.providerServiceId,
            unitCost: l.unitCost,
            marginPercent: l.marginPercent,
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
            <Button size="small" onClick={() => window.open(`/quotes/${item.id}/print`, '_blank')}>
              In
            </Button>
            {item.status === 2 ? (
              <Button size="small" type="primary" onClick={() => setConvertingId(item.id)}>
                Chuyển đơn
              </Button>
            ) : null}
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
          {/* Dự trù giá: số khách theo hạng + % giá trẻ so với NL (spec 2026-07-11). */}
          <div style={{ display: 'flex', gap: 8 }}>
            <div style={{ flex: 1 }}>
              <NumberField name="adults" label="Người lớn" />
            </div>
            <div style={{ flex: 1 }}>
              <NumberField name="children" label="Trẻ em" />
            </div>
            <div style={{ flex: 1 }}>
              <NumberField name="infants" label="Trẻ nhỏ" />
            </div>
            <div style={{ flex: 1 }}>
              <NumberField name="childPercent" label="% giá TE" />
            </div>
            <div style={{ flex: 1 }}>
              <NumberField name="infantPercent" label="% giá TN" />
            </div>
          </div>
          <Typography.Text strong>Các dòng báo giá</Typography.Text>
          <QuoteLinesField />
          {isEdit && detail.data ? (
            <Typography.Text type="secondary">
              Giá NL: {money(detail.data.adultPrice)} · TE: {money(detail.data.childPrice)} · TN:{' '}
              {money(detail.data.infantPrice)} — Vốn: {money(detail.data.totalCost)} · Bán:{' '}
              {money(detail.data.totalAmount)} · Lãi dự kiến: {money(detail.data.totalProfit)}
            </Typography.Text>
          ) : null}
        </CrudFormModal>
      ) : null}

      <Modal
        open={convertingId !== null}
        title="Chuyển báo giá thành đơn"
        okText="Chuyển"
        okButtonProps={{ disabled: !departureId }}
        confirmLoading={convert.isPending}
        onCancel={() => {
          setConvertingId(null);
          setDepartureId(undefined);
        }}
        onOk={onConvert}
      >
        <Typography.Paragraph type="secondary">
          Đơn sẽ đặt chỗ theo số khách của báo giá; doanh thu đơn = tổng báo giá; các dòng dịch vụ
          (KS/xe/visa/vé/vé bay) sinh phiếu đặt dịch vụ lẻ.
        </Typography.Paragraph>
        <Select
          style={{ width: '100%' }}
          placeholder="Chọn chuyến khởi hành"
          loading={departures.isLoading}
          options={departureOptions}
          value={departureId}
          onChange={setDepartureId}
          showSearch
          optionFilterProp="label"
        />
      </Modal>
    </>
  );
}
