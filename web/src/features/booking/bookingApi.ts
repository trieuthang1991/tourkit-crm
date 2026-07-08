import { useMutation, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { orderSchema, seatSchema } from './seatTypes';
import type { BookingRequestForm, Order, Seat } from './seatTypes';

function bookingsPath(departureId: string) {
  return `/api/v1/tour-departures/${departureId}/bookings`;
}

function holdsPath(departureId: string) {
  return `/api/v1/tour-departures/${departureId}/holds`;
}

export function useCreateBooking(departureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: BookingRequestForm): Promise<Order> => {
      const { data } = await httpClient.post<unknown>(bookingsPath(departureId), body);
      return orderSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['orders'] }),
  });
}

export function useCreateHold(departureId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: BookingRequestForm): Promise<Seat> => {
      const { data } = await httpClient.post<unknown>(holdsPath(departureId), body);
      return seatSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['orders'] }),
  });
}
