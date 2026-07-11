import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { currencySchema } from '../currencies/types';
import { providersCrud } from '../providers/providersCrud';
import { providerServicesCrud } from './providerServicesCrud';
import { providerServiceCreateSchema, providerServiceUpdateSchema } from './providerServiceTypes';
import type { ProviderService, ProviderServiceForm } from './providerServiceTypes';
import { serviceItemsCrud } from './serviceItemsCrud';

const columns: ColumnsType<ProviderService> = [
  { title: 'NCC', dataIndex: 'providerId', key: 'providerId' },
  { title: 'Tên gói giá', dataIndex: 'priceName', key: 'priceName' },
  {
    title: 'Giá hợp đồng',
    key: 'contractPrice',
    render: (_: unknown, r: ProviderService) =>
      r.currencyCode && r.currencyCode !== 'VND'
        ? `${r.contractPrice.toLocaleString('vi-VN')} ${r.currencyCode} = ${money(r.contractPriceVnd)}`
        : money(r.contractPriceVnd),
  },
  { title: 'Giá công bố', key: 'publicPrice', render: (_: unknown, r: ProviderService) => money(r.publicPriceVnd) },
  { title: 'Tiền tệ', dataIndex: 'currencyCode', key: 'currencyCode', render: (v: string | null) => v ?? 'VND' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

// Options fetched via hooks — must live in their own components (rendered as JSX children of
// CrudFormModal) rather than called as plain functions, so the hook calls don't land in
// ResourcePage's own conditional render branch.
function ProviderIdField() {
  const list = providersCrud.useList({ page: 1, size: 200 });
  const options = (list.data?.items ?? []).map((p) => ({ label: p.name, value: p.id }));
  return <SelectField name="providerId" label="Nhà cung cấp" options={options} required />;
}

function ServiceItemIdField() {
  const list = serviceItemsCrud.useList({ page: 1, size: 200 });
  const options = (list.data?.items ?? []).map((s) => ({ label: s.name, value: s.id }));
  return <SelectField name="serviceItemId" label="Dịch vụ" options={options} allowClear />;
}

function CurrencyCodeField() {
  const list = useQuery({
    queryKey: ['currencies'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/currencies');
      return z.array(currencySchema).parse(data);
    },
  });
  // Mặc định VND; chọn ngoại tệ để nhập giá vốn theo tiền tệ đó (hệ quy đổi VND theo tỷ giá).
  const options = [
    { label: 'VND', value: 'VND' },
    ...(list.data ?? []).filter((c) => c.code !== 'VND').map((c) => ({ label: `${c.code} — ${c.name}`, value: c.code })),
  ];
  return <SelectField name="currencyCode" label="Tiền tệ giá vốn" options={options} allowClear />;
}

export function ProviderServicesPage() {
  return (
    <ResourcePage<ProviderService, ProviderServiceForm>
      title="Bảng giá NCC"
      columns={columns}
      crud={providerServicesCrud}
      perms={{ create: 'service.manage', update: 'service.manage', remove: 'service.manage' }}
      toForm={(p) => ({
        providerId: p?.providerId ?? '',
        serviceItemId: p?.serviceItemId ?? null,
        priceName: p?.priceName ?? null,
        contractPrice: p?.contractPrice ?? 0,
        publicPrice: p?.publicPrice ?? 0,
        currencyCode: p?.currencyCode ?? null,
        amountOfPeople: p?.amountOfPeople ?? 0,
        note: p?.note ?? null,
        status: p?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa bảng giá' : 'Thêm bảng giá'}
          schema={mode === 'edit' ? providerServiceUpdateSchema : providerServiceCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? <ProviderIdField /> : null}
          <ServiceItemIdField />
          <TextField name="priceName" label="Tên gói giá" />
          <NumberField name="contractPrice" label="Giá hợp đồng" required />
          <NumberField name="publicPrice" label="Giá công bố" required />
          <CurrencyCodeField />
          <NumberField name="amountOfPeople" label="Số người" required />
          <TextAreaField name="note" label="Ghi chú" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
