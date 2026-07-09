import type { ColumnsType } from 'antd/es/table';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, SelectField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { serviceItemsCrud } from './serviceItemsCrud';
import { SERVICE_CATEGORY, serviceItemCreateSchema, serviceItemUpdateSchema } from './serviceItemTypes';
import type { ServiceItem, ServiceItemForm } from './serviceItemTypes';

const SERVICE_CATEGORY_OPTIONS = Object.entries(SERVICE_CATEGORY).map(([value, label]) => ({
  value: Number(value),
  label,
}));

const columns: ColumnsType<ServiceItem> = [
  { title: 'Mã', dataIndex: 'code', key: 'code' },
  { title: 'Tên', dataIndex: 'name', key: 'name' },
  {
    title: 'Loại',
    dataIndex: 'category',
    key: 'category',
    render: (category: number) => statusText(SERVICE_CATEGORY, category),
  },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function ServiceItemsPage() {
  return (
    <ResourcePage<ServiceItem, ServiceItemForm>
      title="Danh mục dịch vụ"
      columns={columns}
      crud={serviceItemsCrud}
      perms={{ create: 'service.manage', update: 'service.manage', remove: 'service.manage' }}
      toForm={(s) => ({
        code: s?.code ?? '',
        name: s?.name ?? '',
        category: s?.category ?? 1,
        status: s?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa dịch vụ' : 'Thêm dịch vụ'}
          schema={mode === 'edit' ? serviceItemUpdateSchema : serviceItemCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? <TextField name="code" label="Mã" required /> : null}
          <TextField name="name" label="Tên" required />
          <SelectField name="category" label="Loại" options={SERVICE_CATEGORY_OPTIONS} required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
