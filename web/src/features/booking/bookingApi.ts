import { useMutation, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { makeCrud } from '../../shared/ui/useCrudResource';
import { orderSchema, seatSchema } from './seatTypes';
import type { BookingRequestForm, Order, Seat } from './seatTypes';

// GET /api/v1/orders (Paged) — dùng cho OrdersPage (list-only, không có create/update/remove).
export const ordersCrud = makeCrud<Order, object, object>({
  key: 'orders',
  basePath: '/api/v1/orders',
  itemSchema: orderSchema,
  getId: (o) => o.id,
});

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

// PUT /api/v1/orders/{orderId}/sales — gán/đổi sales phụ trách đơn.
export function useAssignSales(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (salesUserId: string | null): Promise<Order> => {
      const { data } = await httpClient.put<unknown>(`/api/v1/orders/${orderId}/sales`, { salesUserId });
      return orderSchema.parse(data);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['orders'] });
      qc.invalidateQueries({ queryKey: ['reports', 'commission-by-user'] });
    },
  });
}
