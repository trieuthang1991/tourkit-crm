import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, App, Button, Form, Input, Result } from 'antd';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { registerTenantFormSchema, useRegisterTenant } from './registrationApi';
import type { RegisterTenantForm } from './registrationApi';

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
      <Form layout="vertical" onFinish={handleSubmit(onSubmit)}>
        <Form.Item
          label="Tên công ty"
          validateStatus={errors.companyName ? 'error' : ''}
          help={errors.companyName?.message}
        >
          <Controller name="companyName" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>
        <Form.Item label="Mã tổ chức (slug)" validateStatus={errors.slug ? 'error' : ''} help={errors.slug?.message}>
          <Controller
            name="slug"
            control={control}
            render={({ field }) => <Input {...field} placeholder="vd: demo-tour" />}
          />
        </Form.Item>
        <Form.Item
          label="Họ tên quản trị viên"
          validateStatus={errors.adminFullName ? 'error' : ''}
          help={errors.adminFullName?.message}
        >
          <Controller name="adminFullName" control={control} render={({ field }) => <Input {...field} />} />
        </Form.Item>
        <Form.Item
          label="Email quản trị viên"
          validateStatus={errors.adminEmail ? 'error' : ''}
          help={errors.adminEmail?.message}
        >
          <Controller
            name="adminEmail"
            control={control}
            render={({ field }) => <Input {...field} placeholder="admin@congty.vn" />}
          />
        </Form.Item>
        <Form.Item
          label="Mật khẩu"
          validateStatus={errors.adminPassword ? 'error' : ''}
          help={errors.adminPassword?.message}
        >
          <Controller name="adminPassword" control={control} render={({ field }) => <Input.Password {...field} />} />
        </Form.Item>
        <Form.Item>
          <Button type="primary" htmlType="submit" block loading={isSubmitting}>
            Đăng ký
          </Button>
        </Form.Item>
        <div style={{ textAlign: 'center' }}>
          <Link to="/login">Đã có tài khoản? Đăng nhập</Link>
        </div>
      </Form>
    </div>
  );
}
