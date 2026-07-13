import { z } from 'zod';

export const vehicleAssignmentSchema = z.object({
  id: z.string().uuid(),
  tourDepartureId: z.string().uuid(),
  vehicleId: z.string().uuid(),
  driverName: z.string().nullable(),
  driverPhone: z.string().nullable(),
  timeGo: z.string().nullable(),
  timeCome: z.string().nullable(),
  note: z.string().nullable(),
  status: z.number(),
  vehicleName: z.string().nullable().optional(),
  departureTitle: z.string().nullable().optional(),
  departureCode: z.string().nullable().optional(),
});
export type VehicleAssignment = z.infer<typeof vehicleAssignmentSchema>;

const vehicleAssignmentCommonFields = {
  vehicleId: z.string().min(1, 'Bắt buộc'),
  driverName: z.string().nullable().transform((v) => (v ? v : null)),
  driverPhone: z.string().nullable().transform((v) => (v ? v : null)),
  timeGo: z.string().nullable(),
  timeCome: z.string().nullable(),
  note: z.string().nullable().transform((v) => (v ? v : null)),
  status: z.number(),
};

export const vehicleAssignmentCreateSchema = z.object({
  tourDepartureId: z.string().min(1, 'Bắt buộc'),
  ...vehicleAssignmentCommonFields,
});
export type VehicleAssignmentCreateForm = z.infer<typeof vehicleAssignmentCreateSchema>;

export const vehicleAssignmentUpdateSchema = z.object({ ...vehicleAssignmentCommonFields });
export type VehicleAssignmentUpdateForm = z.infer<typeof vehicleAssignmentUpdateSchema>;

// Unified shape (superset): create có tourDepartureId, update không — cả hai output đều thoả.
export type VehicleAssignmentForm = VehicleAssignmentUpdateForm & { tourDepartureId?: string };
