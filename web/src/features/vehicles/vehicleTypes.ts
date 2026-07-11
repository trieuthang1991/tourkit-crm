import { z } from 'zod';

export const vehicleSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  firmName: z.string().nullable(),
  seatType: z.number(),
  status: z.number(),
});
export type Vehicle = z.infer<typeof vehicleSchema>;

const vehicleCommonFields = {
  name: z.string().min(1, 'Bắt buộc'),
  firmName: z.string().nullable().transform((v) => (v ? v : null)),
  seatType: z.number(),
  status: z.number(),
};

export const vehicleCreateSchema = z.object({ ...vehicleCommonFields });
export type VehicleCreateForm = z.infer<typeof vehicleCreateSchema>;

export const vehicleUpdateSchema = z.object({ ...vehicleCommonFields });
export type VehicleUpdateForm = z.infer<typeof vehicleUpdateSchema>;

export type VehicleForm = VehicleUpdateForm;
