import { App, Card, Col, DatePicker, Input, Popconfirm, Row, Space, Table, Typography } from '../../shared/ui/antd';
import { StatGrid } from '../../ui/kit';
import { Button, DataCard, ExportButton, SegmentTabs, StatusTag, invoiceTone } from '../../shared/ui';
import { exportRowsToCsv } from '../../shared/exportCsv';
import type { ColumnsType } from '../../shared/ui/antd';
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
import { CellDate, CellEntity, CellMoney, CellStack } from '../../shared/ui/TableCells';

const INVOICE_STATUS: Record<number, string> = {
  0: 'Nháp',
  1: 'Phát hành',
  2: 'Huỷ',
};

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

  // Xuất CSV trang hiện tại.
  const exportCsv = () =>
    exportRowsToCsv(
      'hoa-don.csv',
      ['Số hoá đơn', 'Ký hiệu', 'Ngày', 'Người mua', 'Mã số thuế', 'Tổng tiền', 'VAT', 'Trạng thái'],
      (list.data?.items ?? []).map((item) => [
        item.number,
        item.series,
        dateText(item.invoiceDate),
        item.buyerName,
        item.buyerTaxCode ?? '',
        item.totalAmount,
        item.vatAmount ?? 0,
        statusText(INVOICE_STATUS, item.status),
      ]),
    );

  const columns: ColumnsType<InvoiceSummary> = [
    {
      title: 'Hoá đơn',
      key: 'invoice',
      width: 170,
      render: (_: unknown, item: InvoiceSummary) => (
        <CellStack main={item.number} sub={item.series} mono tone="heading" subTone="muted" subMono />
      ),
    },
    { title: 'Ngày', dataIndex: 'invoiceDate', key: 'invoiceDate', width: 130, render: (v: string) => <CellDate value={dateText(v)} /> },
    {
      title: 'Người mua',
      key: 'buyer',
      width: 300,
      render: (_: unknown, item: InvoiceSummary) => (
        <CellEntity name={item.buyerName} code={item.buyerTaxCode ? `MST ${item.buyerTaxCode}` : undefined} />
      ),
    },
    {
      title: 'Giá trị',
      key: 'amount',
      width: 180,
      align: 'right',
      render: (_: unknown, item: InvoiceSummary) => (
        <CellMoney value={item.totalAmount} sub={item.vatAmount ?? 0} subLabel="VAT" subTone="muted" />
      ),
    },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', width: 140, render: (v: number) => <StatusTag tone={invoiceTone(v)}>{statusText(INVOICE_STATUS, v)}</StatusTag> },
    {
      title: '',
      key: '__actions',
      width: 160,
      render: (_: unknown, item: InvoiceSummary) =>
        canManage ? (
          <Space>
            <Button variant="ghost" size="small" onClick={() => setEditingId(item.id)}>
              Sửa
            </Button>
            <Popconfirm title="Xoá hoá đơn này?" onConfirm={() => onDelete(item.id)}>
              <Button variant="danger" size="small">
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
        <div style={{ display: 'flex', gap: 10 }}>
          <ExportButton filename="hoa-don.csv" onExport={exportCsv} />
          {canManage ? (
            <Button variant="primary" onClick={() => setEditingId('new')}>
              Thêm hoá đơn
            </Button>
          ) : null}
        </div>
      </div>

      <StatGrid
        style={{ marginBottom: 16 }}
        items={[
          { label: 'Tổng hoá đơn', value: stats.data?.total ?? 0 },
          { label: 'Tổng tiền', value: money(stats.data?.totalAmount ?? 0) },
          { label: 'Tổng VAT', value: money(stats.data?.totalVat ?? 0) },
          { label: 'Đã phát hành', value: stats.data?.issued ?? 0 },
          { label: 'Nháp', value: stats.data?.draft ?? 0 },
          { label: 'Huỷ', value: stats.data?.cancelled ?? 0 },
        ]}
      />

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
              <Button variant="primary" onClick={applyFilters}>Tìm kiếm</Button>
              <Button variant="ghost" onClick={resetFilters}>Đặt lại</Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <SegmentTabs
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => {
            setStatus(val === 'all' ? undefined : Number(val));
            setPage(1);
          }}
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...Object.entries(INVOICE_STATUS).map(([v, label]) => ({ label, value: v }))]}
        />
      </div>

      <DataCard title="Danh sách hoá đơn (VAT)">
      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 1080 }}
        pagination={{
          current: page,
          pageSize: size,
          total: list.data?.total ?? 0,
          onChange: setPage,
          showSizeChanger: false,
        }}
      />
      </DataCard>

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
