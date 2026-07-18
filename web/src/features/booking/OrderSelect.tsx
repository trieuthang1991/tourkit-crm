import { useEffect, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Select } from '../../shared/ui/antd';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { orderSchema } from './seatTypes';

/* Chọn đơn hàng bằng TÌM KIẾM server-side (theo mã/khách) — KHÔNG tải toàn bộ đơn.
   Value là order id (UUID lưu ngầm); người dùng gõ mã/khách để chọn, không phải gõ UUID. */

export function OrderSelect({
  value,
  onChange,
  placeholder = 'Tìm đơn theo mã / khách…',
}: {
  value?: string | null;
  onChange: (v: string | null) => void;
  placeholder?: string;
}) {
  const [raw, setRaw] = useState('');
  const [search, setSearch] = useState('');
  useEffect(() => {
    const t = setTimeout(() => setSearch(raw), 300); // debounce gõ tìm
    return () => clearTimeout(t);
  }, [raw]);

  const q = useQuery({
    queryKey: ['orders', 'search', search],
    queryFn: async () =>
      pagedSchema(orderSchema).parse(
        (await httpClient.get<unknown>('/api/v1/orders', { params: { page: 1, size: 20, q: search || undefined } })).data,
      ).items,
  });
  const options = (q.data ?? []).map((o) => ({ label: `${o.code}${o.customerName ? ` — ${o.customerName}` : ''}`, value: o.id }));

  return (
    <Select
      showSearch
      allowClear
      style={{ width: '100%' }}
      placeholder={placeholder}
      filterOption={false}
      onSearch={setRaw}
      loading={q.isFetching}
      options={options}
      value={value ?? undefined}
      onChange={(v) => onChange((v as string) ?? null)}
      notFoundContent={q.isFetching ? 'Đang tìm…' : 'Gõ mã đơn / tên khách để tìm'}
    />
  );
}
