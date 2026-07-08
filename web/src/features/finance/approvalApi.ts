import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { httpClient } from '../../shared/api/httpClient';
import { approvalSchema } from './approvalTypes';
import type { ActApprovalForm, Approval, StartApprovalForm } from './approvalTypes';

const approvalKey = (receiptId: string) => ['receipts', receiptId, 'approval'] as const;

// GET trả 404 khi phiếu thu chưa có quy trình duyệt — coi đây là "chưa có approval" (null),
// không phải lỗi, nên không toast và không retry.
export function useApproval(receiptId: string) {
  return useQuery({
    queryKey: approvalKey(receiptId),
    queryFn: async (): Promise<Approval | null> => {
      try {
        const { data } = await httpClient.get<unknown>(`/api/v1/receipts/${receiptId}/approval`);
        return approvalSchema.parse(data);
      } catch (e) {
        if (axios.isAxiosError(e) && e.response?.status === 404) {
          return null;
        }
        throw e;
      }
    },
    enabled: !!receiptId,
    retry: false,
  });
}

export function useStartApproval(receiptId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: StartApprovalForm): Promise<Approval> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/receipts/${receiptId}/approval`, body);
      return approvalSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: approvalKey(receiptId) }),
  });
}

export function useActApproval(receiptId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: ActApprovalForm): Promise<Approval> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/receipts/${receiptId}/approval/act`, body);
      return approvalSchema.parse(data);
    },
    onSuccess: () => qc.invalidateQueries({ queryKey: approvalKey(receiptId) }),
  });
}
