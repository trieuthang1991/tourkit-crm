import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { flightTicketSchema, flightTicketStatsSchema } from './types';
import type { CreateFlightTicketForm } from './types';

const KEY = ['flight-tickets'];

export type FlightFilter = {
  q?: string;
  marketRef?: string;
  providerRef?: string;
  tourType?: string;
  assigned?: boolean;
};

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useFlightTickets(page: number, size: number, filter: FlightFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/flight-tickets', { params: cleanParams({ page, size, ...filter }) });
      return pagedSchema(flightTicketSchema).parse(data);
    },
  });
}

export function useFlightTicketStats(filter: FlightFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'stats', filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/flight-tickets/stats', { params: cleanParams({ ...filter }) });
      return flightTicketStatsSchema.parse(data);
    },
  });
}

export function useCreateFlightTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateFlightTicketForm) => {
      await httpClient.post('/api/v1/flight-tickets', { ...body, segments: [] });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useAssignFlightTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, orderRef }: { id: string; orderRef: string | null }) => {
      await httpClient.post(`/api/v1/flight-tickets/${id}/assign`, { orderRef });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteFlightTicket() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/flight-tickets/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

const marketOptionSchema = z.object({ id: z.string().uuid(), name: z.string() });
export function useMarketOptions() {
  return useQuery({
    queryKey: ['market-types', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/market-types');
      return z.array(marketOptionSchema).parse(data);
    },
  });
}

const providerOptionSchema = z.object({ id: z.string().uuid(), name: z.string() });
export function useProviderOptions() {
  return useQuery({
    queryKey: ['providers', 'options'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/providers', { params: { page: 1, size: 500 } });
      return pagedSchema(providerOptionSchema).parse(data).items;
    },
  });
}
