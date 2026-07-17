import { zodResolver } from '@hookform/resolvers/zod';
import type { ReactNode } from 'react';
import { Alert, Button, Input } from '../../shared/ui/antd';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useAuth } from './AuthContext';

function Field({ label, error, children }: { label?: string; error?: string; children: ReactNode }) {
  return (
    <div style={{ marginBottom: 16 }}>
      {label ? <label style={{ display: 'block', marginBottom: 6, fontSize: 12.5, fontWeight: 500, color: 'var(--tk-text-strong)' }}>{label}</label> : null}
      {children}
      {error ? <div style={{ marginTop: 6, fontSize: 12, color: 'var(--tk-danger)' }}>{error}</div> : null}
    </div>
  );
}

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
      <form onSubmit={handleSubmit(onSubmit)}>
        <Field label="Mã tổ chức" error={errors.tenantSlug?.message}>
          <Controller name="tenantSlug" control={control} render={({ field }) => <Input {...field} placeholder="vd: demo-tour" />} />
        </Field>
        <Field label="Email" error={errors.email?.message}>
          <Controller name="email" control={control} render={({ field }) => <Input {...field} placeholder="email@congty.vn" />} />
        </Field>
        <Field label="Mật khẩu" error={errors.password?.message}>
          <Controller name="password" control={control} render={({ field }) => <Input.Password {...field} />} />
        </Field>
        <Button type="primary" htmlType="submit" block loading={isSubmitting}>
          Đăng nhập
        </Button>
        <div style={{ textAlign: 'center', marginTop: 16 }}>
          <Link to="/register">Đăng ký công ty</Link>
        </div>
      </form>
    </div>
  );
}
