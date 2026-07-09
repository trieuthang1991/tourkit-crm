import { makeCrud } from '../../shared/ui/useCrudResource';
import { providerServiceSchema } from './providerServiceTypes';
import type { ProviderService, ProviderServiceForm } from './providerServiceTypes';

export const providerServicesCrud = makeCrud<ProviderService, ProviderServiceForm, ProviderServiceForm>({
  key: 'providerServices',
  basePath: '/api/v1/provider-services',
  itemSchema: providerServiceSchema,
  getId: (p) => p.id,
});
