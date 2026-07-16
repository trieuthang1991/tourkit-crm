import { App, Button, Card, Col, DatePicker, Input, Row, Segmented, Select, Space, Statistic, Tag, Typography } from '../../shared/ui/antd';
import dayjs from 'dayjs';
import type { Dayjs } from 'dayjs';
import { useMemo, useState } from 'react';
import { useAuth } from '../auth/AuthContext';
import { errorMessage } from '../../shared/api/problem';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { useProviderOptions } from '../flights/flightsApi';
import {
  useCreateRoomAllotment,
  useDeleteRoomAllotment,
  useRoomAllotments,
  useRoomAllotmentStats,
  useUpdateRoomAllotment,
} from './roomAllotmentApi';
import type { RoomAllotmentFilter } from './roomAllotmentApi';
import {
  DAY_TYPE_BG,
  DAY_TYPE_BORDER,
  DAY_TYPE_LABEL,
  DAY_TYPE_OPTIONS,
  DAY_TYPE_TAG_COLOR,
  roomAllotmentFormSchema,
} from './roomAllotmentTypes';
import type { RoomAllotment, RoomAllotmentForm } from './roomAllotmentTypes';

const DKEY = (d: Dayjs | string) => dayjs(d).format('YYYY-MM-DD');
const shortMoney = (v: number) => (v >= 1000 ? `${Math.round(v / 1000).toLocaleString('vi-VN')}k` : String(v));

const EMPTY_FORM: RoomAllotmentForm = {
  providerRef: '',
  serviceName: '',
  projectName: null,
  province: null,
  market: null,
  date: dayjs().startOf('day').toISOString(),
  dayType: 0,
  quota: 0,
  booked: 0,
  price: 0,
  rating: null,
  note: null,
};

type Group = {
  key: string;
  providerRef: string;
  providerName: string;
  serviceName: string;
  province: string | null;
  rating: number | null;
  cells: Map<string, RoomAllotment>;
};

export function RoomFundPage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('roomfund.manage');

  // --- bộ lọc ---
  const [search, setSearch] = useState('');
  const [province, setProvince] = useState('');
  const [market, setMarket] = useState('');
  const [providerRef, setProviderRef] = useState<string | undefined>();
  const [applied, setApplied] = useState<RoomAllotmentFilter>({});
  const applyFilters = () => {
    setApplied({ q: search || undefined, province: province || undefined, market: market || undefined, providerRef });
  };
  const resetFilters = () => {
    setSearch('');
    setProvince('');
    setMarket('');
    setProviderRef(undefined);
    setApplied({});
  };

  // --- khoảng ngày (cột lịch) ---
  const [rangeStart, setRangeStart] = useState<Dayjs>(dayjs().startOf('day'));
  const [days, setDays] = useState(7);
  const dayList = useMemo(() => Array.from({ length: days }, (_, i) => rangeStart.add(i, 'day')), [rangeStart, days]);

  const rangeFilter: RoomAllotmentFilter = {
    ...applied,
    dateFrom: rangeStart.toISOString(),
    dateTo: rangeStart.add(days - 1, 'day').endOf('day').toISOString(),
  };
  const list = useRoomAllotments(1, 3000, rangeFilter);
  const stats = useRoomAllotmentStats(rangeFilter);
  const providers = useProviderOptions();
  const providerOpts = (providers.data ?? []).map((p) => ({ label: p.name, value: p.id }));

  const groups = useMemo<Group[]>(() => {
    const map = new Map<string, Group>();
    for (const a of list.data?.items ?? []) {
      const key = `${a.providerRef}|${a.serviceName}`;
      let g = map.get(key);
      if (!g) {
        g = {
          key,
          providerRef: a.providerRef,
          providerName: a.providerName ?? a.providerRef,
          serviceName: a.serviceName,
          province: a.province,
          rating: a.rating,
          cells: new Map(),
        };
        map.set(key, g);
      }
      g.cells.set(DKEY(a.date), a);
    }
    return [...map.values()];
  }, [list.data]);

  // --- chế độ: vận hành (click sửa) vs tính giá combo (click chọn) ---
  const [mode, setMode] = useState<'ops' | 'combo'>('ops');
  const [combo, setCombo] = useState<Map<string, RoomAllotment>>(new Map());
  const comboItems = [...combo.values()];
  const comboTotal = comboItems.reduce((s, a) => s + a.price, 0);
  const toggleCombo = (a: RoomAllotment) => {
    setCombo((prev) => {
      const next = new Map(prev);
      if (next.has(a.id)) next.delete(a.id);
      else next.set(a.id, a);
      return next;
    });
  };

  // --- CRUD ---
  const [editingId, setEditingId] = useState<string | 'new' | null>(null);
  const [prefill, setPrefill] = useState<Partial<RoomAllotmentForm> | null>(null);
  const editing = (list.data?.items ?? []).find((a) => a.id === editingId) ?? null;
  const isEdit = editingId !== null && editingId !== 'new';

  const create = useCreateRoomAllotment();
  const update = useUpdateRoomAllotment();
  const remove = useDeleteRoomAllotment();

  async function run(action: () => Promise<unknown>, ok: string) {
    try {
      await action();
      message.success(ok);
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  const openCell = (g: Group, d: Dayjs, cell?: RoomAllotment) => {
    if (mode === 'combo') {
      if (cell) toggleCombo(cell);
      return;
    }
    if (!canManage) return;
    if (cell) {
      setPrefill(null);
      setEditingId(cell.id);
    } else {
      setPrefill({
        providerRef: g.providerRef,
        serviceName: g.serviceName,
        province: g.province,
        date: d.startOf('day').toISOString(),
        dayType: d.day() === 0 || d.day() === 6 ? 1 : 0,
      });
      setEditingId('new');
    }
  };

  async function onSubmit(values: RoomAllotmentForm) {
    await run(async () => {
      if (isEdit) await update.mutateAsync({ id: editingId as string, body: values });
      else await create.mutateAsync(values);
      setEditingId(null);
      setPrefill(null);
    }, 'Đã lưu quỹ phòng');
  }

  const defaultValues: RoomAllotmentForm =
    isEdit && editing
      ? {
          providerRef: editing.providerRef,
          serviceName: editing.serviceName,
          projectName: editing.projectName,
          province: editing.province,
          market: editing.market,
          date: editing.date,
          dayType: editing.dayType,
          quota: editing.quota,
          booked: editing.booked,
          price: editing.price,
          rating: editing.rating,
          note: editing.note,
        }
      : { ...EMPTY_FORM, ...prefill };

  const s = stats.data;
  const th: React.CSSProperties = { padding: '6px 8px', borderBottom: '1px solid #f0f0f0', fontWeight: 600, fontSize: 12, whiteSpace: 'nowrap' };
  const stickyLeft: React.CSSProperties = { position: 'sticky', left: 0, background: '#fff', zIndex: 2, boxShadow: '2px 0 4px -2px rgba(0,0,0,0.12)' };

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 16 }}>
        <div>
          <Typography.Title level={3} style={{ margin: 0 }}>
            Quỹ phòng khách sạn
          </Typography.Title>
          <Typography.Text type="secondary">Lịch tồn (allotment) + giá NET theo ngày — mỗi ô tô màu theo loại ngày.</Typography.Text>
        </div>
        {canManage ? (
          <Button type="primary" onClick={() => { setPrefill(null); setEditingId('new'); }}>Tạo mới</Button>
        ) : null}
      </div>

      <Row gutter={[12, 12]} style={{ marginBottom: 16 }}>
        {[
          { title: 'Số ô lịch', value: s?.cells ?? 0 },
          { title: 'Số NCC', value: s?.providers ?? 0 },
          { title: 'Tổng tồn', value: s?.totalQuota ?? 0 },
          { title: 'Đã đặt', value: s?.totalBooked ?? 0 },
          { title: 'Còn lại', value: s?.totalAvailable ?? 0 },
        ].map((c) => (
          <Col key={c.title} xs={12} sm={8} lg={4} flex="1">
            <Card styles={{ body: { padding: 12 } }}>
              <Statistic title={c.title} value={c.value} loading={stats.isLoading} valueStyle={{ fontSize: 20 }} />
            </Card>
          </Col>
        ))}
        <Col xs={24} lg={4} flex="1">
          <Card styles={{ body: { padding: 12 } }}>
            <div style={{ fontSize: 12, color: '#888', marginBottom: 6 }}>Loại ngày</div>
            <Space size={[4, 4]} wrap>
              {DAY_TYPE_OPTIONS.map((o) => (
                <Tag key={o.value} color={DAY_TYPE_TAG_COLOR[o.value]} style={{ marginInlineEnd: 0 }}>{o.label}</Tag>
              ))}
            </Space>
          </Card>
        </Col>
      </Row>

      <Card size="small" style={{ marginBottom: 12 }}>
        <Row gutter={[12, 12]}>
          <Col xs={24} sm={12} lg={7}>
            <Input.Search allowClear placeholder="Dịch vụ / dự án / tỉnh thành" value={search} onChange={(e) => setSearch(e.target.value)} onSearch={applyFilters} />
          </Col>
          <Col xs={24} sm={12} lg={5}>
            <Input allowClear placeholder="Tỉnh thành" value={province} onChange={(e) => setProvince(e.target.value)} />
          </Col>
          <Col xs={24} sm={12} lg={4}>
            <Input allowClear placeholder="Thị trường" value={market} onChange={(e) => setMarket(e.target.value)} />
          </Col>
          <Col xs={24} sm={12} lg={5}>
            <Select showSearch allowClear optionFilterProp="label" style={{ width: '100%' }} placeholder="Nhà cung cấp" options={providerOpts} value={providerRef} onChange={(v) => setProviderRef(v ?? undefined)} />
          </Col>
          <Col span={24}>
            <Space>
              <Button type="primary" onClick={applyFilters}>Tìm kiếm</Button>
              <Button onClick={resetFilters}>Đặt lại</Button>
            </Space>
          </Col>
        </Row>
      </Card>

      <Space wrap style={{ marginBottom: 12 }}>
        <Button onClick={() => setRangeStart((d) => d.subtract(days, 'day'))}>‹ Trước</Button>
        <Button type="primary" ghost onClick={() => setRangeStart(dayjs().startOf('day'))}>Hôm nay</Button>
        <Button onClick={() => setRangeStart((d) => d.add(days, 'day'))}>Tiếp ›</Button>
        <DatePicker allowClear={false} format="DD/MM/YYYY" value={rangeStart} onChange={(d) => d && setRangeStart(d.startOf('day'))} />
        <Segmented
          value={String(days)}
          onChange={(v) => setDays(Number(v))}
          options={[{ label: '7 ngày', value: '7' }, { label: '14 ngày', value: '14' }, { label: '30 ngày', value: '30' }]}
        />
        <Segmented
          value={mode}
          onChange={(v) => setMode(v as 'ops' | 'combo')}
          options={[{ label: 'Vận hành', value: 'ops' }, { label: 'Tính giá combo', value: 'combo' }]}
        />
      </Space>

      {mode === 'combo' ? (
        <Card size="small" style={{ marginBottom: 12, background: '#fafafa' }}>
          <Space wrap align="center">
            <Typography.Text strong>Tính giá combo nhanh:</Typography.Text>
            <Typography.Text type="secondary">Bấm các ô để chọn dịch vụ → cộng giá NET.</Typography.Text>
            <Tag color="blue">{comboItems.length} dịch vụ</Tag>
            <Typography.Text strong style={{ fontSize: 16 }}>Tổng NET: {money(comboTotal)}</Typography.Text>
            {comboItems.length > 0 ? <Button size="small" onClick={() => setCombo(new Map())}>Xoá chọn</Button> : null}
          </Space>
        </Card>
      ) : null}

      <div style={{ overflowX: 'auto', border: '1px solid #f0f0f0', borderRadius: 8, background: '#fff' }}>
        <table style={{ borderCollapse: 'collapse', width: '100%', minWidth: 160 + days * 88 }}>
          <thead>
            <tr>
              <th style={{ ...th, ...stickyLeft, textAlign: 'left', minWidth: 200 }}>Nhà cung cấp / Dịch vụ</th>
              {dayList.map((d) => {
                const weekend = d.day() === 0 || d.day() === 6;
                const today = d.isSame(dayjs(), 'day');
                return (
                  <th key={DKEY(d)} style={{ ...th, textAlign: 'center', minWidth: 80, color: weekend ? '#cf1322' : undefined, background: today ? '#e6f4ff' : undefined }}>
                    {d.format('DD/MM')}
                    <div style={{ fontWeight: 400, color: '#999' }}>{['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'][d.day()]}</div>
                  </th>
                );
              })}
            </tr>
          </thead>
          <tbody>
            {groups.length === 0 ? (
              <tr>
                <td colSpan={days + 1} style={{ padding: 32, textAlign: 'center', color: '#999' }}>
                  {list.isLoading ? 'Đang tải…' : 'Chưa có quỹ phòng trong khoảng ngày này.'}
                </td>
              </tr>
            ) : (
              groups.map((g) => (
                <tr key={g.key}>
                  <td style={{ ...stickyLeft, ...th, textAlign: 'left', verticalAlign: 'top' }}>
                    <div style={{ fontWeight: 600 }}>{g.providerName}</div>
                    <div style={{ fontSize: 12, color: '#888' }}>{g.serviceName}</div>
                    {g.province ? <div style={{ fontSize: 11, color: '#aaa' }}>{g.province}{g.rating ? ` · ${g.rating}★` : ''}</div> : null}
                  </td>
                  {dayList.map((d) => {
                    const cell = g.cells.get(DKEY(d));
                    const selected = cell ? combo.has(cell.id) : false;
                    return (
                      <td
                        key={DKEY(d)}
                        onClick={() => openCell(g, d, cell)}
                        style={{
                          borderBottom: '1px solid #f0f0f0',
                          borderLeft: '1px solid #f5f5f5',
                          padding: 4,
                          textAlign: 'center',
                          verticalAlign: 'top',
                          cursor: cell ? 'pointer' : canManage && mode === 'ops' ? 'pointer' : 'default',
                          background: cell ? DAY_TYPE_BG[cell.dayType] : undefined,
                          outline: selected ? '2px solid #1677ff' : undefined,
                          outlineOffset: -2,
                        }}
                        title={cell ? `${DAY_TYPE_LABEL[cell.dayType]} · ${money(cell.price)} · còn ${cell.available}/${cell.quota}` : 'Trống — bấm để thêm'}
                      >
                        {cell ? (
                          <div style={{ borderLeft: `3px solid ${DAY_TYPE_BORDER[cell.dayType]}`, paddingLeft: 4, textAlign: 'left', lineHeight: 1.3 }}>
                            <div style={{ fontWeight: 600, fontSize: 12 }}>{shortMoney(cell.price)}</div>
                            <div style={{ fontSize: 11, color: cell.available <= 0 ? '#cf1322' : '#389e0d' }}>còn {cell.available}/{cell.quota}</div>
                          </div>
                        ) : (
                          <span style={{ color: '#e0e0e0', fontSize: 12 }}>·</span>
                        )}
                      </td>
                    );
                  })}
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {editingId !== null ? (
        <CrudFormModal
          open
          title={isEdit ? 'Sửa ô quỹ phòng' : 'Thêm ô quỹ phòng'}
          schema={roomAllotmentFormSchema}
          defaultValues={defaultValues}
          submitting={create.isPending || update.isPending}
          onCancel={() => { setEditingId(null); setPrefill(null); }}
          onSubmit={onSubmit}
          width={560}
        >
          <SelectField name="providerRef" label="Nhà cung cấp" options={providerOpts} required showSearch />
          <TextField name="serviceName" label="Dịch vụ / loại phòng" required placeholder="VD: Deluxe Twin" />
          <TextField name="projectName" label="Tên dự án" />
          <TextField name="province" label="Tỉnh thành" />
          <TextField name="market" label="Thị trường" />
          <DatePickerField name="date" label="Ngày áp dụng" required />
          <SelectField name="dayType" label="Loại ngày" options={DAY_TYPE_OPTIONS} />
          <NumberField name="quota" label="Tồn (số phòng)" />
          <NumberField name="booked" label="Đã đặt" />
          <NumberField name="price" label="Giá NET/đêm" />
          <NumberField name="rating" label="Hạng sao" />
          <TextAreaField name="note" label="Ghi chú" />
          {isEdit && canManage ? (
            <Button danger onClick={() => run(async () => { await remove.mutateAsync(editingId as string); setEditingId(null); }, 'Đã xoá')}>
              Xoá ô này
            </Button>
          ) : null}
        </CrudFormModal>
      ) : null}
    </>
  );
}
