import { App, Button } from 'antd';
import type { ColumnsType, ColumnType } from 'antd/es/table';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { errorMessage } from '../../shared/api/problem';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { SelectField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { useAuth } from '../auth/AuthContext';
import { leadsCrud } from './leadsCrud';
import { LEAD_STATUS, leadCreateSchema, leadUpdateSchema } from './types';
import type { Lead, LeadForm } from './types';

const LEAD_STATUS_OPTIONS = Object.entries(LEAD_STATUS).map(([value, label]) => ({
  value: Number(value),
  label,
}));

function ConvertButton({ lead }: { lead: Lead }) {
  const { has } = useAuth();
  const { message } = App.useApp();
  const qc = useQueryClient();
  const convert = useMutation({
    mutationFn: async (id: string) => {
      const { data } = await httpClient.post<{ customerId: string }>(`/api/v1/leads/${id}/convert`);
      return data;
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['leads'] });
      qc.invalidateQueries({ queryKey: ['customers'] });
    },
  });

  if (!has('lead.convert') || lead.convertedCustomerId !== null) {
    return null;
  }

  return (
    <Button
      size="small"
      loading={convert.isPending}
      onClick={async () => {
        try {
          await convert.mutateAsync(lead.id);
          message.success('Đã chuyển thành khách hàng');
        } catch (e) {
          message.error(errorMessage(e));
        }
      }}
    >
      Chuyển thành KH
    </Button>
  );
}

const convertColumn: ColumnType<Lead> = {
  title: '',
  key: '__convert',
  width: 160,
  render: (_: unknown, lead: Lead) => <ConvertButton lead={lead} />,
};

const columns: ColumnsType<Lead> = [
  { title: 'Họ tên', dataIndex: 'fullName', key: 'fullName' },
  { title: 'Điện thoại', dataIndex: 'phone', key: 'phone' },
  { title: 'Email', dataIndex: 'email', key: 'email' },
  { title: 'Nguồn', dataIndex: 'source', key: 'source' },
  {
    title: 'Trạng thái',
    dataIndex: 'status',
    key: 'status',
    render: (status: number) => statusText(LEAD_STATUS, status),
  },
  convertColumn,
];

export function LeadsPage() {
  return (
    <ResourcePage<Lead, LeadForm>
      title="Lead (CRM)"
      columns={columns}
      crud={leadsCrud}
      perms={{ create: 'lead.create', update: 'lead.update', remove: 'lead.delete' }}
      toForm={(l) => ({
        fullName: l?.fullName ?? '',
        phone: l?.phone ?? null,
        email: l?.email ?? null,
        source: l?.source ?? null,
        assignedToUserId: l?.assignedToUserId ?? null,
        status: l?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa lead' : 'Thêm lead'}
          schema={mode === 'edit' ? leadUpdateSchema : leadCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="fullName" label="Họ tên" required />
          <TextField name="phone" label="Điện thoại" />
          <TextField name="email" label="Email" />
          <TextField name="source" label="Nguồn" />
          <TextField name="assignedToUserId" label="Người phụ trách (userId)" />
          {mode === 'edit' ? (
            <SelectField name="status" label="Trạng thái" options={LEAD_STATUS_OPTIONS} required />
          ) : null}
        </CrudFormModal>
      )}
    />
  );
}
