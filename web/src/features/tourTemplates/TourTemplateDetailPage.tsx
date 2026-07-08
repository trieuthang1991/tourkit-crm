import { App, Button, Input, InputNumber, Select, Space, Table, Tabs } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import {
  ASSIGNEE_ROLE,
  useAssignees,
  useItinerary,
  usePriceScenarios,
  usePutAssignees,
  usePutItinerary,
  usePutPriceScenarios,
  useTourTemplate,
} from './api/tourDetailApi';
import type { Assignee, ItineraryDay, PriceScenario } from './api/tourDetailApi';

const ASSIGNEE_ROLE_OPTIONS = Object.entries(ASSIGNEE_ROLE).map(([value, label]) => ({
  value: Number(value),
  label,
}));

type ItineraryRow = { key: string; dayIndex: number; title: string; detail: string | null };
type PriceScenarioRow = { key: string; fromQty: number; toQty: number; unitPrice: number };
type AssigneeRow = { key: string; userId: string; role: number };

function newKey(): string {
  return crypto.randomUUID();
}

function ItineraryTab({ templateId }: { templateId: string }) {
  const { message } = App.useApp();
  const { data, isLoading } = useItinerary(templateId);
  const putItinerary = usePutItinerary(templateId);
  const [rows, setRows] = useState<ItineraryRow[]>([]);

  useEffect(() => {
    if (data) {
      setRows(data.map((d: ItineraryDay) => ({ key: d.id, dayIndex: d.dayIndex, title: d.title, detail: d.detail })));
    }
  }, [data]);

  function updateRow(key: string, patch: Partial<ItineraryRow>) {
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  }

  function addRow() {
    setRows((prev) => [...prev, { key: newKey(), dayIndex: prev.length + 1, title: '', detail: null }]);
  }

  function removeRow(key: string) {
    setRows((prev) => prev.filter((r) => r.key !== key));
  }

  async function save() {
    try {
      await putItinerary.mutateAsync(
        rows.map((r) => ({ dayIndex: r.dayIndex, title: r.title, detail: r.detail ? r.detail : null })),
      );
      message.success('Đã lưu lịch trình');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<ItineraryRow> = [
    {
      title: 'Ngày',
      dataIndex: 'dayIndex',
      key: 'dayIndex',
      width: 100,
      render: (_: unknown, row) => (
        <InputNumber
          style={{ width: '100%' }}
          value={row.dayIndex}
          onChange={(v) => updateRow(row.key, { dayIndex: v ?? 0 })}
        />
      ),
    },
    {
      title: 'Tiêu đề',
      dataIndex: 'title',
      key: 'title',
      render: (_: unknown, row) => (
        <Input value={row.title} onChange={(e) => updateRow(row.key, { title: e.target.value })} />
      ),
    },
    {
      title: 'Chi tiết',
      dataIndex: 'detail',
      key: 'detail',
      render: (_: unknown, row) => (
        <Input
          value={row.detail ?? ''}
          onChange={(e) => updateRow(row.key, { detail: e.target.value })}
        />
      ),
    },
    {
      title: '',
      key: '__remove',
      width: 90,
      render: (_: unknown, row) => (
        <Button size="small" danger onClick={() => removeRow(row.key)}>
          Xoá
        </Button>
      ),
    },
  ];

  return (
    <>
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={addRow}>Thêm dòng</Button>
        <Button type="primary" onClick={save} loading={putItinerary.isPending}>
          Lưu
        </Button>
      </Space>
      <Table<ItineraryRow> rowKey="key" columns={columns} dataSource={rows} loading={isLoading} pagination={false} />
    </>
  );
}

function PriceScenariosTab({ templateId }: { templateId: string }) {
  const { message } = App.useApp();
  const { data, isLoading } = usePriceScenarios(templateId);
  const putPriceScenarios = usePutPriceScenarios(templateId);
  const [rows, setRows] = useState<PriceScenarioRow[]>([]);

  useEffect(() => {
    if (data) {
      setRows(
        data.map((p: PriceScenario) => ({ key: p.id, fromQty: p.fromQty, toQty: p.toQty, unitPrice: p.unitPrice })),
      );
    }
  }, [data]);

  function updateRow(key: string, patch: Partial<PriceScenarioRow>) {
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  }

  function addRow() {
    setRows((prev) => [...prev, { key: newKey(), fromQty: 0, toQty: 0, unitPrice: 0 }]);
  }

  function removeRow(key: string) {
    setRows((prev) => prev.filter((r) => r.key !== key));
  }

  async function save() {
    try {
      await putPriceScenarios.mutateAsync(
        rows.map((r) => ({ fromQty: r.fromQty, toQty: r.toQty, unitPrice: r.unitPrice })),
      );
      message.success('Đã lưu bảng giá');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<PriceScenarioRow> = [
    {
      title: 'Từ số lượng',
      dataIndex: 'fromQty',
      key: 'fromQty',
      render: (_: unknown, row) => (
        <InputNumber
          style={{ width: '100%' }}
          value={row.fromQty}
          onChange={(v) => updateRow(row.key, { fromQty: v ?? 0 })}
        />
      ),
    },
    {
      title: 'Đến số lượng',
      dataIndex: 'toQty',
      key: 'toQty',
      render: (_: unknown, row) => (
        <InputNumber
          style={{ width: '100%' }}
          value={row.toQty}
          onChange={(v) => updateRow(row.key, { toQty: v ?? 0 })}
        />
      ),
    },
    {
      title: 'Đơn giá',
      dataIndex: 'unitPrice',
      key: 'unitPrice',
      render: (_: unknown, row) => (
        <InputNumber
          style={{ width: '100%' }}
          value={row.unitPrice}
          onChange={(v) => updateRow(row.key, { unitPrice: v ?? 0 })}
        />
      ),
    },
    {
      title: '',
      key: '__remove',
      width: 90,
      render: (_: unknown, row) => (
        <Button size="small" danger onClick={() => removeRow(row.key)}>
          Xoá
        </Button>
      ),
    },
  ];

  return (
    <>
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={addRow}>Thêm dòng</Button>
        <Button type="primary" onClick={save} loading={putPriceScenarios.isPending}>
          Lưu
        </Button>
      </Space>
      <Table<PriceScenarioRow>
        rowKey="key"
        columns={columns}
        dataSource={rows}
        loading={isLoading}
        pagination={false}
      />
    </>
  );
}

function AssigneesTab({ tourId }: { tourId: string }) {
  const { message } = App.useApp();
  const { data, isLoading } = useAssignees(tourId);
  const putAssignees = usePutAssignees(tourId);
  const [rows, setRows] = useState<AssigneeRow[]>([]);

  useEffect(() => {
    if (data) {
      setRows(data.map((a: Assignee) => ({ key: a.id, userId: a.userId, role: a.role })));
    }
  }, [data]);

  function updateRow(key: string, patch: Partial<AssigneeRow>) {
    setRows((prev) => prev.map((r) => (r.key === key ? { ...r, ...patch } : r)));
  }

  function addRow() {
    setRows((prev) => [...prev, { key: newKey(), userId: '', role: 3 }]);
  }

  function removeRow(key: string) {
    setRows((prev) => prev.filter((r) => r.key !== key));
  }

  async function save() {
    try {
      await putAssignees.mutateAsync(rows.map((r) => ({ userId: r.userId, role: r.role })));
      message.success('Đã lưu phân công');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<AssigneeRow> = [
    {
      title: 'User ID',
      dataIndex: 'userId',
      key: 'userId',
      render: (_: unknown, row) => (
        <Input value={row.userId} onChange={(e) => updateRow(row.key, { userId: e.target.value })} />
      ),
    },
    {
      title: 'Vai trò',
      dataIndex: 'role',
      key: 'role',
      width: 200,
      render: (_: unknown, row) => (
        <Select
          style={{ width: '100%' }}
          value={row.role}
          options={ASSIGNEE_ROLE_OPTIONS}
          onChange={(v) => updateRow(row.key, { role: v })}
        />
      ),
    },
    {
      title: '',
      key: '__remove',
      width: 90,
      render: (_: unknown, row) => (
        <Button size="small" danger onClick={() => removeRow(row.key)}>
          Xoá
        </Button>
      ),
    },
  ];

  return (
    <>
      <Space style={{ marginBottom: 16 }}>
        <Button onClick={addRow}>Thêm dòng</Button>
        <Button type="primary" onClick={save} loading={putAssignees.isPending}>
          Lưu
        </Button>
      </Space>
      <Table<AssigneeRow> rowKey="key" columns={columns} dataSource={rows} loading={isLoading} pagination={false} />
    </>
  );
}

export function TourTemplateDetailPage() {
  const { id } = useParams<{ id: string }>();
  const templateId = id ?? '';
  const { data: template } = useTourTemplate(templateId);

  return (
    <>
      <PageHeader title={template ? `${template.code} — ${template.title}` : 'Tour mẫu'} />
      <Tabs
        items={[
          { key: 'itinerary', label: 'Lịch trình', children: <ItineraryTab templateId={templateId} /> },
          {
            key: 'price-scenarios',
            label: 'Bảng giá theo số lượng',
            children: <PriceScenariosTab templateId={templateId} />,
          },
          { key: 'assignees', label: 'Phân công', children: <AssigneesTab tourId={templateId} /> },
        ]}
      />
    </>
  );
}
