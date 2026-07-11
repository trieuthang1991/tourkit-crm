import type { ColumnsType } from 'antd/es/table';
import { money } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { NumberField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { agentsCrud } from './agentsCrud';
import { agentFormSchema } from './types';
import type { Agent, AgentForm } from './types';

const columns: ColumnsType<Agent> = [
  { title: 'Mã', dataIndex: 'code', key: 'code' },
  { title: 'Tên đại lý', dataIndex: 'name', key: 'name' },
  { title: 'Liên hệ', dataIndex: 'contactPerson', key: 'contactPerson' },
  { title: 'Điện thoại', dataIndex: 'phone', key: 'phone' },
  { title: 'Hạn mức', dataIndex: 'creditLimit', key: 'creditLimit', render: (v: number) => money(v) },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function AgentsPage() {
  return (
    <ResourcePage<Agent, AgentForm>
      title="Đại lý (B2B)"
      columns={columns}
      crud={agentsCrud}
      perms={{ create: 'agent.manage', update: 'agent.manage', remove: 'agent.manage' }}
      toForm={(a) => ({
        code: a?.code ?? '',
        name: a?.name ?? '',
        contactPerson: a?.contactPerson ?? null,
        phone: a?.phone ?? null,
        email: a?.email ?? null,
        taxCode: a?.taxCode ?? null,
        address: a?.address ?? null,
        creditLimit: a?.creditLimit ?? 0,
        status: a?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa đại lý' : 'Thêm đại lý'}
          schema={agentFormSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="code" label="Mã" required />
          <TextField name="name" label="Tên đại lý" required />
          <TextField name="contactPerson" label="Người liên hệ" />
          <TextField name="phone" label="Điện thoại" />
          <TextField name="email" label="Email" />
          <TextField name="taxCode" label="Mã số thuế" />
          <TextField name="address" label="Địa chỉ" />
          <NumberField name="creditLimit" label="Hạn mức tín dụng" required />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
