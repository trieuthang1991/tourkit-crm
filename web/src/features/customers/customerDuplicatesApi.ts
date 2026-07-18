import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const duplicateCustomerSchema = z.object({
  id: z.string().uuid(),
  code: z.string().nullable(),
  fullName: z.string(),
  phone: z.string().nullable(),
  email: z.string().nullable(),
  createdAt: z.string(),
});

export const duplicateGroupSchema = z.object({
  matchType: z.string(),
  matchKey: z.string(),
  customers: z.array(duplicateCustomerSchema),
});
export type DuplicateGroup = z.infer<typeof duplicateGroupSchema>;

export function useCustomerDuplicates() {
  return useQuery({
    queryKey: ['customers', 'duplicates'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/customers/duplicates');
      return z.array(duplicateGroupSchema).parse(data);
    },
  });
}
