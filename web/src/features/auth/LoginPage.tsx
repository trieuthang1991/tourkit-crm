import { zodResolver } from '@hookform/resolvers/zod';
import { Alert, Button, Input } from '../../shared/ui/antd';
import { useState } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { Link, useNavigate } from 'react-router-dom';
import { z } from 'zod';
import { useAuth } from './AuthContext';

const loginFormSchema = z.object({
  tenantSlug: z.string().min(1, 'Vui lòng nhập mã tổ chức'),
  email: z.string().min(1, 'Vui lòng nhập email').email('Email không hợp lệ'),
  password: z.string().min(1, 'Vui lòng nhập mật khẩu'),
});

type LoginFormValues = z.infer<typeof loginFormSchema>;

const BRAND_FEATS = [
  { icon: 'groups', text: 'CRM & chăm sóc khách hàng tập trung' },
  { icon: 'calculate', text: 'Báo giá — điều hành tour — công nợ liền mạch' },
  { icon: 'insights', text: 'Báo cáo doanh thu & hiệu suất thời gian thực' },
];

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
    <div className="mk mk-auth">
      <aside className="mk-auth__brand">
        <span className="mk-auth__glow" />
        <Link to="/gioi-thieu" className="mk-auth__brandtop" style={{ textDecoration: 'none' }}>
          <span className="mk-auth__mark">T</span>
          <span className="mk-auth__brandname">TourKit</span>
        </Link>
        <div className="mk-auth__brandmid">
          <h2>Điều hành công ty lữ hành trên một nền tảng duy nhất.</h2>
          <p>Từ cơ hội bán hàng đến báo giá, đơn tour, điều hành dịch vụ và công nợ — TourKit CRM giúp đội của bạn chạy nhanh và chuẩn hơn mỗi ngày.</p>
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

      <section className="mk-auth__panel">
        <div className="mk-auth__card">
          <div className="mk-auth__logo-sm">
            <span className="mk-auth__mark">T</span>
            <span>TourKit</span>
          </div>
          <h1 className="mk-auth__title">Chào mừng trở lại 👋</h1>
          <p className="mk-auth__sub">Đăng nhập vào không gian làm việc của công ty bạn.</p>

          {submitError ? <Alert type="error" message={submitError} style={{ marginBottom: 18 }} /> : null}

          <form onSubmit={handleSubmit(onSubmit)}>
            <div style={{ marginBottom: 18 }}>
              <label className="mk-auth__label">Mã tổ chức</label>
              <Controller name="tenantSlug" control={control} render={({ field }) => <Input {...field} placeholder="vd: demo-tour" />} />
              {errors.tenantSlug ? <div className="mk-auth__err">{errors.tenantSlug.message}</div> : null}
            </div>
            <div style={{ marginBottom: 18 }}>
              <label className="mk-auth__label">Email</label>
              <Controller name="email" control={control} render={({ field }) => <Input {...field} placeholder="email@congty.vn" />} />
              {errors.email ? <div className="mk-auth__err">{errors.email.message}</div> : null}
            </div>
            <div style={{ marginBottom: 22 }}>
              <label className="mk-auth__label">Mật khẩu</label>
              <Controller name="password" control={control} render={({ field }) => <Input.Password {...field} placeholder="••••••••" />} />
              {errors.password ? <div className="mk-auth__err">{errors.password.message}</div> : null}
            </div>
            <Button type="primary" htmlType="submit" block loading={isSubmitting}>
              Đăng nhập
            </Button>
          </form>

          <div className="mk-auth__foot">
            Chưa có tài khoản?{' '}
            <Link to="/register" className="mk-auth__link">Đăng ký công ty</Link>
          </div>
        </div>
      </section>
    </div>
  );
}
