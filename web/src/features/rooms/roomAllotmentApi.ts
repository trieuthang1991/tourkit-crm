import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';
import { roomAllotmentSchema, roomAllotmentStatsSchema } from './roomAllotmentTypes';
import type { RoomAllotmentForm } from './roomAllotmentTypes';

const KEY = ['room-allotments'];

export type RoomAllotmentFilter = {
  q?: string;
  projectName?: string;
  province?: string;
  market?: string;
  providerRef?: string;
  rating?: number;
  dateFrom?: string;
  dateTo?: string;
};

function cleanParams(obj: Record<string, unknown>): Record<string, unknown> {
  return Object.fromEntries(Object.entries(obj).filter(([, v]) => v !== undefined && v !== null && v !== ''));
}

export function useRoomAllotments(page: number, size: number, filter: RoomAllotmentFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'list', page, size, filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/room-allotments', {
        params: cleanParams({ page, size, ...filter }),
      });
      return pagedSchema(roomAllotmentSchema).parse(data);
    },
  });
}

export function useRoomAllotmentStats(filter: RoomAllotmentFilter = {}) {
  return useQuery({
    queryKey: [...KEY, 'stats', filter],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/room-allotments/stats', {
        params: cleanParams(filter),
      });
      return roomAllotmentStatsSchema.parse(data);
    },
  });
}

export function useCreateRoomAllotment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: RoomAllotmentForm) => {
      const { data } = await httpClient.post<unknown>('/api/v1/room-allotments', body);
      return roomAllotmentSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useUpdateRoomAllotment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ id, body }: { id: string; body: RoomAllotmentForm }) => {
      await httpClient.put(`/api/v1/room-allotments/${id}`, body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useDeleteRoomAllotment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/room-allotments/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
