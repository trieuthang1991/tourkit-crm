import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Form, Input } from 'antd';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useAuth } from './AuthContext';

const loginFormSchema = z.object({
  tenantSlug: z.string().min(1, 'Vui lòng nhập mã tổ chức'),
  email: z.string().min(1, 'Vui lòng nhập email').email('Email không hợp lệ'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
});

type LoginFormValues = z.infer<typeof loginFormSchema>;

export function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const [submitError, setSubmitError] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<LoginFormValues>({
    resolver: zodResolver(loginFormSchema),
    defaultValues: { tenantSlug: '', email: '', password: '' },
  });

  const onSubmit = async (values: LoginFormValues): Promise<void> => {
    setSubmitError(null);
    try {
      await login(values);
      navigate('/', { replace: true });
    } catch {
      setSubmitError('Đăng nhập thất bại. Kiểm tra lại thông tin đăng nhập.');
    }
  };

  return (
    <div style={{ maxWidth: 360, margin: '96px auto' }}>
      <h1 style={{ textAlign: 'center', marginBottom: 24 }}>TourKit — Đăng nhập</h1>
      {submitError ? <Alert type="error" message={submitError} style={{ marginBottom: 16 }} /> : null}
      <Form layout="vertical" onFinish={handleSubmit(onSubmit)}>
        <Form.Item
          label="Mã tổ chức"
          validateStatus={errors.tenantSlug ? 'error' : ''}
          help={errors.tenantSlug?.message}
        >
          <Controller
            name="tenantSlug"
            control={control}
            render={({ field }) => <Input {...field} placeholder="vd: demo-tour" />}
          />
        </Form.Item>
        <Form.Item label="Email" validateStatus={errors.email ? 'error' : ''} help={errors.email?.message}>
          <Controller
            name="email"
            control={control}
            render={({ field }) => <Input {...field} placeholder="email@congty.vn" />}
          />
        </Form.Item>
        <Form.Item
          label="Mật khẩu"
          validateStatus={errors.password ? 'error' : ''}
          help={errors.password?.message}
        >
          <Controller name="password" control={control} render={({ field }) => <Input.Password {...field} />} />
        </Form.Item>
        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={isSubmitting}>
            Đăng nhập
          </Button>
        </Form.Item>
      </Form>
    </div>
  );
}
