import { App, Button, DatePicker, Input, InputNumber, Modal, Select, Space, Typography } from 'antd';
import dayjs from 'dayjs';
import { useMemo, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { useAuth } from '../auth/AuthContext';

const templateRowSchema = z.object({ id: z.string().uuid(), title: z.string(), code: z.string() });

/// Mở hàng loạt chuyến từ 1 mẫu: startDate + số chuyến + khoảng cách (ngày) → sinh danh sách ngày ở FE,
/// gọi POST /tour-departures/batch. Tiện mở chuyến định kỳ (vd mỗi thứ 7 trong 3 tháng).
export function BatchDepartureButton() {
  const { has } = useAuth();
  const { message } = App.useApp();
  const qc = useQueryClient();
  const [open, setOpen] = useState(false);
  const [templateId, setTemplateId] = useState<string | undefined>();
  const [codePrefix, setCodePrefix] = useState('');
  const [totalSlots, setTotalSlots] = useState(0);
  const [startDate, setStartDate] = useState<string | null>(null);
  const [count, setCount] = useState(4);
  const [intervalDays, setIntervalDays] = useState(7);

  const templates = useQuery({
    queryKey: ['tourTemplates'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/tour-templates', { params: { page: 1, size: 200 } });
      return z.object({ items: z.array(templateRowSchema) }).parse(data).items;
    },
    enabled: open,
  });

  const templateOptions = useMemo(
    () => (templates.data ?? []).map((t) => ({ label: `${t.code} — ${t.title}`, value: t.id })),
    [templates.data],
  );

  const create = useMutation({
    mutationFn: async () => {
      if (!templateId || !startDate) throw new Error('Chọn mẫu và ngày bắt đầu');
      const start = dayjs(startDate);
      const items = Array.from({ length: count }, (_, i) => ({
        departureDate: start.add(i * intervalDays, 'day').toISOString(),
        endDate: null,
      }));
      const { data } = await httpClient.post<unknown>('/api/v1/tour-departures/batch', {
        templateId,
        codePrefix,
        title: null,
        totalSlots,
        items,
      });
      return z.object({ created: z.number() }).parse(data).created;
    },
    onSuccess: (created) => {
      qc.invalidateQueries({ queryKey: ['departures'] });
      message.success(`Đã mở ${created} chuyến`);
      setOpen(false);
    },
    onError: (e) => message.error(errorMessage(e)),
  });

  if (!has('departure.create')) return null;

  return (
    <>
      <Button style={{ marginBottom: 16 }} onClick={() => setOpen(true)}>
        Mở hàng loạt
      </Button>
      <Modal
        open={open}
        title="Mở hàng loạt chuyến từ mẫu"
        okText="Mở"
        okButtonProps={{ disabled: !templateId || !startDate || !codePrefix }}
        confirmLoading={create.isPending}
        onCancel={() => setOpen(false)}
        onOk={() => create.mutate()}
      >
        <Space direction="vertical" style={{ width: '100%' }}>
          <Select
            style={{ width: '100%' }}
            placeholder="Tour mẫu"
            showSearch
            optionFilterProp="label"
            loading={templates.isLoading}
            options={templateOptions}
            value={templateId}
            onChange={setTemplateId}
          />
          <Input placeholder="Tiền tố mã (vd TUAN)" value={codePrefix} onChange={(e) => setCodePrefix(e.target.value)} />
          <DatePicker
            style={{ width: '100%' }}
            placeholder="Ngày khởi hành đầu tiên"
            value={startDate ? dayjs(startDate) : null}
            onChange={(d) => setStartDate(d ? d.toISOString() : null)}
          />
          <Space>
            <Typography.Text>Số chuyến</Typography.Text>
            <InputNumber min={1} max={52} value={count} onChange={(v) => setCount(v ?? 1)} />
            <Typography.Text>Cách nhau (ngày)</Typography.Text>
            <InputNumber min={1} value={intervalDays} onChange={(v) => setIntervalDays(v ?? 1)} />
          </Space>
          <Space>
            <Typography.Text>Số chỗ (0 = theo mẫu)</Typography.Text>
            <InputNumber min={0} value={totalSlots} onChange={(v) => setTotalSlots(v ?? 0)} />
          </Space>
        </Space>
      </Modal>
    </>
  );
}
