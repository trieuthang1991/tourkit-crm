import { z } from 'zod';

export const providerServiceSchema = z.object({
  id: z.string().uuid(),
  providerId: z.string().uuid(),
  serviceItemId: z.string().uuid().nullable(),
  priceName: z.string().nullable(),
  contractPrice: z.number(),
  publicPrice: z.number(),
  amountOfPeople: z.number(),
  note: z.string().nullable(),
  status: z.number(),
});
export type ProviderService = z.infer<typeof providerServiceSchema>;

const providerServiceCommonFields = {
  serviceItemId: z.string().uuid().nullable(),
  priceName: z.string().nullable().transform((v) => (v ? v : null)),
  contractPrice: z.number(),
  publicPrice: z.number(),
  amountOfPeople: z.number(),
  note: z.string().nullable().transform((v) => (v ? v : null)),
  status: z.number(),
};

export const providerServiceCreateSchema = z.object({
  providerId: z.string().uuid(),
  ...providerServiceCommonFields,
});
export type ProviderServiceCreateForm = z.infer<typeof providerServiceCreateSchema>;

export const providerServiceUpdateSchema = z.object(providerServiceCommonFields);
export type ProviderServiceUpdateForm = z.infer<typeof providerServiceUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — providerId is optional so both
// providerServiceCreateSchema (with providerId) and providerServiceUpdateSchema (no providerId)
// outputs satisfy it.
export type ProviderServiceForm = ProviderServiceUpdateForm & { providerId?: string };
