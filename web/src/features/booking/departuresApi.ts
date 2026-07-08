import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { httpClient } from '../../shared/api/httpClient';
import { makeCrud } from '../../shared/ui/useCrudResource';
import { departureSchema } from './departureTypes';
import type { Departure, DepartureForm } from './departureTypes';

// Không có PUT/DELETE cho tour-departures — DeparturesPage chỉ dùng useList + useCreate
// (destructure bỏ useUpdate/useRemove, ResourcePage tự ẩn nút Sửa/Xoá khi thiếu).
export const departuresCrud = makeCrud<Departure, DepartureForm, DepartureForm>({
  key: 'departures',
  basePath: '/api/v1/tour-departures',
  itemSchema: departureSchema,
  getId: (d) => d.id,
});

export function useDeparture(id: string) {
  return useQuery({
    queryKey: ['departures', id],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/tour-departures/${id}`);
      return departureSchema.parse(data);
    },
    enabled: !!id,
  });
}

export function useCloseDeparture() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.post(`/api/v1/tour-departures/${id}/close`);
    },
    // Khớp prefix ['departures'] của departuresCrud.keys.all — invalidate luôn cả list và useDeparture(id).
    onSuccess: () => qc.invalidateQueries({ queryKey: departuresCrud.keys.all }),
  });
}
