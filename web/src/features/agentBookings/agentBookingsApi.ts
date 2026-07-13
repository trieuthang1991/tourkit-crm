import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { agentBookingSchema, agentBookingSummarySchema } from './types';
import type { AddPassengerForm, CreateAgentBookingForm } from './types';

const KEY = ['agentBookings'];

export type AgentBookingFilter = { q?: string; agentId?: string; status?: number };

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useAgentBookings(page: number, size: number, filter: AgentBookingFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agent-bookings', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(agentBookingSummarySchema).parse(data);
    },
  });
}

export const agentBookingStatsSchema = z.object({
  total: z.number(),
  pending: z.number(),
  confirmed: z.number(),
  cancelled: z.number(),
  done: z.number(),
  totalAmount: z.number(),
});

export function useAgentBookingStats() {
  return useQuery({
    queryKey: [...KEY, 'stats'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/agent-bookings/stats');
      return agentBookingStatsSchema.parse(data);
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
