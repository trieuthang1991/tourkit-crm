import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { paymentTermSchema } from '../paymentTerms/types';
import { providersCrud } from './providersCrud';
import { PROVIDER_TYPE, providerCreateSchema, providerUpdateSchema } from './types';
import type { Provider, ProviderForm } from './types';

const PROVIDER_TYPE_OPTIONS = Object.entries(PROVIDER_TYPE).map(([value, label]) => ({
  value: Number(value),
  label,
}));

function PaymentTermField() {
  const list = useQuery({
    queryKey: ['payment-terms'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/payment-terms');
      return z.array(paymentTermSchema).parse(data);
    },
  });
  const options = (list.data ?? []).map((t) => ({ label: t.name, value: t.id }));
  return <SelectField name="paymentTermId" label="Điều khoản thanh toán" options={options} allowClear />;
}

const columns: ColumnsType<Provider> = [
  { title: 'Mã', dataIndex: 'code', key: 'code' },
  { title: 'Tên', dataIndex: 'name', key: 'name' },
  {
    title: 'Loại',
    dataIndex: 'type',
    key: 'type',
    render: (type: number) => statusText(PROVIDER_TYPE, type),
  },
  { title: 'Điện thoại', dataIndex: 'phone', key: 'phone' },
  { title: 'Tỉ lệ', dataIndex: 'rate', key: 'rate' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function ProvidersPage() {
  return (
    <ResourcePage<Provider, ProviderForm>
      title="Nhà cung cấp"
      columns={columns}
      crud={providersCrud}
      perms={{ create: 'provider.create', update: 'provider.update', remove: 'provider.delete' }}
      toForm={(p) => ({
        code: p?.code ?? '',
        name: p?.name ?? '',
        type: p?.type ?? 1,
        phone: p?.phone ?? null,
        email: p?.email ?? null,
        address: p?.address ?? null,
        taxCode: p?.taxCode ?? null,
        contactPerson: p?.contactPerson ?? null,
        bankAccount: p?.bankAccount ?? null,
        bankName: p?.bankName ?? null,
        paymentTermId: p?.paymentTermId ?? null,
        rate: p?.rate ?? 0,
        status: p?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa nhà cung cấp' : 'Thêm nhà cung cấp'}
          schema={mode === 'edit' ? providerUpdateSchema : providerCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? <TextField name="code" label="Mã" required /> : null}
          <TextField name="name" label="Tên" required />
          <SelectField name="type" label="Loại" options={PROVIDER_TYPE_OPTIONS} required />
          <TextField name="phone" label="Điện thoại" />
          <TextField name="email" label="Email" />
          <TextField name="address" label="Địa chỉ" />
          <TextField name="taxCode" label="Mã số thuế" />
          <TextField name="contactPerson" label="Người liên hệ" />
          <TextField name="bankAccount" label="Số tài khoản" />
          <TextField name="bankName" label="Ngân hàng" />
          <PaymentTermField />
          <NumberField name="rate" label="Tỉ lệ" required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
