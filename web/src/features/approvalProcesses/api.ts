import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { approvalProcessDetailSchema, approvalProcessSchema } from './types';

const LIST_KEY = ['approval-processes'];
const detailKey = (id: string) => ['approval-process', id];

const userRowSchema = z.object({ id: z.string().uuid(), fullName: z.string() });
const positionRowSchema = z.object({ id: z.string().uuid(), name: z.string() });

export function useApprovalProcesses() {
  return useQuery({
    queryKey: LIST_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/approval-processes');
      return z.array(approvalProcessSchema).parse(data);
    },
  });
}

export function useApprovalProcessDetail(id: string | null) {
  return useQuery({
    queryKey: detailKey(id ?? ''),
    enabled: !!id,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/approval-processes/${id}`);
      return approvalProcessDetailSchema.parse(data);
    },
  });
}

export function usePositionOptions() {
  return useQuery({
    queryKey: ['positions'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/positions');
      return z.array(positionRowSchema).parse(data);
    },
  });
}

export function useUserOptions() {
  return useQuery({
    queryKey: ['users'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/users');
      return z.array(userRowSchema).parse(data);
    },
  });
}

export function useCreateProcess() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: { name: string; method: number }) => {
      await httpClient.post('/api/v1/approval-processes', body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: LIST_KEY }),
  });
}

export function useDeleteProcess() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/approval-processes/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: LIST_KEY }),
  });
}

export function useAddStep(processId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (positionId: string) => {
      await httpClient.post(`/api/v1/approval-processes/${processId}/steps`, { positionId });
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: detailKey(processId) });
      qc.invalidateQueries({ queryKey: LIST_KEY });
    },
  });
}

export function useDeleteStep(processId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (stepId: string) => {
      await httpClient.delete(`/api/v1/approval-processes/${processId}/steps/${stepId}`);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: detailKey(processId) });
      qc.invalidateQueries({ queryKey: LIST_KEY });
    },
  });
}

export function useSetStepUsers(processId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ stepId, userIds }: { stepId: string; userIds: string[] }) => {
      await httpClient.put(`/api/v1/approval-processes/${processId}/steps/${stepId}/users`, { userIds });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: detailKey(processId) }),
  });
}
