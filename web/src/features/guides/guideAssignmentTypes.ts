import { z } from 'zod';

export const guideAssignmentSchema = z.object({
  id: z.string().uuid(),
  tourDepartureId: z.string().uuid(),
  providerId: z.string().uuid(),
  timeGo: z.string().nullable(),
  timeCome: z.string().nullable(),
  timeReturn: z.string().nullable(),
  note: z.string().nullable(),
  status: z.number(),
  handoverContent: z.string().nullable(),
  handedOverAt: z.string().nullable(),
  providerName: z.string().nullable().optional(),
  departureTitle: z.string().nullable().optional(),
  departureCode: z.string().nullable().optional(),
});
export type GuideAssignment = z.infer<typeof guideAssignmentSchema>;

const guideAssignmentCommonFields = {
  providerId: z.string().min(1, 'Bắt buộc'),
  timeGo: z.string().nullable(),
  timeCome: z.string().nullable(),
  timeReturn: z.string().nullable(),
  note: z.string().nullable().transform((v) => (v ? v : null)),
  status: z.number(),
};

export const guideAssignmentCreateSchema = z.object({
  tourDepartureId: z.string().min(1, 'Bắt buộc'),
  ...guideAssignmentCommonFields,
});
export type GuideAssignmentCreateForm = z.infer<typeof guideAssignmentCreateSchema>;

export const guideAssignmentUpdateSchema = z.object({ ...guideAssignmentCommonFields });
export type GuideAssignmentUpdateForm = z.infer<typeof guideAssignmentUpdateSchema>;

// Unified shape (superset): create có tourDepartureId, update không — cả hai output đều thoả.
export type GuideAssignmentForm = GuideAssignmentUpdateForm & { tourDepartureId?: string };
