import { App, Button, Card, Col, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField } from '../../shared/ui/Field';
import { customerCommissionRulesCrud } from './customerCommissionRulesCrud';
import {
  useCustomerCommissionRules,
  useCustomerCommissionStats,
  useCustomerTypeOptions,
} from './customerCommissionRulesApi';
import type { CustomerCommissionFilter } from './customerCommissionRulesApi';
import { CUSTOMER_COMMISSION_STATUS, customerCommissionRuleFormSchema } from './types';
import type { CustomerCommissionRule, CustomerCommissionRuleForm } from './types';

const STATUS_COLOR: Record<number, string> = { 1: 'green', 0: 'default' };

export function CustomerCommissionRulesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('commission.create');

  const [page, setPage] = useState(1);
  const size = 20;
  const [customerType, setCustomerType] = useState<number | undefined>();
  const [status, setStatus] = useState<number | undefined>();

  const list = useCustomerCommissionRules(page, size, { customerType, status } as CustomerCommissionFilter);
  const stats = useCustomerCommissionStats();
  const types = useCustomerTypeOptions();
  const typeOpts = (types.data ?? []).map((t) => ({ label: t.name, value: t.code }));
  const typeName = (code: number) => typeOpts.find((o) => o.value === code)?.label;

  const [editing, setEditing] = useState<CustomerCommissionRule | null>(null);
  const [creating, setCreating] = useState(false);

  const create = customerCommissionRulesCrud.useCreate();
  const update = customerCommissionRulesCrud.useUpdate();
  const remove = customerCommissionRulesCrud.useRemove();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onSubmit(values: CustomerCommissionRuleForm) {
    await run(async () => {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: { ...values, customerType: editing.customerType } });
        setEditing(null);
      } else {
        await create.mutateAsync(values);
        setCreating(false);
      }
    }, editing ? 'Đã cập nhật' : 'Đã thêm quy tắc');
  }

  const resetFilters = () => {
    setCustomerType(undefined);
    setStatus(undefined);
    setPage(1);
  };

  const columns: ColumnsType<CustomerCommissionRule> = [
    {
      title: 'Loại khách',
      dataIndex: 'customerTypeName',
      key: 'customerTypeName',
      render: (v: string | null, r) => v ?? typeName(r.customerType) ?? `#${r.customerType}`,
    },
    { title: 'Hoa hồng (%)', dataIndex: 'percentage', key: 'percentage', width: 140, align: 'right', render: (v: number) => `${v}%` },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (v: number) => <Tag color={STATUS_COLOR[v] ?? 'default'}>{statusText(CUSTOMER_COMMISSION_STATUS, v)}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 160,
      render: (_: unknown, item: CustomerCommissionRule) =>
        canManage ? (
          <Space>
            <Button size="small" onClick={() => setEditing(item)}>
              Sửa
            </Button>
            <Popconfirm title="Xoá quy tắc này?" onConfirm={() => run(() => remove.mutateAsync(item.id), 'Đã xoá')}>
              <Button size="small" danger>
                Xoá
              </Button>
            </Popconfirm>
          </Space>
        ) : null,
    },
  ];

  const statusOpts = Object.entries(CUSTOMER_COMMISSION_STATUS).map(([v, label]) => ({ label, value: Number(v) }));

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Hoa hồng theo loại khách
        </Typography.Title>
        {canManage ? (
          <Button type="primary" onClick={() => setCreating(true)}>
            Thêm quy tắc
          </Button>
        ) : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Tổng quy tắc', value: stats.data?.total ?? 0, suffix: '' },
          { title: 'Đang áp dụng', value: stats.data?.active ?? 0, suffix: '' },
          { title: 'Tạm ngừng', value: stats.data?.inactive ?? 0, suffix: '' },
          { title: 'Tỉ lệ TB', value: stats.data?.avgPercentage ?? 0, suffix: '%' },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={12} lg={6} flex="1">
            <Card styles={{ body: { padding: 16 } }}>
              <Statistic title={c.title} value={c.value} suffix={c.suffix} loading={stats.isLoading} />
            </Card>
          </Col>
        ))}
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Loại khách"
              options={typeOpts} value={customerType} onChange={(v) => { setCustomerType(v ?? undefined); setPage(1); }} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Button onClick={resetFilters}>Đặt lại</Button>
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
          options={[{ label: `Tất cả (${stats.data?.total ?? 0})`, value: 'all' }, ...statusOpts.map((o) => ({ label: o.label, value: String(o.value) }))]}
        />
      </div>

      <Table
        rowKey="id"
        columns={columns}
        dataSource={list.data?.items ?? []}
        loading={list.isLoading}
        scroll={{ x: 'max-content' }}
        pagination={{ current: page, pageSize: size, total: list.data?.total ?? 0, onChange: setPage, showSizeChanger: false }}
      />

      {creating || editing ? (
        <CrudFormModal
          open
          title={editing ? 'Sửa quy tắc' : 'Thêm quy tắc'}
          schema={customerCommissionRuleFormSchema}
          defaultValues={
            editing
              ? { customerType: editing.customerType, percentage: editing.percentage, status: editing.status }
              : { customerType: typeOpts[0]?.value ?? 0, percentage: 0, status: 1 }
          }
          submitting={create.isPending || update.isPending}
          onCancel={() => {
            setCreating(false);
            setEditing(null);
          }}
          onSubmit={onSubmit}
        >
          <SelectField name="customerType" label="Loại khách" required options={typeOpts} />
          <NumberField name="percentage" label="Hoa hồng (%)" required />
          <SelectField name="status" label="Trạng thái" required options={statusOpts} />
        </CrudFormModal>
      ) : null}
    </>
  );
}
