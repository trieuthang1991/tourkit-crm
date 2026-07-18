import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, App, Button, Input } from '../../shared/ui/antd';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link } from 'react-router-dom';
import { errorMessage } from '../../shared/api/problem';
import { registerTenantFormSchema, useRegisterTenant } from './registrationApi';
import type { RegisterTenantForm } from './registrationApi';

const BRAND_FEATS = [
  { icon: 'rocket_launch', text: 'Khởi tạo không gian công ty trong vài phút' },
  { icon: 'tune', text: 'Bật đúng phân hệ nghiệp vụ bạn cần' },
  { icon: 'shield', text: 'Dữ liệu tách biệt theo từng tổ chức' },
];

function AuthBrand() {
  return (
    <aside className="mk-auth__brand">
      <span className="mk-auth__glow" />
      <Link to="/gioi-thieu" className="mk-auth__brandtop" style={{ textDecoration: 'none' }}>
        <span className="mk-auth__mark">T</span>
        <span className="mk-auth__brandname">TourKit</span>
      </Link>
      <div className="mk-auth__brandmid">
        <h2>Bắt đầu điều hành công ty lữ hành của bạn.</h2>
        <p>Tạo tài khoản công ty và trải nghiệm trọn bộ phân hệ: CRM, báo giá, đơn tour, điều hành dịch vụ và tài chính — tất cả trong một hệ thống.</p>
        <ul className="mk-auth__feats">
          {BRAND_FEATS.map((f) => (
            <li key={f.icon} className="mk-auth__feat">
              <span className="mk-auth__featdot">
                <span className="material-symbols-outlined">{f.icon}</span>
              </span>
              {f.text}
            </li>
          ))}
        </ul>
      </div>
      <div className="mk-auth__brandfoot">© {new Date().getFullYear()} TourKit CRM · Phần mềm quản lý & điều hành tour</div>
    </aside>
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

  return (
    <div className="mk mk-auth">
      <AuthBrand />
      <section className="mk-auth__panel">
        <div className="mk-auth__card">
          <div className="mk-auth__logo-sm">
            <span className="mk-auth__mark">T</span>
            <span>TourKit</span>
          </div>

          {createdSlug ? (
            <div style={{ textAlign: 'center' }}>
              <span className="material-symbols-outlined" style={{ fontSize: 56, color: 'var(--tk-success)' }}>check_circle</span>
              <h1 className="mk-auth__title" style={{ marginTop: 8 }}>Đăng ký thành công</h1>
              <p className="mk-auth__sub">
                Mã tổ chức của bạn: <b style={{ color: 'var(--tk-heading)' }}>{createdSlug}</b>
              </p>
              <Link to="/login">
                <Button type="primary" block>Đến trang đăng nhập</Button>
              </Link>
            </div>
          ) : (
            <>
              <h1 className="mk-auth__title">Đăng ký công ty 🚀</h1>
              <p className="mk-auth__sub">Tạo không gian làm việc cho công ty của bạn.</p>

              {submitError ? <Alert type="error" message={submitError} style={{ marginBottom: 18 }} /> : null}

              <form onSubmit={handleSubmit(onSubmit)}>
                <div style={{ marginBottom: 16 }}>
                  <label className="mk-auth__label">Tên công ty</label>
                  <Controller name="companyName" control={control} render={({ field }) => <Input {...field} placeholder="Công ty Du lịch..." />} />
                  {errors.companyName ? <div className="mk-auth__err">{errors.companyName.message}</div> : null}
                </div>
                <div style={{ marginBottom: 16 }}>
                  <label className="mk-auth__label">Mã tổ chức (slug)</label>
                  <Controller name="slug" control={control} render={({ field }) => <Input {...field} placeholder="vd: demo-tour" />} />
                  {errors.slug ? <div className="mk-auth__err">{errors.slug.message}</div> : null}
                </div>
                <div style={{ marginBottom: 16 }}>
                  <label className="mk-auth__label">Họ tên quản trị viên</label>
                  <Controller name="adminFullName" control={control} render={({ field }) => <Input {...field} placeholder="Nguyễn Văn A" />} />
                  {errors.adminFullName ? <div className="mk-auth__err">{errors.adminFullName.message}</div> : null}
                </div>
                <div style={{ marginBottom: 16 }}>
                  <label className="mk-auth__label">Email quản trị viên</label>
                  <Controller name="adminEmail" control={control} render={({ field }) => <Input {...field} placeholder="admin@congty.vn" />} />
                  {errors.adminEmail ? <div className="mk-auth__err">{errors.adminEmail.message}</div> : null}
                </div>
                <div style={{ marginBottom: 22 }}>
                  <label className="mk-auth__label">Mật khẩu</label>
                  <Controller name="adminPassword" control={control} render={({ field }) => <Input.Password {...field} placeholder="••••••••" />} />
                  {errors.adminPassword ? <div className="mk-auth__err">{errors.adminPassword.message}</div> : null}
                </div>
                <Button type="primary" htmlType="submit" block loading={isSubmitting}>
                  Đăng ký công ty
                </Button>
              </form>

              <div className="mk-auth__foot">
                Đã có tài khoản?{' '}
                <Link to="/login" className="mk-auth__link">Đăng nhập</Link>
              </div>
            </>
          )}
        </div>
      </section>
    </div>
  );
}
