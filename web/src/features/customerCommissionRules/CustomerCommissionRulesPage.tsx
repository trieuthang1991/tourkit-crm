import type { ColumnsType } from 'antd/es/table';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { customerCommissionRulesCrud } from './customerCommissionRulesCrud';
import { customerCommissionRuleFormSchema } from './types';
import type { CustomerCommissionRule, CustomerCommissionRuleForm } from './types';

const columns: ColumnsType<CustomerCommissionRule> = [
  { title: 'Loại khách', dataIndex: 'customerType', key: 'customerType' },
  { title: 'Hoa hồng (%)', dataIndex: 'percentage', key: 'percentage' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function CustomerCommissionRulesPage() {
  return (
    <ResourcePage<CustomerCommissionRule, CustomerCommissionRuleForm>
      title="Hoa hồng theo loại khách"
      columns={columns}
      crud={customerCommissionRulesCrud}
      perms={{ create: 'commission.create', update: 'commission.create', remove: 'commission.create' }}
      toForm={(r) => ({
        customerType: r?.customerType ?? 0,
        percentage: r?.percentage ?? 0,
        status: r?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa quy tắc' : 'Thêm quy tắc'}
          schema={customerCommissionRuleFormSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <NumberField name="customerType" label="Loại khách hàng" required />
          <NumberField name="percentage" label="Hoa hồng (%)" required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
