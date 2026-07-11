import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const notificationSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  message: z.string().nullable(),
  linkUrl: z.string().nullable(),
  isRead: z.boolean(),
  createdAt: z.string(),
});
export type Notification = z.infer<typeof notificationSchema>;

const KEY = ['notifications'];

export function useNotifications() {
  return useQuery({
    queryKey: KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/notifications');
      return z.array(notificationSchema).parse(data);
    },
  });
}

export function useUnreadCount() {
  return useQuery({
    queryKey: [...KEY, 'unread-count'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/notifications/unread-count');
      return z.object({ count: z.number() }).parse(data).count;
    },
    refetchInterval: 60_000, // cập nhật badge mỗi phút
  });
}

export function useMarkRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.post(`/api/v1/notifications/${id}/read`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}

export function useMarkAllRead() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async () => {
      await httpClient.post('/api/v1/notifications/read-all');
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: KEY }),
  });
}
