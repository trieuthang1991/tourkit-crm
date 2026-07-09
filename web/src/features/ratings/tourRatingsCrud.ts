import { makeCrud } from '../../shared/ui/useCrudResource';
import { tourRatingSchema } from './tourRatingTypes';
import type { TourRating, TourRatingForm } from './tourRatingTypes';

export const tourRatingsCrud = makeCrud<TourRating, TourRatingForm, TourRatingForm>({
  key: 'tourRatings',
  basePath: '/api/v1/tour-ratings',
  itemSchema: tourRatingSchema,
  getId: (r) => r.id,
});
