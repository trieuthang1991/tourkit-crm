import type { ColumnsType } from 'antd/es/table';
import { dateText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { vehicleAssignmentsCrud } from './vehicleAssignmentsCrud';
import { vehicleAssignmentCreateSchema, vehicleAssignmentUpdateSchema } from './vehicleAssignmentTypes';
import type { VehicleAssignment, VehicleAssignmentForm } from './vehicleAssignmentTypes';

const columns: ColumnsType<VehicleAssignment> = [
  { title: 'Chuyến (departureId)', dataIndex: 'tourDepartureId', key: 'tourDepartureId' },
  { title: 'Xe (vehicleId)', dataIndex: 'vehicleId', key: 'vehicleId' },
  { title: 'Tài xế', dataIndex: 'driverName', key: 'driverName' },
  { title: 'SĐT tài xế', dataIndex: 'driverPhone', key: 'driverPhone' },
  {
    title: 'Giờ đón',
    dataIndex: 'timeGo',
    key: 'timeGo',
    render: (v: string | null) => dateText(v),
  },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function VehicleAssignmentsPage() {
  return (
    <ResourcePage<VehicleAssignment, VehicleAssignmentForm>
      title="Phân xe cho chuyến"
      columns={columns}
      crud={vehicleAssignmentsCrud}
      perms={{ create: 'vehicle.manage', update: 'vehicle.manage', remove: 'vehicle.manage' }}
      toForm={(v) => ({
        tourDepartureId: v?.tourDepartureId ?? '',
        vehicleId: v?.vehicleId ?? '',
        driverName: v?.driverName ?? null,
        driverPhone: v?.driverPhone ?? null,
        timeGo: v?.timeGo ?? null,
        timeCome: v?.timeCome ?? null,
        note: v?.note ?? null,
        status: v?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa phân xe' : 'Thêm phân xe'}
          schema={mode === 'edit' ? vehicleAssignmentUpdateSchema : vehicleAssignmentCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? (
            <TextField name="tourDepartureId" label="Mã chuyến (departureId)" required />
          ) : null}
          <TextField name="vehicleId" label="Mã xe (vehicleId)" required />
          <TextField name="driverName" label="Tài xế" />
          <TextField name="driverPhone" label="SĐT tài xế" />
          <DatePickerField name="timeGo" label="Giờ đón" />
          <DatePickerField name="timeCome" label="Giờ trả" />
          <TextAreaField name="note" label="Ghi chú" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
