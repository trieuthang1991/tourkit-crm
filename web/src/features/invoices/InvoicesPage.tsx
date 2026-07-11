import { App, Button, Popconfirm, Space, Table, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { dateText, money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { InvoiceLinesField } from './InvoiceLinesField';
import { useCreateInvoice, useDeleteInvoice, useInvoice, useInvoices, useUpdateInvoice } from './invoicesApi';
import { invoiceFormSchema } from './types';
import type { InvoiceForm, InvoiceSummary } from './types';

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
  const list = useInvoices(page, size);

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
    { title: 'Ký hiệu', dataIndex: 'series', key: 'series' },
    { title: 'Số', dataIndex: 'number', key: 'number' },
    { title: 'Ngày', dataIndex: 'invoiceDate', key: 'invoiceDate', render: (v: string) => dateText(v) },
    { title: 'Người mua', dataIndex: 'buyerName', key: 'buyerName' },
    { title: 'Tổng tiền', dataIndex: 'totalAmount', key: 'totalAmount', render: (v: number) => money(v) },
    { title: 'Trạng thái', dataIndex: 'status', key: 'status', render: (v: number) => statusText(INVOICE_STATUS, v) },
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
