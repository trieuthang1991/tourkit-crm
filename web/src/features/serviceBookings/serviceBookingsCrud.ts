import { makeCrud } from '../../shared/ui/useCrudResource';
import { serviceBookingSchema } from './types';
import type { ServiceBooking, ServiceBookingForm } from './types';

export const serviceBookingsCrud = makeCrud<ServiceBooking, ServiceBookingForm, ServiceBookingForm>({
  key: 'serviceBookings',
  basePath: '/api/v1/service-bookings',
  itemSchema: serviceBookingSchema,
  getId: (s) => s.id,
});
