import type { ColumnsType } from 'antd/es/table';
import { dateText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { customerCaresCrud } from './customerCaresCrud';
import { customerCareCreateSchema, customerCareUpdateSchema } from './customerCareTypes';
import type { CustomerCare, CustomerCareForm } from './customerCareTypes';

const columns: ColumnsType<CustomerCare> = [
  { title: 'Khách hàng', dataIndex: 'customerId', key: 'customerId' },
  { title: 'Tiêu đề', dataIndex: 'title', key: 'title' },
  {
    title: 'Nhắc hẹn',
    dataIndex: 'remindAt',
    key: 'remindAt',
    render: (v: string | null) => dateText(v),
  },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function CustomerCaresPage() {
  return (
    <ResourcePage<CustomerCare, CustomerCareForm>
      title="Chăm sóc khách hàng"
      columns={columns}
      crud={customerCaresCrud}
      perms={{ create: 'care.manage', update: 'care.manage', remove: 'care.manage' }}
      toForm={(c) => ({
        customerId: c?.customerId ?? '',
        title: c?.title ?? '',
        detail: c?.detail ?? null,
        remindAt: c?.remindAt ?? null,
        feedback: c?.feedback ?? null,
        assignedToUserId: c?.assignedToUserId ?? null,
        status: c?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa chăm sóc khách hàng' : 'Thêm chăm sóc khách hàng'}
          schema={mode === 'edit' ? customerCareUpdateSchema : customerCareCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? <TextField name="customerId" label="Mã khách hàng" required /> : null}
          <TextField name="title" label="Tiêu đề" required />
          <TextAreaField name="detail" label="Nội dung" />
          <DatePickerField name="remindAt" label="Nhắc hẹn" />
          <TextField name="assignedToUserId" label="Người phụ trách (userId)" />
          <NumberField name="status" label="Trạng thái" required />
          {mode === 'edit' ? <TextAreaField name="feedback" label="Phản hồi" /> : null}
        </CrudFormModal>
      )}
    />
  );
}
