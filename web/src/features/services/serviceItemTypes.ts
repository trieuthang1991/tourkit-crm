import { z } from 'zod';

export const serviceItemSchema = z.object({
  id: z.string().uuid(),
  code: z.string(),
  name: z.string(),
  category: z.number(),
  status: z.number(),
});
export type ServiceItem = z.infer<typeof serviceItemSchema>;

const serviceItemCommonFields = {
  name: z.string().min(1, 'Bắt buộc'),
  category: z.number(),
  status: z.number(),
};

export const serviceItemCreateSchema = z.object({
  code: z.string().min(1, 'Bắt buộc'),
  ...serviceItemCommonFields,
});
export type ServiceItemCreateForm = z.infer<typeof serviceItemCreateSchema>;

export const serviceItemUpdateSchema = z.object(serviceItemCommonFields);
export type ServiceItemUpdateForm = z.infer<typeof serviceItemUpdateSchema>;

// Unified shape used as the ResourcePage/makeCrud TForm generic — code is optional so both
// serviceItemCreateSchema (with code) and serviceItemUpdateSchema (no code) outputs satisfy it.
export type ServiceItemForm = ServiceItemUpdateForm & { code?: string };

export const SERVICE_CATEGORY: Record<number, string> = {
  1: 'Khách sạn',
  2: 'Vận chuyển',
  3: 'Nhà hàng',
  4: 'HDV',
  5: 'Hàng không',
  6: 'Visa',
  7: 'Khác',
};
