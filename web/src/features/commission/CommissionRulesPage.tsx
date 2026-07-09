import type { ColumnsType } from 'antd/es/table';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { commissionRulesCrud } from './commissionRulesCrud';
import { commissionRuleCreateSchema, commissionRuleUpdateSchema } from './commissionRuleTypes';
import type { CommissionRule, CommissionRuleForm } from './commissionRuleTypes';

const columns: ColumnsType<CommissionRule> = [
  { title: 'ID người dùng', dataIndex: 'userId', key: 'userId' },
  { title: 'Tỉ lệ (%)', dataIndex: 'percentage', key: 'percentage' },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function CommissionRulesPage() {
  return (
    <ResourcePage<CommissionRule, CommissionRuleForm>
      title="Cấu hình hoa hồng"
      columns={columns}
      crud={commissionRulesCrud}
      perms={{ create: 'commission.create', update: 'commission.create', remove: 'commission.create' }}
      toForm={(r) => ({
        userId: r?.userId ?? '',
        percentage: r?.percentage ?? 0,
        status: r?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa quy tắc hoa hồng' : 'Thêm quy tắc hoa hồng'}
          schema={mode === 'edit' ? commissionRuleUpdateSchema : commissionRuleCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? <TextField name="userId" label="ID người dùng" required /> : null}
          <NumberField name="percentage" label="Tỉ lệ (%)" required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
