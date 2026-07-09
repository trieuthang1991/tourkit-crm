import { makeCrud } from '../../shared/ui/useCrudResource';
import { serviceItemSchema } from './serviceItemTypes';
import type { ServiceItem, ServiceItemForm } from './serviceItemTypes';

export const serviceItemsCrud = makeCrud<ServiceItem, ServiceItemForm, ServiceItemForm>({
  key: 'serviceItems',
  basePath: '/api/v1/service-items',
  itemSchema: serviceItemSchema,
  getId: (s) => s.id,
});
