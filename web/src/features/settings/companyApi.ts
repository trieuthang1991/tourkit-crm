import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { z } from 'zod';

export const companyProfileSchema = z.object({
  name: z.string(),
  shortName: z.string().nullable(),
  address: z.string().nullable(),
  hotline: z.string().nullable(),
  email: z.string().nullable(),
  website: z.string().nullable(),
  taxCode: z.string().nullable(),
  legalRepName: z.string().nullable(),
  legalRepTitle: z.string().nullable(),
  licenseNumber: z.string().nullable(),
  bankAccount: z.string().nullable(),
});
export type CompanyProfile = z.infer<typeof companyProfileSchema>;

const KEY = ['company-profile'];

export function useCompanyProfile() {
  return useQuery({
    queryKey: KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/company-profile');
      return companyProfileSchema.parse(data);
    },
  });
}

export function useSaveCompanyProfile() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CompanyProfile) => {
      await httpClient.put('/api/v1/company-profile', body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
