import type { ColumnsType } from 'antd/es/table';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { vehiclesCrud } from './vehiclesCrud';
import { vehicleCreateSchema, vehicleUpdateSchema } from './vehicleTypes';
import type { Vehicle, VehicleForm } from './vehicleTypes';

const columns: ColumnsType<Vehicle> = [
  { title: 'Tên xe', dataIndex: 'name', key: 'name' },
  { title: 'Hãng', dataIndex: 'firmName', key: 'firmName' },
  { title: 'Số chỗ', dataIndex: 'seatType', key: 'seatType' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function VehiclesPage() {
  return (
    <ResourcePage<Vehicle, VehicleForm>
      title="Xe"
      columns={columns}
      crud={vehiclesCrud}
      perms={{ create: 'vehicle.manage', update: 'vehicle.manage', remove: 'vehicle.manage' }}
      toForm={(v) => ({
        name: v?.name ?? '',
        firmName: v?.firmName ?? null,
        seatType: v?.seatType ?? 0,
        status: v?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa xe' : 'Thêm xe'}
          schema={mode === 'edit' ? vehicleUpdateSchema : vehicleCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="name" label="Tên xe" required />
          <TextField name="firmName" label="Hãng" />
          <NumberField name="seatType" label="Số chỗ" required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
