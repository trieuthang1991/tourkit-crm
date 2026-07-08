import type { ColumnsType } from 'antd/es/table';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { customersCrud } from './customersCrud';
import { customerFormSchema } from './types';
import type { Customer, CustomerForm } from './types';

const columns: ColumnsType<Customer> = [
  { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
  { title: 'Điện thoại', dataIndex: 'phone', key: 'phone' },
];

export function CustomersPage() {
  return (
    <ResourcePage<Customer, CustomerForm>
      title="Khách hàng"
      columns={columns}
      crud={customersCrud}
      perms={{ create: 'customer.create', update: 'customer.update', remove: 'customer.delete' }}
      toForm={(c) => ({ fullName: c?.fullName ?? '', phone: c?.phone ?? null })}
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
        </CrudFormModal>
      )}
    />
  );
}
