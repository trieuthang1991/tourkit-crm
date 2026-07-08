import { makeCrud } from '../../shared/ui/useCrudResource';
import { leadSchema } from './types';
import type { Lead, LeadForm } from './types';

export const leadsCrud = makeCrud<Lead, LeadForm, LeadForm>({
  key: 'leads',
  basePath: '/api/v1/leads',
  itemSchema: leadSchema,
  getId: (l) => l.id,
});
