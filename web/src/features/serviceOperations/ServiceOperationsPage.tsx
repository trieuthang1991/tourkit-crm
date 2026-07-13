import { App, Button, Card, Col, Input, InputNumber, Modal, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
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

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<ServiceOperation> = [
    { title: 'Mã phiếu', dataIndex: 'code', key: 'code', width: 130, fixed: 'left' },
    { title: 'Nhà cung cấp', dataIndex: 'providerName', key: 'providerName', width: 180, render: (v: string | null) => v ?? '—' },
    { title: 'Tên dịch vụ', dataIndex: 'description', key: 'description' },
    { title: 'Ngày sử dụng DV', dataIndex: 'usageDate', key: 'usageDate', width: 140, render: (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—') },
    { title: 'Tổng tiền chi', dataIndex: 'totalAmount', key: 'totalAmount', width: 140, align: 'right', render: (v: number) => money(v) },
    { title: 'Đã thanh toán', dataIndex: 'paidAmount', key: 'paidAmount', width: 140, align: 'right', render: (v: number) => <span style={{ color: '#3f8600' }}>{money(v)}</span> },
    { title: 'Còn thiếu', dataIndex: 'remainingAmount', key: 'remainingAmount', width: 140, align: 'right', render: (v: number) => <span style={{ color: v > 0 ? '#cf1322' : undefined }}>{money(v)}</span> },
    { title: 'Trạng thái', dataIndex: 'paymentStatus', key: 'paymentStatus', width: 130, render: (v: number) => <Tag color={STATUS_COLOR[v]}>{statusText(PAYMENT_STATUS, v)}</Tag> },
    {
      title: '',
      key: '__actions',
      width: 100,
      fixed: 'right',
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
        <Table.Summary.Cell index={0} colSpan={4}><b>Tổng cộng (trang này)</b></Table.Summary.Cell>
        <Table.Summary.Cell index={4} align="right"><b>{money(stats.data?.totalCost ?? 0)}</b></Table.Summary.Cell>
        <Table.Summary.Cell index={5} align="right"><b style={{ color: '#3f8600' }}>{money(stats.data?.totalPaid ?? 0)}</b></Table.Summary.Cell>
        <Table.Summary.Cell index={6} align="right"><b style={{ color: '#cf1322' }}>{money(stats.data?.totalRemaining ?? 0)}</b></Table.Summary.Cell>
        <Table.Summary.Cell index={7} colSpan={2} />
      </Table.Summary.Row>
    </Table.Summary>
  );

  return (
    <>
      <Typography.Title level={3} style={{ marginTop: 0 }}>Danh sách phiếu điều hành dịch vụ</Typography.Title>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Tổng đơn hàng', value: stats.data?.total ?? 0, money: false },
          { title: 'Chưa thanh toán', value: stats.data?.unpaid ?? 0, money: false },
          { title: 'Chưa chi hết', value: stats.data?.partial ?? 0, money: false },
          { title: 'Hoàn thành', value: stats.data?.done ?? 0, money: false },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={12} lg={6} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

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

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 'max-content' }}
        pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
        summary={summary}
      />

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
    </>
  );
}
