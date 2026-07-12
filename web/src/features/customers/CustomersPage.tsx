import { Tag } from 'antd';
import type { ColumnsType } from 'antd/es/table';
import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { customersCrud } from './customersCrud';
import { CUSTOMER_TYPE_OPTIONS, GENDER_OPTIONS, customerFormSchema, customerTypeLabel } from './types';
import type { Customer, CustomerForm } from './types';

const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });

const dash = (v: string | null | undefined) => (v ? v : '—');
const dateVi = (v: string | null) => (v ? new Date(v).toLocaleDateString('vi-VN') : '—');

// Cột bám danh sách Data khách hàng hệ cũ (/customer-data).
const columns: ColumnsType<Customer> = [
  { title: 'Mã KH', dataIndex: 'code', key: 'code', width: 130, fixed: 'left', render: dash },
  { title: 'Họ và tên', dataIndex: 'fullName', key: 'fullName', width: 180, fixed: 'left' },
  { title: 'Số điện thoại', dataIndex: 'phone', key: 'phone', width: 120, render: dash },
  { title: 'Email', dataIndex: 'email', key: 'email', width: 170, render: dash },
  { title: 'Tỉnh thành', dataIndex: 'city', key: 'city', width: 120, render: dash },
  {
    title: 'Phân nhóm',
    dataIndex: 'segments',
    key: 'segments',
    width: 200,
    render: (v: string[]) => (v.length ? v.map((s) => <Tag key={s}>{s}</Tag>) : '—'),
  },
  { title: 'Ngày sinh', dataIndex: 'dateOfBirth', key: 'dateOfBirth', width: 110, render: dateVi },
  { title: 'Ngày tạo', dataIndex: 'createdAt', key: 'createdAt', width: 110, render: dateVi },
  {
    title: 'CSKH gần nhất',
    key: 'lastCare',
    width: 180,
    render: (_: unknown, c: Customer) =>
      c.lastCareAt ? (
        <span>
          {dateVi(c.lastCareAt)}
          {c.lastCareContent ? <div style={{ fontSize: 12, color: '#999' }}>{c.lastCareContent}</div> : null}
        </span>
      ) : (
        '—'
      ),
  },
  { title: 'Số lần mua', dataIndex: 'purchaseCount', key: 'purchaseCount', width: 100, align: 'center' },
  { title: 'Doanh thu', dataIndex: 'revenue', key: 'revenue', width: 130, align: 'right', render: (v: number) => money(v) },
  { title: 'Loại KH', dataIndex: 'customerType', key: 'customerType', width: 110, render: (v: number) => customerTypeLabel(v) },
  {
    title: 'Người tạo',
    key: 'createdBy',
    width: 140,
    render: (_: unknown, c: Customer) => c.createdByName ?? dash(c.createdBy),
  },
  {
    title: 'NV phụ trách',
    dataIndex: 'assignedToNames',
    key: 'assignedTo',
    width: 160,
    render: (v: string[]) => (v.length ? v.map((n) => <Tag key={n}>{n}</Tag>) : '—'),
  },
  {
    title: 'Thẻ',
    dataIndex: 'tags',
    key: 'tags',
    width: 140,
    render: (v: string[]) => (v.length ? v.map((t) => <Tag key={t} color="blue">{t}</Tag>) : '—'),
  },
];

function AssignedToField() {
  const list = useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
  const options = (list.data ?? []).map((u) => ({ label: u.fullName, value: u.id }));
  return <SelectField name="assignedTo" label="NV phụ trách" options={options} mode="multiple" />;
}

export function CustomersPage() {
  return (
    <ResourcePage<Customer, CustomerForm>
      title="Data khách hàng"
      columns={columns}
      crud={customersCrud}
      perms={{ create: 'customer.create', update: 'customer.update', remove: 'customer.delete' }}
      toForm={(c) => ({
        fullName: c?.fullName ?? '',
        phone: c?.phone ?? null,
        customerType: c?.customerType ?? 0,
        source: c?.source ?? null,
        tag: c?.tag ?? null,
        tempBalance: c?.tempBalance ?? 0,
        email: c?.email ?? null,
        address: c?.address ?? null,
        dateOfBirth: c?.dateOfBirth ?? null,
        idCardNumber: c?.idCardNumber ?? null,
        passportNumber: c?.passportNumber ?? null,
        passportExpiry: c?.passportExpiry ?? null,
        nationality: c?.nationality ?? null,
        gender: c?.gender ?? null,
        city: c?.city ?? null,
        marketGroup: c?.marketGroup ?? null,
        initialNeed: c?.initialNeed ?? null,
        collaboratorName: c?.collaboratorName ?? null,
        campaign: c?.campaign ?? null,
        segments: c?.segments ?? [],
        tags: c?.tags ?? [],
        assignedTo: c?.assignedTo ?? [],
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa khách hàng' : 'Thêm khách hàng'}
          schema={customerFormSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="fullName" label="Họ tên" required />
          <TextField name="phone" label="Điện thoại" />
          <SelectField name="customerType" label="Loại khách hàng" options={CUSTOMER_TYPE_OPTIONS} required />
          <SelectField name="gender" label="Giới tính" options={GENDER_OPTIONS} allowClear />
          <TextField name="email" label="Email" />
          <TextField name="city" label="Tỉnh thành" />
          <TextField name="address" label="Địa chỉ" />
          <DatePickerField name="dateOfBirth" label="Ngày sinh" />
          <SelectField name="segments" label="Phân nhóm" options={[]} mode="tags" />
          <SelectField name="tags" label="Thẻ KH" options={[]} mode="tags" />
          <AssignedToField />
          <TextField name="source" label="Nguồn khách" />
          <TextField name="marketGroup" label="Nhóm/Thị trường" />
          <TextField name="campaign" label="Chiến dịch" />
          <TextField name="collaboratorName" label="CTV" />
          <TextAreaField name="initialNeed" label="Nhu cầu ban đầu" />
          <NumberField name="tempBalance" label="Tạm ứng" required />
          <TextField name="idCardNumber" label="CMND/CCCD" />
          <TextField name="nationality" label="Quốc tịch" />
          <TextField name="passportNumber" label="Số hộ chiếu" />
          <DatePickerField name="passportExpiry" label="Hộ chiếu hết hạn" />
        </CrudFormModal>
      )}
    />
  );
}
