import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { agentBookingSchema, agentBookingSummarySchema } from './types';
import type { AddPassengerForm, CreateAgentBookingForm } from './types';

const KEY = ['agentBookings'];

export function useAgentBookings(page: number, size: number) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agent-bookings', { params: { page, size } });
      return pagedSchema(agentBookingSummarySchema).parse(data);
    },
  });
}

export function useAgentBooking(id: string) {
  return useQuery({
    queryKey: [...KEY, id],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/agent-bookings/${id}`);
      return agentBookingSchema.parse(data);
    },
    enabled: !!id,
  });
}

export function useCreateAgentBooking() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateAgentBookingForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/agent-bookings', body);
      return agentBookingSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useAddPassenger() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ bookingId, body }: { bookingId: string; body: AddPassengerForm }) => {
      await httpClient.post(`/api/v1/agent-bookings/${bookingId}/passengers`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useRemovePassenger() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ bookingId, passengerId }: { bookingId: string; passengerId: string }) => {
      await httpClient.delete(`/api/v1/agent-bookings/${bookingId}/passengers/${passengerId}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
