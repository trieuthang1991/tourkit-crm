import { z } from 'zod';

// Quỹ phòng / allotment — DTO trả từ API (Available derived + enrich tên NCC).
export const roomAllotmentSchema = z.object({
  id: z.string().uuid(),
  providerRef: z.string(),
  providerName: z.string().nullable(),
  serviceName: z.string(),
  projectName: z.string().nullable(),
  province: z.string().nullable(),
  market: z.string().nullable(),
  date: z.string(),
  dayType: z.number(),
  quota: z.number(),
  booked: z.number(),
  available: z.number(),
  price: z.number(),
  rating: z.number().nullable(),
  note: z.string().nullable(),
});
export type RoomAllotment = z.infer<typeof roomAllotmentSchema>;

export const roomAllotmentStatsSchema = z.object({
  cells: z.number(),
  providers: z.number(),
  totalQuota: z.number(),
  totalBooked: z.number(),
  totalAvailable: z.number(),
  normalDays: z.number(),
  weekendDays: z.number(),
  holidayDays: z.number(),
  peakDays: z.number(),
});
export type RoomAllotmentStats = z.infer<typeof roomAllotmentStatsSchema>;

export const roomAllotmentFormSchema = z.object({
  providerRef: z.string().min(1, 'Bắt buộc'),
  serviceName: z.string().min(1, 'Bắt buộc'),
  projectName: z.string().nullable(),
  province: z.string().nullable(),
  market: z.string().nullable(),
  date: z.string(),
  dayType: z.number(),
  quota: z.number().min(0),
  booked: z.number().min(0),
  price: z.number().min(0),
  rating: z.number().nullable(),
  note: z.string().nullable(),
});
export type RoomAllotmentForm = z.infer<typeof roomAllotmentFormSchema>;

// Loại ngày (bám hệ cũ — tô màu ô lịch giá).
export const DAY_TYPE_LABEL: Record<number, string> = { 0: 'Ngày thường', 1: 'Cuối tuần', 2: 'Lễ tết', 3: 'Cao điểm' };
// Màu ô theo loại ngày: thường=xanh dương · cuối tuần=xanh lá · lễ tết=đỏ · cao điểm=vàng.
export const DAY_TYPE_BG: Record<number, string> = {
  0: '#e6f4ff', // xanh dương nhạt
  1: '#eafce6', // xanh lá nhạt
  2: '#fff1f0', // đỏ nhạt
  3: '#fffbe6', // vàng nhạt
};
export const DAY_TYPE_BORDER: Record<number, string> = {
  0: '#91caff',
  1: '#95de64',
  2: '#ffa39e',
  3: '#ffe58f',
};
export const DAY_TYPE_TAG_COLOR: Record<number, string> = { 0: 'blue', 1: 'green', 2: 'red', 3: 'gold' };
export const DAY_TYPE_OPTIONS = [
  { value: 0, label: 'Ngày thường' },
  { value: 1, label: 'Cuối tuần' },
  { value: 2, label: 'Lễ tết' },
  { value: 3, label: 'Cao điểm' },
];
