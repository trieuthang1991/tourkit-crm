import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { flightIndividualSchema, flightIndividualStatsSchema } from './individualTypes';
import type { FlightIndividualForm } from './individualTypes';

const KEY = ['flight-tickets-individual'];

export type FlightIndividualFilter = {
  q?: string;
  providerRef?: string;
  status?: number;
  tab?: string;
  departFrom?: string;
  departTo?: string;
};

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useFlightIndividuals(page: number, size: number, filter: FlightIndividualFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/flight-tickets-individual', {
        params: cleanParams({ page, size, ...filter }),
      });
      return pagedSchema(flightIndividualSchema).parse(data);
    },
  });
}

export function useFlightIndividualStats(filter: FlightIndividualFilter = {}) {
  // Thống kê/đếm sub-tab theo bộ lọc HIỆN TẠI nhưng KHÔNG theo tab (để badge mỗi tab đúng tổng).
  const rest: Omit<FlightIndividualFilter, 'tab'> = { ...filter };
  delete (rest as FlightIndividualFilter).tab;
  return useQuery({
    queryKey: [...KEY, 'stats', rest],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/flight-tickets-individual/stats', {
        params: cleanParams(rest),
      });
      return flightIndividualStatsSchema.parse(data);
    },
  });
}

export function useCreateFlightIndividual() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: FlightIndividualForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/flight-tickets-individual', body);
      return flightIndividualSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateFlightIndividual() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: FlightIndividualForm }) => {
      await httpClient.put(`/api/v1/flight-tickets-individual/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteFlightIndividual() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/flight-tickets-individual/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
