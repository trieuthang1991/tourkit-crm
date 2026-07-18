import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
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

// GET /api/v1/orders/{id} — chi tiết 1 đơn (đã làm giàu). initial: Order truyền qua navigation state để hiển thị ngay.
export function useOrder(orderId: string, initial?: Order) {
  return useQuery({
    queryKey: ['orders', 'detail', orderId],
    enabled: !!orderId,
    initialData: initial,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}`);
      return orderSchema.parse(data);
    },
  });
}

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

// POST /api/v1/orders/{orderId}/close — tất toán/chốt đơn (gate: đã thu đủ + hoa hồng đã quyết).
export function useCloseOrder(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (): Promise<Order> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/close`, {});
      return orderSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['orders'] }),
  });
}

// POST /api/v1/orders/{orderId}/reopen — mở lại đơn đã tất toán.
export function useReopenOrder(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (): Promise<Order> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/reopen`, {});
      return orderSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: ['orders'] }),
  });
}
