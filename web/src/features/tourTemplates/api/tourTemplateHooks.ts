import { useQuery } from '@tanstack/react-query';
import { tourTemplateApi } from './tourTemplateApi';

export const tourTemplateKeys = {
  all: ['tourTemplates'] as const,
  list: () => [...tourTemplateKeys.all, 'list'] as const,
};

export function useTourTemplates() {
  return useQuery({
    queryKey: tourTemplateKeys.list(),
    queryFn: tourTemplateApi.list,
  });
}
