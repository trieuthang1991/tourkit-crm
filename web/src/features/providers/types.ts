import { z } from 'zod';

export const providerSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  name: z.string(),
  type: z.number(),
  phone: z.string().nullable(),
  email: z.string().nullable(),
  address: z.string().nullable(),
  taxCode: z.string().nullable(),
  contactPerson: z.string().nullable(),
  bankAccount: z.string().nullable(),
  bankName: z.string().nullable(),
  paymentTermId: z.string().nullable(),
  rate: z.number(),
  status: z.number(),
  province: z.string().nullable(),
  branchId: z.string().nullable(),
  // Công nợ NCC (danh sách): tổng mua · đã trả · còn nợ
  totalCost: z.number(),
  paid: z.number(),
  outstanding: z.number(),
});
export type Provider = z.infer<typeof providerSchema>;

const providerCommonFields = {
  name: z.string().min(1, 'Bắt buộc'),
  type: z.number(),
  phone: z.string().nullable().transform((v) => (v ? v : null)),
  email: z.string().nullable().transform((v) => (v ? v : null)),
  address: z.string().nullable().transform((v) => (v ? v : null)),
  taxCode: z.string().nullable().transform((v) => (v ? v : null)),
  contactPerson: z.string().nullable().transform((v) => (v ? v : null)),
  bankAccount: z.string().nullable().transform((v) => (v ? v : null)),
  bankName: z.string().nullable().transform((v) => (v ? v : null)),
  paymentTermId: z.string().nullable().transform((v) => (v ? v : null)),
  province: z.string().nullable().transform((v) => (v ? v : null)),
  branchId: z.string().nullable().transform((v) => (v ? v : null)),
  rate: z.number(),
  status: z.number(),
};

export const providerCreateSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  ...providerCommonFields,
});
export type ProviderCreateForm = z.infer<typeof providerCreateSchema>;

export const providerUpdateSchema = z.object(providerCommonFields);
export type ProviderUpdateForm = z.infer<typeof providerUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — code is optional so both
// providerCreateSchema (with code) and providerUpdateSchema (no code) outputs satisfy it.
export type ProviderForm = ProviderUpdateForm & { code?: string };

export const PROVIDER_TYPE: Record<number, string> = {
  1: 'Khách sạn',
  2: 'Vận chuyển',
  3: 'Nhà hàng',
  4: 'HDV',
  5: 'Hàng không',
  6: 'Khác',
};
