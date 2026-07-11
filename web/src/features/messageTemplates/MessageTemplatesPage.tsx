import type { ColumnsType } from 'antd/es/table';
import { statusText } from '../../shared/format';
import { CrudFormModal } from '../../shared/ui/CrudFormModal';
import { SelectField, TextAreaField, TextField } from '../../shared/ui/Field';
import { ResourcePage } from '../../shared/ui/ResourcePage';
import { messageTemplatesCrud } from './messageTemplatesCrud';
import { TEMPLATE_CHANNEL, messageTemplateFormSchema } from './types';
import type { MessageTemplate, MessageTemplateForm } from './types';

const CHANNEL_OPTIONS = Object.entries(TEMPLATE_CHANNEL).map(([value, label]) => ({ value: Number(value), label }));

const columns: ColumnsType<MessageTemplate> = [
  { title: 'Tên mẫu', dataIndex: 'name', key: 'name' },
  { title: 'Kênh', dataIndex: 'channel', key: 'channel', render: (v: number) => statusText(TEMPLATE_CHANNEL, v) },
  { title: 'Tiêu đề', dataIndex: 'subject', key: 'subject', render: (v: string | null) => v ?? '—' },
];

export function MessageTemplatesPage() {
  return (
    <ResourcePage<MessageTemplate, MessageTemplateForm>
      title="Mẫu tin nhắn"
      columns={columns}
      crud={messageTemplatesCrud}
      perms={{ create: 'marketing.create', update: 'marketing.create', remove: 'marketing.create' }}
      toForm={(t) => ({
        name: t?.name ?? '',
        channel: t?.channel ?? 1,
        subject: t?.subject ?? null,
        body: t?.body ?? '',
      })}
      renderForm={() => null}
      formModal={({ open, mode, submitting, onCancel, onSubmit, defaultValues }) => (
        <CrudFormModal
          open={open}
          title={mode === 'edit' ? 'Sửa mẫu' : 'Thêm mẫu'}
          schema={messageTemplateFormSchema}
          defaultValues={defaultValues}
          submitting={submitting}
          onCancel={onCancel}
          onSubmit={onSubmit}
        >
          <TextField name="name" label="Tên mẫu" required />
          <SelectField name="channel" label="Kênh" options={CHANNEL_OPTIONS} required />
          <TextField name="subject" label="Tiêu đề (Email)" />
          <TextAreaField name="body" label="Nội dung" required />
        </CrudFormModal>
      )}
    />
  );
}
