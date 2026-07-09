import { makeCrud } from '../../shared/ui/useCrudResource';
import { customerCareSchema } from './customerCareTypes';
import type { CustomerCare, CustomerCareForm } from './customerCareTypes';

export const customerCaresCrud = makeCrud<CustomerCare, CustomerCareForm, CustomerCareForm>({
  key: 'customerCares',
  basePath: '/api/v1/customer-cares',
  itemSchema: customerCareSchema,
  getId: (c) => c.id,
});
