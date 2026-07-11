import { makeCrud } from '../../shared/ui/useCrudResource';
import { messageTemplateSchema } from './types';
import type { MessageTemplate, MessageTemplateForm } from './types';

export const messageTemplatesCrud = makeCrud<MessageTemplate, MessageTemplateForm, MessageTemplateForm>({
  key: 'message-templates',
  basePath: '/api/v1/message-templates',
  itemSchema: messageTemplateSchema,
  getId: (t) => t.id,
});
