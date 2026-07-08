import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../api/httpClient';
import { pagedSchema } from '../api/paged';
import type { PageParams } from '../api/paged';

// eslint-disable-next-line @typescript-eslint/no-unused-vars -- _TCreate/_TUpdate giữ chỗ để makeCrud<TItem,TCreate,TUpdate> suy luận kiểu tại call site.
export type CrudConfig<TItem, _TCreate, _TUpdate> = {
  key: string; // query key root, e.g. 'customers'
  basePath: string; // e.g. '/api/v1/customers'
  itemSchema: z.ZodType<TItem>;
  getId: (item: TItem) => string;
};

export function makeCrud<TItem, TCreate extends object, TUpdate extends object>(
  cfg: CrudConfig<TItem, TCreate, TUpdate>,
) {
  const keys = {
    all: [cfg.key] as const,
    list: (p: PageParams) => [cfg.key, 'list', p.page, p.size] as const,
  };

  function useList(p: PageParams) {
    return useQuery({
      queryKey: keys.list(p),
      queryFn: async () => {
        const { data } = await httpClient.get<unknown>(cfg.basePath, { params: p });
        return pagedSchema(cfg.itemSchema).parse(data);
      },
    });
  }

  function useCreate() {
    const qc = useQueryClient();
    return useMutation({
      mutationFn: async (body: TCreate) => {
        const { data } = await httpClient.post<unknown>(cfg.basePath, body);
        return cfg.itemSchema.parse(data);
      },
      onSuccess: () => qc.invalidateQueries({ queryKey: keys.all }),
    });
  }

  function useUpdate() {
    const qc = useQueryClient();
    return useMutation({
      // PUT trả 204 No Content (body rỗng) → KHÔNG parse; ResourcePage chỉ cần invalidate để refetch.
      mutationFn: async ({ id, body }: { id: string; body: TUpdate }) => {
        await httpClient.put(`${cfg.basePath}/${id}`, body);
      },
      onSuccess: () => qc.invalidateQueries({ queryKey: keys.all }),
    });
  }

  function useRemove() {
    const qc = useQueryClient();
    return useMutation({
      mutationFn: async (id: string) => {
        await httpClient.delete(`${cfg.basePath}/${id}`);
      },
      onSuccess: () => qc.invalidateQueries({ queryKey: keys.all }),
    });
  }

  return { keys, useList, useCreate, useUpdate, useRemove, getId: cfg.getId };
}
