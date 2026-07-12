import { z } from 'zod';

const nullableText = z
  .string()
  .nullable()
  .transform((v) => (v ? v : null));

// Loại khách hàng bám hệ cũ (0 Cá nhân, 1 Doanh nghiệp, 2 Đối tác, 3 CTV).
export const CUSTOMER_TYPE_OPTIONS = [
  { value: 0, label: 'Cá nhân' },
  { value: 1, label: 'Doanh nghiệp' },
  { value: 2, label: 'Đối tác' },
  { value: 3, label: 'CTV' },
];
export const customerTypeLabel = (v: number) =>
  CUSTOMER_TYPE_OPTIONS.find((o) => o.value === v)?.label ?? String(v);

export const GENDER_OPTIONS = [
  { value: 'Nam', label: 'Nam' },
  { value: 'Nữ', label: 'Nữ' },
  { value: 'Khác', label: 'Khác' },
];

export const customerSchema = z.object({
  id: z.string().uuid(),
  code: z.string().nullable(),
  fullName: z.string(),
  phone: z.string().nullable(),
  customerType: z.number(),
  source: z.string().nullable(),
  tag: z.string().nullable(),
  tempBalance: z.number(),
  email: z.string().nullable(),
  address: z.string().nullable(),
  dateOfBirth: z.string().nullable(),
  idCardNumber: z.string().nullable(),
  passportNumber: z.string().nullable(),
  passportExpiry: z.string().nullable(),
  nationality: z.string().nullable(),
  // CRM (từ CrmProfileJson) — ID tham chiếu là string để migrate dữ liệu cũ
  gender: z.string().nullable(),
  city: z.string().nullable(),
  marketGroup: z.string().nullable(),
  initialNeed: z.string().nullable(),
  collaboratorName: z.string().nullable(),
  campaign: z.string().nullable(),
  createdBy: z.string().nullable(),
  createdByName: z.string().nullable(),
  segments: z.array(z.string()),
  tags: z.array(z.string()),
  assignedTo: z.array(z.string()),
  assignedToNames: z.array(z.string()),
  createdAt: z.string(),
  // Aggregate
  purchaseCount: z.number(),
  revenue: z.number(),
  lastCareAt: z.string().nullable(),
  lastCareContent: z.string().nullable(),
});
export type Customer = z.infer<typeof customerSchema>;

export const customerFormSchema = z.object({
  fullName: z.string().min(1, 'Bắt buộc'),
  phone: nullableText,
  customerType: z.number(),
  source: nullableText,
  tag: nullableText,
  tempBalance: z.number(),
  email: nullableText,
  address: nullableText,
  dateOfBirth: z.string().nullable(),
  idCardNumber: nullableText,
  passportNumber: nullableText,
  passportExpiry: z.string().nullable(),
  nationality: nullableText,
  gender: nullableText,
  city: nullableText,
  marketGroup: nullableText,
  initialNeed: nullableText,
  collaboratorName: nullableText,
  campaign: nullableText,
  segments: z.array(z.string()),
  tags: z.array(z.string()),
  assignedTo: z.array(z.string()),
});
export type CustomerForm = z.infer<typeof customerFormSchema>;
