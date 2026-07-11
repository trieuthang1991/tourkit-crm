import type { ColumnsType } from 'antd/es/table';
import { dateText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { DatePickerField, NumberField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { guideAssignmentsCrud } from './guideAssignmentsCrud';
import { guideAssignmentCreateSchema, guideAssignmentUpdateSchema } from './guideAssignmentTypes';
import type { GuideAssignment, GuideAssignmentForm } from './guideAssignmentTypes';

const columns: ColumnsType<GuideAssignment> = [
  { title: 'Chuyến (departureId)', dataIndex: 'tourDepartureId', key: 'tourDepartureId' },
  { title: 'HDV (providerId)', dataIndex: 'providerId', key: 'providerId' },
  {
    title: 'Giờ đi',
    dataIndex: 'timeGo',
    key: 'timeGo',
    render: (v: string | null) => dateText(v),
  },
  { title: 'Trạng thái', dataIndex: 'status', key: 'status' },
];

export function GuideAssignmentsPage() {
  return (
    <ResourcePage<GuideAssignment, GuideAssignmentForm>
      title="Phân công HDV"
      columns={columns}
      crud={guideAssignmentsCrud}
      perms={{ create: 'guide.manage', update: 'guide.manage', remove: 'guide.manage' }}
      toForm={(g) => ({
        tourDepartureId: g?.tourDepartureId ?? '',
        providerId: g?.providerId ?? '',
        timeGo: g?.timeGo ?? null,
        timeCome: g?.timeCome ?? null,
        timeReturn: g?.timeReturn ?? null,
        note: g?.note ?? null,
        status: g?.status ?? 1,
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa phân công HDV' : 'Thêm phân công HDV'}
          schema={mode === 'edit' ? guideAssignmentUpdateSchema : guideAssignmentCreateSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          {mode === 'create' ? (
            <TextField name="tourDepartureId" label="Mã chuyến (departureId)" required />
          ) : null}
          <TextField name="providerId" label="Mã HDV (providerId)" required />
          <DatePickerField name="timeGo" label="Giờ đi" />
          <DatePickerField name="timeCome" label="Giờ về" />
          <DatePickerField name="timeReturn" label="Giờ trả tour" />
          <TextAreaField name="note" label="Ghi chú" />
          <NumberField name="status" label="Trạng thái" required />
        </CrudFormModal>
      )}
    />
  );
}
