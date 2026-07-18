import { App, Button, Card, Col, DatePicker, Input, Modal, Popconfirm, Row, Segmented, Select, Space, Table, Tag, Typography } from '../../shared/ui/antd';
import dayjs from 'dayjs';
import type { ColumnsType } from '../../shared/ui/antd';
import { useMemo, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { departuresCrud } from '../booking/departuresApi';
import { QuoteLinesField } from './QuoteLinesField';
import { useConvertQuote, useCreateQuote, useDeleteQuote, useQuote, useQuotes, useQuoteStats, useUpdateQuote } from './quotesApi';
import type { QuoteFilter } from './quotesApi';
import { quoteFormSchema, QUOTE_TYPE_LABEL } from './types';
import type { QuoteForm, QuoteSummary } from './types';
import { CellStack, CellEntity, CellMoney, CellText, CellDate } from '../../shared/ui/TableCells';
import { StatGrid } from '../../ui/kit';
import { DataCard, ExportButton } from '../../shared/ui';
import { exportRowsToCsv } from '../../shared/exportCsv';

const QUOTE_STATUS_COLOR: Record<number, string> = { 0: 'default', 1: 'blue', 2: 'green', 3: 'red' };

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
  quoteType: 0,
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

// Màn Báo giá dùng chung cho 7 loại (Tour/Combo/GIT/Landtour/Booking Phòng/Dịch vụ lẻ/Visa).
// Route truyền quoteType + title → mỗi mục menu là 1 view lọc riêng, cùng khung calculator.
export function QuotesPage({ quoteType = 0, title = 'Báo giá' }: { quoteType?: number; title?: string } = {}) {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('quote.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<number | undefined>();
  const [dateDraft, setDateDraft] = useState<{ from?: string; to?: string }>({});
  const [filter, setFilter] = useState<QuoteFilter>({});
  const applyFilters = () =>
    setFilter({ q: search || undefined, status, validFrom: dateDraft.from, validTo: dateDraft.to });
  const resetFilters = () => {
    setSearch('');
    setStatus(undefined);
    setDateDraft({});
    setFilter({});
    setPage(1);
  };
  const list = useQuotes(page, size, { ...filter, status, quoteType });
  const stats = useQuoteStats(quoteType);

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
  const [fitDate, setFitDate] = useState<string | null>(null); // FIT: ngày khởi hành chuyến riêng
  const departures = departuresCrud.useList({ page: 1, size: 200 });
  const departureOptions = useMemo(
    () => (departures.data?.items ?? []).map((d) => ({ label: `${d.code} — ${d.title}`, value: d.id })),
    [departures.data],
  );

  async function onConvert() {
    if (!convertingId || (!departureId && !fitDate)) return;
    try {
      const result = await convert.mutateAsync({
        id: convertingId,
        tourDepartureId: departureId ?? null,
        departureDate: departureId ? null : fitDate,
      });
      setConvertingId(null);
      setDepartureId(undefined);
      setFitDate(null);
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
          quoteType: detail.data.quoteType,
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
      : { ...EMPTY_FORM, quoteType };

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

  // Xuất CSV trang hiện tại.
  const exportCsv = () =>
    exportRowsToCsv(
      'bao-gia.csv',
      ['Mã báo giá', 'Loại', 'Khách hàng', 'Tiêu đề', 'Người lớn', 'Trẻ em', 'Trẻ nhỏ', 'Tổng tiền', 'Lợi nhuận', 'Hạn hiệu lực', 'Trạng thái'],
      (list.data?.items ?? []).map((r) => [
        r.code,
        QUOTE_TYPE_LABEL[r.quoteType],
        r.customerName,
        r.title,
        r.adults ?? 0,
        r.children ?? 0,
        r.infants ?? 0,
        r.totalAmount,
        r.totalProfit ?? 0,
        r.validUntil ? new Date(r.validUntil).toLocaleDateString('vi-VN') : '',
        statusText(QUOTE_STATUS, r.status),
      ]),
    );

  const columns: ColumnsType<QuoteSummary> = [
    // Báo giá: mã (mono) + loại báo giá
    {
      title: 'Báo giá',
      key: 'code',
      width: 160,
      render: (_: unknown, r: QuoteSummary) => <CellStack main={r.code} mono sub={QUOTE_TYPE_LABEL[r.quoteType]} />,
    },
    // Khách / Tour: khách hàng (đậm) + tiêu đề (mờ)
    {
      title: 'Khách / Tour',
      key: 'customerName',
      width: 280,
      render: (_: unknown, r: QuoteSummary) => <CellEntity name={r.customerName} meta={r.title} title={r.title} />,
    },
    // Số khách: NL / TE / EB
    {
      title: 'Số khách',
      key: '__pax',
      width: 120,
      align: 'center',
      render: (_: unknown, r: QuoteSummary) => (
        <CellText mono>{`${r.adults ?? 0}NL / ${r.children ?? 0}TE / ${r.infants ?? 0}EB`}</CellText>
      ),
    },
    // Giá trị: tổng bán (chính) + lợi nhuận (phụ, tone theo dấu)
    {
      title: 'Giá trị',
      key: 'totalAmount',
      width: 180,
      align: 'right',
      render: (_: unknown, r: QuoteSummary) => (
        <CellMoney
          value={r.totalAmount}
          sub={r.totalProfit ?? 0}
          subLabel="LN"
          subTone={(r.totalProfit ?? 0) < 0 ? 'danger' : 'success'}
        />
      ),
    },
    // Hạn hiệu lực
    {
      title: 'Hạn hiệu lực',
      key: 'validUntil',
      width: 130,
      render: (_: unknown, r: QuoteSummary) => (
        <CellDate value={r.validUntil ? new Date(r.validUntil).toLocaleDateString('vi-VN') : undefined} />
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 150,
      render: (v: number, r: QuoteSummary) => (
        <Space size={4}>
          <Tag color={QUOTE_STATUS_COLOR[v]}>{statusText(QUOTE_STATUS, v)}</Tag>
          {r.convertedOrderId ? <Tag color="purple">Đã chuyển đơn</Tag> : null}
        </Space>
      ),
    },
    {
      title: '',
      key: '__actions',
      width: 180,
      render: (_: unknown, item: QuoteSummary) =>
        canManage ? (
          <Space>
            <Button size="small" onClick={() => setEditingId(item.id)}>
              Sửa
            </Button>
            <Button size="small" onClick={() => window.open(`/quotes/${item.id}/print`, '_blank')}>
              In
            </Button>
            {item.status === 2 && !item.convertedOrderId ? (
              <Button size="small" type="primary" onClick={() => setConvertingId(item.id)}>
                Chuyển đơn
              </Button>
            ) : null}
            {item.convertedOrderId ? (
              // Đã chuyển đơn → mở đơn để thu tiền (đề nghị thanh toán = flow phiếu thu + duyệt sẵn có).
              <Button size="small" onClick={() => window.open(`/orders/${item.convertedOrderId}`, '_blank')}>
                Đơn
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
          {title}
        </Typography.Title>
        <div style={{ display: 'flex', gap: 10 }}>
          <ExportButton filename="bao-gia.csv" onExport={exportCsv} />
          {canManage ? (
            <Button type="primary" onClick={() => setEditingId('new')}>
              Thêm báo giá
            </Button>
          ) : null}
        </div>
      </div>

      {/* Thẻ thống kê */}
      <StatGrid
        style={{ marginBottom: 16 }}
        items={[
          { label: 'Tổng báo giá', value: stats.data?.total ?? 0 },
          { label: 'Tổng giá trị', value: money(stats.data?.totalAmount ?? 0) },
          { label: 'Lợi nhuận dự kiến', value: money(stats.data?.totalProfit ?? 0) },
          { label: 'Chấp nhận', value: stats.data?.accepted ?? 0 },
          { label: 'Đã gửi', value: stats.data?.sent ?? 0 },
          { label: 'Từ chối', value: stats.data?.rejected ?? 0 },
        ]}
      />

      {/* Thanh lọc */}
      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={8}>
            <Input.Search allowClear placeholder="Mã / khách hàng / tiêu đề" value={search}
              onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={8}>
            <DatePicker.RangePicker style={{ width: '100%' }} placeholder={['Hạn từ', 'đến']}
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

      {/* Tabs trạng thái */}
      <div style={{ marginBottom: 12, overflowX: 'auto' }}>
        <Segmented
          value={status === undefined ? 'all' : String(status)}
          onChange={(val) => {
            setStatus(val === 'all' ? undefined : Number(val));
            setPage(1);
          }}
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...Object.entries(QUOTE_STATUS).map(([v, label]) => ({ label, value: v }))]}
        />
      </div>

      <DataCard title="Danh sách báo giá">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          childrenColumnName="__noNested" /* field 'children' (số trẻ em) KHÔNG phải hàng con lồng nhau */
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
        okButtonProps={{ disabled: !departureId && !fitDate }}
        confirmLoading={convert.isPending}
        onCancel={() => {
          setConvertingId(null);
          setDepartureId(undefined);
          setFitDate(null);
        }}
        onOk={onConvert}
      >
        <Typography.Paragraph type="secondary">
          Đơn sẽ đặt chỗ theo số khách của báo giá (giá chỗ = giá báo giá); doanh thu đơn = tổng báo
          giá; các dòng dịch vụ (KS/xe/visa/vé/vé bay) sinh phiếu đặt dịch vụ lẻ.
        </Typography.Paragraph>
        <Select
          style={{ width: '100%', marginBottom: 8 }}
          placeholder="Ghép chuyến khởi hành sẵn có"
          allowClear
          loading={departures.isLoading}
          options={departureOptions}
          value={departureId}
          onChange={(v) => {
            setDepartureId(v ?? undefined);
            if (v) setFitDate(null); // chọn chuyến sẵn → bỏ chế độ FIT
          }}
          showSearch
          optionFilterProp="label"
        />
        <Typography.Paragraph type="secondary" style={{ marginBottom: 4 }}>
          Hoặc tour lẻ FIT — nhập ngày khởi hành, hệ tự tạo chuyến riêng (kín, đúng số khách):
        </Typography.Paragraph>
        <DatePicker
          style={{ width: '100%' }}
          disabled={!!departureId}
          value={fitDate ? dayjs(fitDate) : null}
          onChange={(d) => setFitDate(d ? d.toISOString() : null)}
        />
      </Modal>
    </>
  );
}
