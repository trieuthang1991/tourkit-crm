import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const surchargeCalcType = { fixed: 0, percent: 1 } as const;

export const orderSurchargeSchema = z.object({
  id: z.string().uuid(),
  orderId: z.string().uuid(),
  surchargeId: z.string().nullable(),
  description: z.string(),
  calcType: z.number(),
  value: z.number(),
  amount: z.number(),
});
export type OrderSurcharge = z.infer<typeof orderSurchargeSchema>;

export const surchargeCatalogSchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  calcType: z.number(),
  defaultValue: z.number(),
  sortOrder: z.number(),
  status: z.number(),
});
export type SurchargeCatalog = z.infer<typeof surchargeCatalogSchema>;

export type CreateOrderSurchargeForm = {
  surchargeId: string | null;
  description: string;
  calcType: number;
  value: number;
};

const key = (orderId: string) => ['orders', orderId, 'surcharges'] as const;

// Danh mục loại phụ thu — nguồn cho picker khi thêm phụ thu vào đơn.
export function useSurchargeCatalog() {
  return useQuery({
    queryKey: ['surcharges'],
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>('/api/v1/surcharges');
      return z.array(surchargeCatalogSchema).parse(data);
    },
  });
}

export function useOrderSurcharges(orderId: string) {
  return useQuery({
    queryKey: key(orderId),
    queryFn: async () => {
      const { data } = await httpClient.get<unknown>(`/api/v1/orders/${orderId}/surcharges`);
      return z.array(orderSurchargeSchema).parse(data);
    },
    enabled: !!orderId,
  });
}

export function useCreateOrderSurcharge(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (body: CreateOrderSurchargeForm): Promise<OrderSurcharge> => {
      const { data } = await httpClient.post<unknown>(`/api/v1/orders/${orderId}/surcharges`, body);
      return orderSurchargeSchema.parse(data);
    },
    // Phụ thu đổi doanh thu đơn → làm mới cả danh sách phụ thu lẫn dữ liệu đơn.
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key(orderId) });
      qc.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}

export function useDeleteOrderSurcharge(orderId: string) {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: async (id: string) => {
      await httpClient.delete(`/api/v1/orders/${orderId}/surcharges/${id}`);
    },
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: key(orderId) });
      qc.invalidateQueries({ queryKey: ['orders'] });
    },
  });
}
