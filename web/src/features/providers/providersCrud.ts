import { makeCrud } from '../../shared/ui/useCrudResource';
import { providerSchema } from './types';
import type { Provider, ProviderForm } from './types';

export const providersCrud = makeCrud<Provider, ProviderForm, ProviderForm>({
  key: 'providers',
  basePath: '/api/v1/providers',
  itemSchema: providerSchema,
  getId: (p) => p.id,
});
