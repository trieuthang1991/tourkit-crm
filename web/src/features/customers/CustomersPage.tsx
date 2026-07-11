import type { ColumnsType } from 'antd/es/table';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { customersCrud } from './customersCrud';
import { customerFormSchema } from './types';
import type { Customer, CustomerForm } from './types';

const columns: ColumnsType<Customer> = [
  { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
  { title: 'Điện thoại', dataIndex: 'phone', key: 'phone' },
  { title: 'Loại KH', dataIndex: 'customerType', key: 'customerType' },
  { title: 'Nguồn', dataIndex: 'source', key: 'source' },
  { title: 'Nhãn', dataIndex: 'tag', key: 'tag' },
  { title: 'Tạm ứng', dataIndex: 'tempBalance', key: 'tempBalance', render: (v: number) => money(v) },
];

export function CustomersPage() {
  return (
    <ResourcePage<Customer, CustomerForm>
      title="Khách hàng"
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
          <NumberField name="customerType" label="Loại khách hàng" required />
          <TextField name="source" label="Nguồn" />
          <TextField name="tag" label="Nhãn" />
          <NumberField name="tempBalance" label="Tạm ứng" required />
          <TextField name="email" label="Email" />
          <TextField name="address" label="Địa chỉ" />
          <DatePickerField name="dateOfBirth" label="Ngày sinh" />
          <TextField name="idCardNumber" label="CMND/CCCD" />
          <TextField name="nationality" label="Quốc tịch" />
          <TextField name="passportNumber" label="Số hộ chiếu" />
          <DatePickerField name="passportExpiry" label="Hộ chiếu hết hạn" />
        </CrudFormModal>
      )}
    />
  );
}
