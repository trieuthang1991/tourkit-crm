import { z } from 'zod';
import { httpClient } from '../../../shared/api/httpClient';
import { tourTemplateSchema } from '../types';
import type { TourTemplate } from '../types';

export const tourTemplateApi = {
  list: async (): Promise<TourTemplate[]> => {
    const { data } = await httpClient.get<unknown>('/api/v1/tour-templates');
    return z.array(tourTemplateSchema).parse(data);
  },
};
