import { useQuery } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { pagedSchema } from '../../shared/api/paged';

export const activityLogSchema = z.object({
  id: z.string().uuid(),
  userId: z.string().uuid().nullable(),
  action: z.string(),
  entityName: z.string(),
  entityId: z.string(),
  changes: z.string().nullable(),
  createdAt: z.string(),
});
export type ActivityLog = z.infer<typeof activityLogSchema>;

export function useActivityLogs(page: number, size: number) {
  return useQuery({
    queryKey: ['activityLogs', page, size],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/activity-logs', {
        params: { page, size },
      });
      return pagedSchema(activityLogSchema).parse(data);
    },
  });
}
