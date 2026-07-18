import { App, Button, Drawer, Popconfirm, Space, Table, Tag } from '../../shared/ui/antd';
import type { ColumnsType } from '../../shared/ui/antd';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { z } from 'zod';
import dayjs from 'dayjs';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { CellDate, CellStack, CellText, DataCard } from '../../shared/ui';
import { DateRangeInput, NumberInput, Select } from '../../ui/inputs';
import { Icon } from '../../ui/kit';
import { useAuth } from '../auth/AuthContext';
import { useUserOptions } from './commissionRulesApi';

/* Màn "Chính sách hoa hồng (bậc thang)" (/commission-campaigns) — chính sách hoa hồng
   theo bậc lợi nhuận, áp dụng cho danh sách nhân viên trong khoảng thời gian.
   Hệ Refined (shim shared/ui/antd). */

const campaignListItemSchema = z.object({
  id: z.string(),
  name: z.string(),
  startDate: z.string().nullable().optional(),
  endDate: z.string().nullable().optional(),
  status: z.number(),
  userCount: z.number(),
  tierCount: z.number(),
});
type CampaignListItem = z.infer<typeof campaignListItemSchema>;

const tierSchema = z.object({
  id: z.string().optional(),
  startAmount: z.number(),
  endAmount: z.number(),
  percentage: z.number(),
});

const campaignDetailSchema = z.object({
  id: z.string(),
  name: z.string(),
  startDate: z.string().nullable().optional(),
  endDate: z.string().nullable().optional(),
  status: z.number(),
  userIds: z.array(z.string()).default([]),
  userNames: z.array(z.string()).default([]),
  tiers: z.array(tierSchema).default([]),
});

const CAMPAIGNS_KEY = ['commission-campaigns'];

// 0 = đang áp dụng, 1 = ngừng
const STATUS_LABEL: Record<number, string> = { 0: 'Đang áp dụng', 1: 'Ngừng' };
const STATUS_COLOR: Record<number, string> = { 0: 'green', 1: 'default' };
const STATUS_OPTS = [
  { label: 'Đang áp dụng', value: 0 },
  { label: 'Ngừng', value: 1 },
];

type TierRow = { startAmount: number | null; endAmount: number | null; percentage: number | null };

function useCampaigns() {
  return useQuery({
    queryKey: CAMPAIGNS_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/commission-campaigns');
      return z.array(campaignListItemSchema).parse(data);
    },
  });
}

function useCampaignDetail(id: string | null) {
  return useQuery({
    queryKey: [...CAMPAIGNS_KEY, 'detail', id],
    enabled: !!id,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/commission-campaigns/${id}`);
      return campaignDetailSchema.parse(data);
    },
  });
}

function useDeleteCampaign() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/commission-campaigns/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: CAMPAIGNS_KEY }),
  });
}

const fmtDate = (iso: string | null | undefined) => (iso ? dayjs(iso).format('DD/MM/YYYY') : '—');

/** Drawer soạn chính sách: tên + khoảng ngày + NV áp dụng + trạng thái + bảng bậc lợi nhuận. */
function CampaignEditorDrawer({ campaignId, onClose }: { campaignId: string | null; onClose: () => void }) {
  const { message } = App.useApp();
  const qc = useQueryClient();
  const isEdit = !!campaignId;

  const users = useUserOptions();
  const userOpts = (users.data ?? []).map((u) => ({ label: u.fullName || u.email, value: u.id }));
  const detail = useCampaignDetail(campaignId);

  const [name, setName] = useState('');
  const [startDate, setStartDate] = useState<string | undefined>();
  const [endDate, setEndDate] = useState<string | undefined>();
  const [userIds, setUserIds] = useState<string[]>([]);
  const [status, setStatus] = useState<number>(0);
  const [tiers, setTiers] = useState<TierRow[]>([{ startAmount: 0, endAmount: null, percentage: null }]);
  const [saving, setSaving] = useState(false);

  // Nạp dữ liệu khi mở form sửa (sau khi detail về).
  useEffect(() => {
    if (isEdit && detail.data) {
      const d = detail.data;
      setName(d.name);
      setStartDate(d.startDate ?? undefined);
      setEndDate(d.endDate ?? undefined);
      setUserIds(d.userIds ?? []);
      setStatus(d.status);
      setTiers(
        (d.tiers ?? []).length
          ? d.tiers.map((t) => ({ startAmount: t.startAmount, endAmount: t.endAmount, percentage: t.percentage }))
          : [{ startAmount: 0, endAmount: null, percentage: null }],
      );
    }
  }, [isEdit, detail.data]);

  const addTier = () => setTiers((rows) => [...rows, { startAmount: null, endAmount: null, percentage: null }]);
  const removeTier = (idx: number) => setTiers((rows) => (rows.length > 1 ? rows.filter((_, i) => i !== idx) : rows));
  const setTier = (idx: number, patch: Partial<TierRow>) =>
    setTiers((rows) => rows.map((r, i) => (i === idx ? { ...r, ...patch } : r)));

  async function save() {
    if (!name.trim()) {
      message.error('Nhập tên chính sách');
      return;
    }
    if (!startDate || !endDate) {
      message.error('Chọn khoảng thời gian áp dụng');
      return;
    }
    if (!userIds.length) {
      message.error('Chọn ít nhất một nhân viên áp dụng');
      return;
    }
    const cleanTiers = tiers
      .filter((t) => t.startAmount != null || t.endAmount != null || t.percentage != null)
      .map((t) => ({
        startAmount: Number(t.startAmount ?? 0),
        endAmount: Number(t.endAmount ?? 0),
        percentage: Number(t.percentage ?? 0),
      }));
    if (!cleanTiers.length) {
      message.error('Thêm ít nhất một bậc lợi nhuận');
      return;
    }
    const body = { name: name.trim(), startDate, endDate, status, userIds, tiers: cleanTiers };
    setSaving(true);
    try {
      if (isEdit) {
        await httpClient.put(`/api/v1/commission-campaigns/${campaignId}`, body);
      } else {
        await httpClient.post('/api/v1/commission-campaigns', body);
      }
      message.success('Đã lưu chính sách hoa hồng');
      qc.invalidateQueries({ queryKey: CAMPAIGNS_KEY });
      onClose();
    } catch (e) {
      // Backend trả message tiếng Việt cho lỗi trùng khoảng ngày / chồng lấn.
      message.error(errorMessage(e));
    } finally {
      setSaving(false);
    }
  }

  const loading = isEdit && detail.isLoading;
  const labelCls = 'mb-1.5 block text-[12.5px] font-medium text-[var(--tk-heading)]';

  return (
    <Drawer
      open
      width={680}
      title={isEdit ? 'Sửa chính sách hoa hồng' : 'Thêm chính sách hoa hồng'}
      onClose={onClose}
      footer={
        <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 8 }}>
          <Button onClick={onClose}>Huỷ</Button>
          <Button type="primary" loading={saving} onClick={save}>
            Lưu
          </Button>
        </div>
      }
    >
      {loading ? (
        <div style={{ padding: 24, textAlign: 'center', color: 'var(--tk-muted)' }}>Đang tải…</div>
      ) : (
        <div style={{ display: 'flex', flexDirection: 'column', gap: 16 }}>
          <div>
            <label className={labelCls}>
              Tên chính sách <span className="text-[var(--tk-danger)]">*</span>
            </label>
            <div className="rf-field">
              <input value={name} onChange={(e) => setName(e.target.value)} placeholder="Nhập tên chính sách" />
            </div>
          </div>

          <div>
            <label className={labelCls}>
              Khoảng thời gian áp dụng <span className="text-[var(--tk-danger)]">*</span>
            </label>
            <DateRangeInput
              from={startDate}
              to={endDate}
              placeholder={['Từ ngày', 'đến']}
              onChange={(f, t) => {
                setStartDate(f);
                setEndDate(t);
              }}
            />
          </div>

          <div>
            <label className={labelCls}>
              Nhân viên áp dụng <span className="text-[var(--tk-danger)]">*</span>
            </label>
            <Select
              multiple
              showSearch
              allowClear
              placeholder="Chọn nhân viên"
              options={userOpts}
              value={userIds}
              onChange={(v) => setUserIds((Array.isArray(v) ? v : []) as string[])}
              style={{ width: '100%' }}
            />
            {userIds.length ? (
              <div className="mt-1 text-[12px] text-[var(--tk-muted-2)]">{userIds.length} nhân viên được chọn</div>
            ) : null}
          </div>

          <div>
            <label className={labelCls}>Trạng thái</label>
            <Select
              options={STATUS_OPTS}
              value={status}
              onChange={(v) => setStatus(Number(v))}
              style={{ width: 220 }}
            />
          </div>

          <div>
            <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: 8 }}>
              <label className={labelCls} style={{ margin: 0 }}>
                Bậc lợi nhuận <span className="text-[var(--tk-danger)]">*</span>
              </label>
              <Button size="small" onClick={addTier}>
                <span style={{ display: 'inline-flex', alignItems: 'center', gap: 4 }}>
                  <Icon name="add" size={16} /> Thêm bậc
                </span>
              </Button>
            </div>
            <div className="rf-card" style={{ padding: 0, overflow: 'hidden' }}>
              <div
                style={{
                  display: 'grid',
                  gridTemplateColumns: '1fr 1fr 120px 40px',
                  gap: 8,
                  padding: '8px 12px',
                  borderBottom: '1px solid var(--tk-line)',
                  fontSize: 12,
                  fontWeight: 600,
                  color: 'var(--tk-heading)',
                }}
              >
                <span>Từ (doanh số)</span>
                <span>Đến (doanh số)</span>
                <span>% hoa hồng</span>
                <span />
              </div>
              {tiers.map((row, idx) => (
                <div
                  key={idx}
                  style={{
                    display: 'grid',
                    gridTemplateColumns: '1fr 1fr 120px 40px',
                    gap: 8,
                    padding: '8px 12px',
                    alignItems: 'center',
                    borderBottom: idx < tiers.length - 1 ? '1px solid var(--tk-line)' : undefined,
                  }}
                >
                  <NumberInput value={row.startAmount} min={0} placeholder="0" onChange={(v) => setTier(idx, { startAmount: v })} />
                  <NumberInput value={row.endAmount} min={0} placeholder="0" onChange={(v) => setTier(idx, { endAmount: v })} />
                  <NumberInput value={row.percentage} min={0} placeholder="0" onChange={(v) => setTier(idx, { percentage: v })} />
                  <button
                    type="button"
                    onClick={() => removeTier(idx)}
                    disabled={tiers.length <= 1}
                    title="Xoá bậc"
                    style={{
                      border: 'none',
                      background: 'transparent',
                      cursor: tiers.length <= 1 ? 'not-allowed' : 'pointer',
                      color: tiers.length <= 1 ? 'var(--tk-muted)' : 'var(--tk-danger)',
                      opacity: tiers.length <= 1 ? 0.4 : 1,
                      display: 'inline-flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}
                  >
                    <Icon name="delete" size={18} />
                  </button>
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </Drawer>
  );
}

export function CommissionCampaignsPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('commission.create');

  const campaigns = useCampaigns();
  const remove = useDeleteCampaign();
  const [editorId, setEditorId] = useState<string | null | undefined>(undefined); // undefined = đóng, null = thêm mới

  async function handleDelete(id: string) {
    try {
      await remove.mutateAsync(id);
      message.success('Đã xoá chính sách');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const columns: ColumnsType<CampaignListItem> = [
    { title: 'Tên chính sách', key: 'name', render: (_: unknown, r: CampaignListItem) => <CellText tone="heading" strong>{r.name}</CellText> },
    {
      title: 'Khoảng ngày',
      key: 'range',
      width: 220,
      render: (_: unknown, r: CampaignListItem) => <CellDate value={`${fmtDate(r.startDate)} → ${fmtDate(r.endDate)}`} />,
    },
    { title: 'Số NV', dataIndex: 'userCount', key: 'userCount', width: 110, render: (v: number) => <CellStack align="right" main={(v ?? 0).toLocaleString('vi-VN')} mono /> },
    { title: 'Số bậc', dataIndex: 'tierCount', key: 'tierCount', width: 110, render: (v: number) => <CellStack align="right" main={(v ?? 0).toLocaleString('vi-VN')} mono /> },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 150,
      render: (v: number) => <Tag color={STATUS_COLOR[v] ?? 'default'}>{STATUS_LABEL[v] ?? String(v)}</Tag>,
    },
  ];

  const tableColumns: ColumnsType<CampaignListItem> = canManage
    ? [
        ...columns,
        {
          title: '',
          key: '__actions',
          width: 160,
          render: (_: unknown, r: CampaignListItem) => (
            <Space>
              <Button size="small" onClick={() => setEditorId(r.id)}>
                Sửa
              </Button>
              <Popconfirm title="Xoá chính sách này?" onConfirm={() => handleDelete(r.id)}>
                <Button size="small" danger loading={remove.isPending}>
                  Xoá
                </Button>
              </Popconfirm>
            </Space>
          ),
        },
      ]
    : columns;

  return (
    <>
      <PageHeader
        title="Chính sách hoa hồng (bậc thang)"
        extra={
          canManage ? (
            <Button type="primary" onClick={() => setEditorId(null)}>
              Thêm chính sách
            </Button>
          ) : null
        }
      />
      <DataCard title="Chính sách hoa hồng">
        <Table
          rowKey="id"
          columns={tableColumns}
          dataSource={campaigns.data ?? []}
          loading={campaigns.isLoading}
          scroll={{ x: 'max-content' }}
          pagination={false}
        />
      </DataCard>

      {editorId !== undefined ? <CampaignEditorDrawer campaignId={editorId} onClose={() => setEditorId(undefined)} /> : null}
    </>
  );
}
