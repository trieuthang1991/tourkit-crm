import { zodResolver } from '@hookform/resolvers/zod';
import type { ReactNode } from 'react';
import { Alert, App, Button, Input, Result } from '../../shared/ui/antd';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { registerTenantFormSchema, useRegisterTenant } from './registrationApi';
import type { RegisterTenantForm } from './registrationApi';

function Field({ label, error, children }: { label?: string; error?: string; children: ReactNode }) {
  return (
    <div style={{ marginBottom: 16 }}>
      {label ? <label style={{ display: 'block', marginBottom: 6, fontSize: 12.5, fontWeight: 500, color: 'var(--tk-text-strong)' }}>{label}</label> : null}
      {children}
      {error ? <div style={{ marginTop: 6, fontSize: 12, color: 'var(--tk-danger)' }}>{error}</div> : null}
    </div>
  );
}

export function RegistrationPage() {
  const { message } = App.useApp();
  const registerTenant = useRegisterTenant();
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [createdSlug, setCreatedSlug] = useState<string | null>(null);

  const {
    control,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<RegisterTenantForm>({
    resolver: zodResolver(registerTenantFormSchema),
    defaultValues: { companyName: '', slug: '', adminEmail: '', adminPassword: '', adminFullName: '' },
  });

  const onSubmit = async (values: RegisterTenantForm): Promise<void> => {
    setSubmitError(null);
    try {
      const result = await registerTenant.mutateAsync(values);
      setCreatedSlug(result.slug);
      message.success('Đăng ký công ty thành công');
    } catch (e) {
      setSubmitError(errorMessage(e, 'Đăng ký thất bại. Vui lòng thử lại.'));
    }
  };

  if (createdSlug) {
    return (
      <div style={{ maxWidth: 420, margin: '96px auto' }}>
        <Result
          status="success"
          title="Đăng ký thành công"
          subTitle={`Mã tổ chức của bạn: ${createdSlug}`}
          extra={
            <Link to="/login">
              <Button type="primary">Đến trang đăng nhập</Button>
            </Link>
          }
        />
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 400, margin: '64px auto' }}>
      <h1 style={{ textAlign: 'center', marginBottom: 24 }}>TourKit — Đăng ký công ty</h1>
      {submitError ? <Alert type="error" message={submitError} style={{ marginBottom: 16 }} /> : null}
      <form onSubmit={handleSubmit(onSubmit)}>
        <Field label="Tên công ty" error={errors.companyName?.message}>
          <Controller name="companyName" control={control} render={({ field }) => <Input {...field} />} />
        </Field>
        <Field label="Mã tổ chức (slug)" error={errors.slug?.message}>
          <Controller name="slug" control={control} render={({ field }) => <Input {...field} placeholder="vd: demo-tour" />} />
        </Field>
        <Field label="Họ tên quản trị viên" error={errors.adminFullName?.message}>
          <Controller name="adminFullName" control={control} render={({ field }) => <Input {...field} />} />
        </Field>
        <Field label="Email quản trị viên" error={errors.adminEmail?.message}>
          <Controller name="adminEmail" control={control} render={({ field }) => <Input {...field} placeholder="admin@congty.vn" />} />
        </Field>
        <Field label="Mật khẩu" error={errors.adminPassword?.message}>
          <Controller name="adminPassword" control={control} render={({ field }) => <Input.Password {...field} />} />
        </Field>
        <Button type="primary" htmlType="submit" block loading={isSubmitting}>
          Đăng ký
        </Button>
        <div style={{ textAlign: 'center', marginTop: 16 }}>
          <Link to="/login">Đã có tài khoản? Đăng nhập</Link>
        </div>
      </form>
    </div>
  );
}
