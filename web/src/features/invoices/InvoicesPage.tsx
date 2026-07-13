import { App, Button, Card, Col, DatePicker, Input, Popconfirm, Row, Segmented, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import dayjs from 'dayjs';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { dateText, money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { InvoiceLinesField } from './InvoiceLinesField';
import { useCreateInvoice, useDeleteInvoice, useInvoice, useInvoices, useInvoiceStats, useUpdateInvoice } from './invoicesApi';
import type { InvoiceFilter } from './invoicesApi';
import { invoiceFormSchema } from './types';
import type { InvoiceForm, InvoiceSummary } from './types';

const INVOICE_STATUS: Record<number, string> = {
  0: 'Nháp',
  1: 'Phát hành',
  2: 'Huỷ',
};
const INVOICE_STATUS_COLOR: Record<number, string> = { 0: 'default', 1: 'green', 2: 'red' };

function emptyForm(): InvoiceForm {
  return {
    series: '',
    number: '',
    invoiceDate: new Date().toISOString(),
    buyerName: '',
    buyerTaxCode: null,
    buyerAddress: null,
    status: 0,
    note: null,
    lines: [{ description: '', quantity: 1, unitPrice: 0, vatRate: 10 }],
  };
}

export function InvoicesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('invoice.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<number | undefined>();
  const [dateDraft, setDateDraft] = useState<{ from?: string; to?: string }>({});
  const [filter, setFilter] = useState<InvoiceFilter>({});
  const applyFilters = () => setFilter({ q: search || undefined, dateFrom: dateDraft.from, dateTo: dateDraft.to });
  const resetFilters = () => {
    setSearch('');
    setStatus(undefined);
    setDateDraft({});
    setFilter({});
    setPage(1);
  };
  const list = useInvoices(page, size, { ...filter, status });
  const stats = useInvoiceStats();

  const [editingId, setEditingId] = useState<string | 'new' | null>(null);
  const isEdit = editingId !== null && editingId !== 'new';
  const detail = useInvoice(isEdit ? editingId : '');

  const create = useCreateInvoice();
  const update = useUpdateInvoice();
  const remove = useDeleteInvoice();

  const modalOpen = editingId === 'new' || (isEdit && !!detail.data);
  const defaultValues: InvoiceForm =
    isEdit && detail.data
      ? {
          series: detail.data.series,
          number: detail.data.number,
          invoiceDate: detail.data.invoiceDate,
          buyerName: detail.data.buyerName,
          buyerTaxCode: detail.data.buyerTaxCode,
          buyerAddress: detail.data.buyerAddress,
          status: detail.data.status,
          note: detail.data.note,
          lines: detail.data.lines.map((l) => ({
            description: l.description,
            quantity: l.quantity,
            unitPrice: l.unitPrice,
            vatRate: l.vatRate,
          })),
        }
      : emptyForm();

  async function onSubmit(values: InvoiceForm) {
    try {
      if (isEdit) {
        await update.mutateAsync({ id: editingId, body: values });
      } else {
        await create.mutateAsync(values);
      }
      setEditingId(null);
      message.success('Đã lưu hoá đơn');
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

  const columns: ColumnsType<InvoiceSummary> = [
    { title: 'Ký hiệu', dataIndex: 'series', key: 'series', width: 120 },
    { title: 'Số', dataIndex: 'number', key: 'number', width: 110 },
    { title: 'Ngày', dataIndex: 'invoiceDate', key: 'invoiceDate', width: 110, render: (v: string) => dateText(v) },
    { title: 'Người mua', dataIndex: 'buyerName', key: 'buyerName', width: 180 },
    { title: 'MST', dataIndex: 'buyerTaxCode', key: 'buyerTaxCode', width: 120, render: (v: string | null) => v ?? '—' },
    { title: 'Tiền thuế', dataIndex: 'vatAmount', key: 'vatAmount', width: 120, align: 'right', render: (v?: number) => money(v ?? 0) },
    { title: 'Tổng tiền', dataIndex: 'totalAmount', key: 'totalAmount', width: 130, align: 'right', render: (v: number) => money(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 120, render: (v: number) => <Tag color={INVOICE_STATUS_COLOR[v]}>{statusText(INVOICE_STATUS, v)}</Tag> },
    {
      title: '',
      key: '__actions',
      width: 160,
      render: (_: unknown, item: InvoiceSummary) =>
        canManage ? (
          <Space>
            <Button size="small" onClick={() => setEditingId(item.id)}>
              Sửa
            </Button>
            <Popconfirm title="Xoá hoá đơn này?" onConfirm={() => onDelete(item.id)}>
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
          Hoá đơn VAT
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => setEditingId('new')}>
            Thêm hoá đơn
          </Button>
        ) : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Tổng hoá đơn', value: stats.data?.total ?? 0, money: false },
          { title: 'Tổng tiền', value: stats.data?.totalAmount ?? 0, money: true },
          { title: 'Tổng VAT', value: stats.data?.totalVat ?? 0, money: true },
          { title: 'Đã phát hành', value: stats.data?.issued ?? 0, money: false },
          { title: 'Nháp', value: stats.data?.draft ?? 0, money: false },
          { title: 'Huỷ', value: stats.data?.cancelled ?? 0, money: false },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} formatter={c.money ? (v) => money(Number(v)) : undefined} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search allowClear placeholder="Ký hiệu / số / người mua / MST" value={search}
              onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Ngày từ', 'đến']}
              value={dateDraft.from && dateDraft.to ? [dayjs(dateDraft.from), dayjs(dateDraft.to)] : null}
              onChange={(d) => setDateDraft({ from: d?.[0]?.startOf('day').toISOString(), to: d?.[1]?.endOf('day').toISOString() })} />
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
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...Object.entries(INVOICE_STATUS).map(([v, label]) => ({ label, value: v }))]}
        />
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 'max-content' }}
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
          title={isEdit ? 'Sửa hoá đơn' : 'Thêm hoá đơn'}
          schema={invoiceFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => setEditingId(null)}
          onSubmit={onSubmit}
        >
          <TextField name="series" label="Ký hiệu" />
          <TextField name="number" label="Số hoá đơn" />
          <TextField name="invoiceDate" label="Ngày hoá đơn (ISO)" required />
          <TextField name="buyerName" label="Người mua" required />
          <TextField name="buyerTaxCode" label="Mã số thuế" />
          <TextField name="buyerAddress" label="Địa chỉ" />
          <NumberField name="status" label="Trạng thái (0 nháp/1 phát hành/2 huỷ)" required />
          <TextAreaField name="note" label="Ghi chú" />
          <Typography.Text strong>Các dòng hàng hoá/dịch vụ</Typography.Text>
          <InvoiceLinesField />
        </CrudFormModal>
      ) : null}
    </>
  );
}
