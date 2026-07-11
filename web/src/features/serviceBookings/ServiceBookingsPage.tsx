import type { ColumnsType } from 'antd/es/table';
import { dateText, money, statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { serviceBookingsCrud } from './serviceBookingsCrud';
import { SERVICE_BOOKING_TYPE, serviceBookingFormSchema } from './types';
import type { ServiceBooking, ServiceBookingForm } from './types';

const TYPE_OPTIONS = Object.entries(SERVICE_BOOKING_TYPE).map(([value, label]) => ({
  value: Number(value),
  label,
}));

const columns: ColumnsType<ServiceBooking> = [
  { title: 'Mã', dataIndex: 'code', key: 'code' },
  { title: 'Loại', dataIndex: 'type', key: 'type', render: (v: number) => statusText(SERVICE_BOOKING_TYPE, v) },
  { title: 'Mô tả', dataIndex: 'description', key: 'description' },
  { title: 'Từ', dataIndex: 'startDate', key: 'startDate', render: (v: string | null) => dateText(v) },
  { title: 'SL', dataIndex: 'quantity', key: 'quantity' },
  { title: 'Thành tiền', dataIndex: 'totalAmount', key: 'totalAmount', render: (v: number) => money(v) },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function ServiceBookingsPage() {
  return (
    <ResourcePage<ServiceBooking, ServiceBookingForm>
      title="Đặt dịch vụ lẻ"
      columns={columns}
      crud={serviceBookingsCrud}
      perms={{ create: 'servicebooking.manage', update: 'servicebooking.manage', remove: 'servicebooking.manage' }}
      toForm={(s) => ({
        code: s?.code ?? '',
        type: s?.type ?? 1,
        orderId: s?.orderId ?? null,
        providerId: s?.providerId ?? null,
        description: s?.description ?? '',
        startDate: s?.startDate ?? null,
        endDate: s?.endDate ?? null,
        quantity: s?.quantity ?? 1,
        unitPrice: s?.unitPrice ?? 0,
        status: s?.status ?? 0,
        note: s?.note ?? null,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa đặt dịch vụ' : 'Thêm đặt dịch vụ'}
          schema={serviceBookingFormSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="code" label="Mã" />
          <SelectField name="type" label="Loại" options={TYPE_OPTIONS} required />
          <TextField name="description" label="Mô tả" required />
          <TextField name="orderId" label="Mã đơn (orderId)" />
          <TextField name="providerId" label="Mã NCC (providerId)" />
          <DatePickerField name="startDate" label="Ngày bắt đầu" />
          <DatePickerField name="endDate" label="Ngày kết thúc" />
          <NumberField name="quantity" label="Số lượng" required />
          <NumberField name="unitPrice" label="Đơn giá" required />
          <NumberField name="status" label="Trạng thái" required />
          <TextAreaField name="note" label="Ghi chú" />
        </CrudFormModal>
      )}
    />
  );
}
