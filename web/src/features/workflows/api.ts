import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';
import { workflowBoardSchema, workflowSchema } from './types';
import type { WorkflowCreateForm } from './types';

const LIST_KEY = ['workflows'];
const boardKey = (id: string) => ['workflow-board', id];

export function useWorkflows() {
  return useQuery({
    queryKey: LIST_KEY,
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/workflows');
      return z.array(workflowSchema).parse(data);
    },
  });
}

export function useWorkflowBoard(id: string) {
  return useQuery({
    queryKey: boardKey(id),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/workflows/${id}`);
      return workflowBoardSchema.parse(data);
    },
  });
}

export function useCreateWorkflow() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: WorkflowCreateForm) => {
      await httpClient.post('/api/v1/workflows', body);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: LIST_KEY }),
  });
}

export function useDeleteWorkflow() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/workflows/${id}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: LIST_KEY }),
  });
}

export function useAddSection(workflowId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (name: string) => {
      await httpClient.post(`/api/v1/workflows/${workflowId}/sections`, { name, color: null, icon: null });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: boardKey(workflowId) }),
  });
}

export function useDeleteSection(workflowId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (sectionId: string) => {
      await httpClient.delete(`/api/v1/workflows/${workflowId}/sections/${sectionId}`);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: boardKey(workflowId) }),
  });
}

export function useMoveTask(workflowId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ taskId, sectionId }: { taskId: string; sectionId: string }) => {
      await httpClient.post(`/api/v1/workflows/${workflowId}/tasks/${taskId}/move`, { sectionId });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: boardKey(workflowId) }),
  });
}

export function useAddCard(workflowId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async ({ title, sectionId }: { title: string; sectionId: string }) => {
      await httpClient.post('/api/v1/work-tasks', {
        title,
        description: null,
        assigneeUserId: null,
        dueDate: null,
        priority: 1,
        status: 0,
        relatedOrderId: null,
        workflowId,
        sectionId,
      });
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: boardKey(workflowId) }),
  });
}
