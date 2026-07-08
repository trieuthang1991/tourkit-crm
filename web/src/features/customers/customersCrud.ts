import { makeCrud } from '../../shared/ui/useCrudResource';
import { customerSchema } from './types';
import type { Customer, CustomerForm } from './types';

export const customersCrud = makeCrud<Customer, CustomerForm, CustomerForm>({
  key: 'customers',
  basePath: '/api/v1/customers',
  itemSchema: customerSchema,
  getId: (c) => c.id,
});
