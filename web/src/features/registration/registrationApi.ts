import { useMutation } from '@tanstack/react-query';
import { z } from 'zod';
import { httpClient } from '../../shared/api/httpClient';

export const registerTenantFormSchema = z.object({
  companyName: z.string().min(1, 'Vui lòng nhập tên công ty'),
  slug: z.string().min(1, 'Vui lòng nhập mã tổ chức'),
  adminEmail: z.string().min(1, 'Vui lòng nhập email').email('Email không hợp lệ'),
  adminPassword: z.string().min(6, 'Mật khẩu tối thiểu 6 ký tự'),
  adminFullName: z.string().min(1, 'Vui lòng nhập họ tên'),
});
export type RegisterTenantForm = z.infer<typeof registerTenantFormSchema>;

const registerTenantResponseSchema = z.object({
  tenantId: z.string().uuid(),
  slug: z.string(),
  adminUserId: z.string().uuid(),
});
export type RegisterTenantResponse = z.infer<typeof registerTenantResponseSchema>;

export function useRegisterTenant() {
  return useMutation({
    mutationFn: async (body: RegisterTenantForm): Promise<RegisterTenantResponse> => {
      // Đăng ký công khai, không cần token — httpClient vẫn gắn Authorization nếu có sẵn nhưng
      // backend bỏ qua vì endpoint này cho phép anonymous.
      const { data } = await httpClient.post<unknown>('/api/v1/registration', body);
      return registerTenantResponseSchema.parse(data);
    },
  });
}
