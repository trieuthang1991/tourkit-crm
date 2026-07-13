import { App, Button, Card, Col, Input, Popconfirm, Row, Segmented, Select, Space, Statistic, Table, Tag, Typography } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField } from '../../shared/ui/Field';
import { commissionRulesCrud } from './commissionRulesCrud';
import { useCommissionRules, useCommissionRuleStats, useUserOptions } from './commissionRulesApi';
import type { CommissionRuleFilter } from './commissionRulesApi';
import { COMMISSION_RULE_STATUS, commissionRuleCreateSchema, commissionRuleUpdateSchema } from './commissionRuleTypes';
import type { CommissionRule, CommissionRuleForm } from './commissionRuleTypes';

const STATUS_COLOR: Record<number, string> = { 1: 'green', 0: 'default' };

export function CommissionRulesPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('commission.create');

  const [page, setPage] = useState(1);
  const size = 20;
  const [search, setSearch] = useState('');
  const [userId, setUserId] = useState<string | undefined>();
  const [status, setStatus] = useState<number | undefined>();
  const [filter, setFilter] = useState<CommissionRuleFilter>({});
  const applyFilters = () => {
    setFilter({ q: search || undefined, userId });
    setPage(1);
  };
  const resetFilters = () => {
    setSearch('');
    setUserId(undefined);
    setStatus(undefined);
    setFilter({});
    setPage(1);
  };
  const list = useCommissionRules(page, size, { ...filter, status });
  const stats = useCommissionRuleStats();
  const users = useUserOptions();
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName || u.email, value: u.id }));

  const [editing, setEditing] = useState<CommissionRule | null>(null);
  const [creating, setCreating] = useState(false);

  const create = commissionRulesCrud.useCreate();
  const update = commissionRulesCrud.useUpdate();
  const remove = commissionRulesCrud.useRemove();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  async function onSubmit(values: CommissionRuleForm) {
    await run(async () => {
      if (editing) {
        await update.mutateAsync({ id: editing.id, body: { percentage: values.percentage, status: values.status } });
        setEditing(null);
      } else {
        await create.mutateAsync({ userId: values.userId ?? '', percentage: values.percentage, status: values.status });
        setCreating(false);
      }
    }, editing ? 'Đã cập nhật' : 'Đã thêm quy tắc');
  }

  const columns: ColumnsType<CommissionRule> = [
    { title: 'Nhân viên', dataIndex: 'userName', key: 'userName', render: (v: string | null) => v ?? '—' },
    { title: 'Tỉ lệ (%)', dataIndex: 'percentage', key: 'percentage', width: 120, align: 'right', render: (v: number) => `${v}%` },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (v: number) => <Tag color={STATUS_COLOR[v] ?? 'default'}>{statusText(COMMISSION_RULE_STATUS, v)}</Tag>,
    },
    {
      title: '',
      key: '__actions',
      width: 160,
      render: (_: unknown, item: CommissionRule) =>
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

  const statusOpts = Object.entries(COMMISSION_RULE_STATUS).map(([v, label]) => ({ label, value: Number(v) }));

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <Typography.Title level={3} style={{ margin: 0 }}>
          Cấu hình hoa hồng
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
            <Input.Search allowClear placeholder="Tên nhân viên" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={6}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhân viên"
              options={userOpts} value={userId} onChange={(v) => setUserId(v ?? undefined)} />
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
          title={editing ? 'Sửa quy tắc hoa hồng' : 'Thêm quy tắc hoa hồng'}
          schema={editing ? commissionRuleUpdateSchema : commissionRuleCreateSchema}
          defaultValues={
            editing
              ? { percentage: editing.percentage, status: editing.status }
              : { userId: '', percentage: 0, status: 1 }
          }
          submitting={create.isPending || update.isPending}
          onCancel={() => {
            setCreating(false);
            setEditing(null);
          }}
          onSubmit={onSubmit}
        >
          {editing ? null : <SelectField name="userId" label="Nhân viên" required options={userOpts} />}
          <NumberField name="percentage" label="Tỉ lệ (%)" required />
          <SelectField name="status" label="Trạng thái" required options={statusOpts} />
        </CrudFormModal>
      ) : null}
    </>
  );
}
