import { App, Button, Card, Input } from '../../shared/ui/antd';
import { useEffect } from 'react';
import { Controller, useForm } from 'react-hook-form';
import { errorMessage } from '../../shared/api/problem';
import { PageHeader } from '../../shared/ui/PageHeader';
import { useAuth } from '../auth/AuthContext';
import { useCompanyProfile, useSaveCompanyProfile } from './companyApi';
import type { CompanyProfile } from './companyApi';

const FIELDS: { name: keyof CompanyProfile; label: string; required?: boolean }[] = [
  { name: 'name', label: 'Tên công ty', required: true },
  { name: 'shortName', label: 'Tên viết tắt' },
  { name: 'address', label: 'Địa chỉ' },
  { name: 'hotline', label: 'Hotline' },
  { name: 'email', label: 'Email' },
  { name: 'website', label: 'Website' },
  { name: 'taxCode', label: 'Mã số thuế' },
  { name: 'legalRepName', label: 'Người đại diện' },
  { name: 'legalRepTitle', label: 'Chức vụ người đại diện' },
  { name: 'licenseNumber', label: 'Số giấy phép' },
  { name: 'bankAccount', label: 'Tài khoản ngân hàng' },
];

export function CompanyProfilePage() {
  const { message } = App.useApp();
  const { has } = useAuth();
  const canManage = has('company.manage');
  const profile = useCompanyProfile();
  const save = useSaveCompanyProfile();
  const { control, handleSubmit, reset, formState: { errors } } = useForm<CompanyProfile>();

  useEffect(() => {
    if (profile.data) reset(profile.data);
  }, [profile.data, reset]);

  async function onFinish(values: CompanyProfile) {
    try {
      await save.mutateAsync(values);
      message.success('Đã lưu hồ sơ công ty');
    } catch (e) {
      message.error(errorMessage(e));
    }
  }

  return (
    <>
      <PageHeader title="Hồ sơ công ty" />
      <Card loading={profile.isLoading} style={{ maxWidth: 640 }}>
        <form onSubmit={handleSubmit(onFinish)}>
          {FIELDS.map((f) => (
            <div key={f.name} style={{ marginBottom: 16 }}>
              <label style={{ display: 'block', marginBottom: 6, fontSize: 12.5, fontWeight: 500, color: 'var(--tk-text-strong)' }}>
                {f.label} {f.required ? <span style={{ color: 'var(--tk-danger)' }}>*</span> : null}
              </label>
              <Controller
                name={f.name}
                control={control}
                rules={f.required ? { required: 'Bắt buộc' } : undefined}
                render={({ field }) => <Input {...field} value={field.value ?? ''} disabled={!canManage} />}
              />
              {errors[f.name] ? <div style={{ marginTop: 6, fontSize: 12, color: 'var(--tk-danger)' }}>{errors[f.name]?.message as string}</div> : null}
            </div>
          ))}
          {canManage ? (
            <Button type="primary" htmlType="submit" loading={save.isPending}>
              Lưu
            </Button>
          ) : null}
        </form>
      </Card>
    </>
  );
}
