import { App, Button, Card, Col, Input, InputNumber, Modal, Row, Segmented, Select, Space, Table, Tag, Typography } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money, statusText } from '../../shared/format';
import {
  PAYMENT_STATUS,
  usePayServiceOperation,
  useProviderOptions,
  useServiceOperations,
  useServiceOperationStats,
} from './serviceOperationsApi';
import type { ServiceOperation, ServiceOperationFilter } from './serviceOperationsApi';
import { useDueAlerts } from '../serviceBookings/paymentTermsApi';
import { PaymentScheduleDrawer } from '../serviceBookings/PaymentScheduleDrawer';
import { CellStack, CellMoney, CellDate } from '../../shared/ui/TableCells';
import { DataCard } from '../../shared/ui';
import { StatGrid } from '../../ui/kit';
import dayjs from 'dayjs';

const STATUS_COLOR: Record<number, string> = { 0: 'red', 1: 'orange', 2: 'green' };

export function ServiceOperationsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('servicebooking.manage');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [providerId, setProviderId] = useState<string | undefined>();
  const [paymentStatus, setPaymentStatus] = useState<number | undefined>();
  const [applied, setApplied] = useState<ServiceOperationFilter>({});
  const applyFilters = () => {
    setApplied({ q: search || undefined, providerId });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setProviderId(undefined);
    setPaymentStatus(undefined);
    setApplied({});
    setPage(1);
  };

  const filter: ServiceOperationFilter = { ...applied, paymentStatus };
  const list = useServiceOperations(page, size, filter);
  const stats = useServiceOperationStats(filter);
  const providers = useProviderOptions();
  const providerOpts = (providers.data ?? []).map((p) => ({ label: p.name, value: p.id }));

  const [payRow, setPayRow] = useState<ServiceOperation | null>(null);
  const [payVal, setPayVal] = useState<number>(0);
  const pay = usePayServiceOperation();

  const dueAlerts = useDueAlerts(7);
  const alerts = dueAlerts.data ?? [];
  const overdueCount = alerts.filter((a) => a.isOverdue).length;
  const dueSoonCount = alerts.length - overdueCount;
  const [scheduleFor, setScheduleFor] = useState<{ id: string; label: string } | null>(null);

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<ServiceOperation> = [
    {
      title: 'Phiếu ĐH',
      key: 'code',
      width: 170,
      render: (_: unknown, r: ServiceOperation) => (
        <CellStack main={r.code} sub={r.usageDate ? new Date(r.usageDate).toLocaleDateString('vi-VN') : undefined} mono />
      ),
    },
    {
      title: 'Dịch vụ / NCC',
      key: 'description',
      render: (_: unknown, r: ServiceOperation) => (
        <CellStack main={r.description} sub={r.providerName ?? undefined} />
      ),
    },
    {
      title: 'Tổng chi',
      key: 'totalAmount',
      width: 160,
      align: 'right',
      render: (_: unknown, r: ServiceOperation) => <CellMoney value={r.totalAmount} />,
    },
    {
      title: 'Thanh toán',
      key: 'paidAmount',
      width: 190,
      align: 'right',
      render: (_: unknown, r: ServiceOperation) => (
        <CellMoney value={r.paidAmount} tone="success" sub={r.remainingAmount > 0 ? r.remainingAmount : undefined} subLabel="Còn" subTone="danger" />
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'paymentStatus',
      key: 'paymentStatus',
      width: 130,
      render: (v: number) => <Tag color={STATUS_COLOR[v]}>{statusText(PAYMENT_STATUS, v)}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 110,
      render: (_: unknown, r: ServiceOperation) =>
        canManage ? (
          <Button size="small" type="primary" onClick={() => { setPayRow(r); setPayVal(r.paidAmount); }}>
            Ghi chi
          </Button>
        ) : null,
    },
  ];

  const summary = () => (
    <Table.Summary fixed>
      <Table.Summary.Row>
        <Table.Summary.Cell index={0} colSpan={2}><b>Tổng cộng (trang này)</b></Table.Summary.Cell>
        <Table.Summary.Cell index={2} align="right"><b>{money(stats.data?.totalCost ?? 0)}</b></Table.Summary.Cell>
        <Table.Summary.Cell index={3} align="right">
          <b style={{ color: 'var(--tk-success)' }}>{money(stats.data?.totalPaid ?? 0)}</b>
          <br />
          <b style={{ color: 'var(--tk-danger)' }}>Còn {money(stats.data?.totalRemaining ?? 0)}</b>
        </Table.Summary.Cell>
        <Table.Summary.Cell index={4} colSpan={2} />
      </Table.Summary.Row>
    </Table.Summary>
  );

  return (
    <>
      <Typography.Title level={3} style={{ marginTop: 0 }}>Danh sách phiếu điều hành dịch vụ</Typography.Title>

      {alerts.length > 0 ? (
        <div style={{ marginBottom: 16 }}>
        <DataCard title="Đến hạn / Quá hạn thanh toán NCC">
          <StatGrid
            style={{ marginBottom: 12 }}
            min={180}
            items={[
              { label: 'Quá hạn', value: overdueCount, tone: 'danger', icon: 'error' },
              { label: 'Sắp đến hạn', value: dueSoonCount, tone: 'warning', icon: 'schedule' },
            ]}
          />
          <Table
            rowKey="id"
            pagination={false}
            dataSource={alerts}
            columns={[
              {
                title: 'Dịch vụ / NCC',
                key: 'service',
                render: (_: unknown, r: (typeof alerts)[number]) => (
                  <CellStack main={r.serviceCode} sub={r.providerName ?? undefined} mono />
                ),
              },
              {
                title: 'Còn lại',
                key: 'remaining',
                width: 150,
                align: 'right',
                render: (_: unknown, r: (typeof alerts)[number]) => <CellMoney value={r.remainingAmount} tone="danger" />,
              },
              {
                title: 'Hạn',
                key: 'due',
                width: 130,
                render: (_: unknown, r: (typeof alerts)[number]) => <CellDate value={dayjs(r.dueDate).format('DD/MM/YYYY')} />,
              },
              {
                title: 'Cảnh báo',
                key: 'flag',
                width: 150,
                render: (_: unknown, r: (typeof alerts)[number]) =>
                  r.isOverdue ? (
                    <Tag color="red">Quá hạn {Math.abs(r.daysUntilDue)} ngày</Tag>
                  ) : (
                    <Tag color="gold">Còn {r.daysUntilDue} ngày</Tag>
                  ),
              },
              {
                title: '',
                key: '__actions',
                width: 130,
                render: (_: unknown, r: (typeof alerts)[number]) => (
                  <Button size="small" onClick={() => setScheduleFor({ id: r.serviceBookingId, label: r.serviceCode })}>
                    Lịch thanh toán
                  </Button>
                ),
              },
            ]}
          />
        </DataCard>
        </div>
      ) : null}

      <StatGrid
        style={{ marginBottom: 16 }}
        items={[
          { label: 'Tổng đơn hàng', value: stats.data?.total ?? 0 },
          { label: 'Chưa thanh toán', value: stats.data?.unpaid ?? 0 },
          { label: 'Chưa chi hết', value: stats.data?.partial ?? 0 },
          { label: 'Hoàn thành', value: stats.data?.done ?? 0 },
        ]}
      />

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={7}>
            <Input.Search allowClear placeholder="Mã phiếu / NCC / tên dịch vụ" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhà cung cấp" options={providerOpts} value={providerId} onChange={(v) => setProviderId(v ?? undefined)} />
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
          value={paymentStatus === undefined ? 'all' : String(paymentStatus)}
          onChange={(val) => { setPaymentStatus(val === 'all' ? undefined : Number(val)); setPage(1); }}
          options={[
            { label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' },
            { label: `Chờ chi (${stats.data?.unpaid ?? 0})`, value: '0' },
            { label: `Chưa chi hết (${stats.data?.partial ?? 0})`, value: '1' },
            { label: `Thành công (${stats.data?.done ?? 0})`, value: '2' },
          ]}
        />
      </div>

      <DataCard title="Danh sách phiếu điều hành">
        <Table
          rowKey="id"
          columns={columns}
          dataSource={list.data?.items ?? []}
          loading={list.isLoading}
          scroll={{ x: 1080 }}
          pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
          summary={summary}
        />
      </DataCard>

      <Modal
        open={!!payRow}
        title={`Ghi chi — ${payRow?.code ?? ''}`}
        okText="Lưu"
        confirmLoading={pay.isPending}
        onCancel={() => setPayRow(null)}
        onOk={() =>
          run(async () => {
            await pay.mutateAsync({ id: payRow!.id, paidAmount: payVal });
            setPayRow(null);
          }, 'Đã cập nhật thanh toán')
        }
      >
        <Typography.Paragraph type="secondary">
          Tổng chi: <b>{money(payRow?.totalAmount ?? 0)}</b>. Nhập số đã thanh toán NCC.
        </Typography.Paragraph>
        <InputNumber
          style={{ width: '100%' }}
          min={0}
          value={payVal}
          onChange={(v) => setPayVal(Number(v ?? 0))}
          formatter={(v) => `${v}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
          parser={(v) => Number((v ?? '').replace(/,/g, ''))}
        />
      </Modal>

      {scheduleFor && (
        <PaymentScheduleDrawer
          bookingId={scheduleFor.id}
          bookingLabel={scheduleFor.label}
          onClose={() => setScheduleFor(null)}
        />
      )}
    </>
  );
}
