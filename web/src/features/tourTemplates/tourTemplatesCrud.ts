import { makeCrud } from '../../shared/ui/useCrudResource';
import { tourTemplateSchema } from './types';
import type { TourTemplate, TourTemplateForm } from './types';

export const tourTemplatesCrud = makeCrud<TourTemplate, TourTemplateForm, TourTemplateForm>({
  key: 'tourTemplates',
  basePath: '/api/v1/tour-templates',
  itemSchema: tourTemplateSchema,
  getId: (t) => t.id,
});
