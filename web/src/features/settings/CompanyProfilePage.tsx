import { App, Button, Card, Form, Input } from 'antd';
import { useEffect } from 'react';
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
  const [form] = Form.useForm<CompanyProfile>();

  useEffect(() => {
    if (profile.data) {
      form.setFieldsValue(profile.data);
    }
  }, [profile.data, form]);

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
        <Form form={form} layout="vertical" onFinish={onFinish} disabled={!canManage}>
          {FIELDS.map((f) => (
            <Form.Item
              key={f.name}
              name={f.name}
              label={f.label}
              rules={f.required ? [{ required: true, message: 'Bắt buộc' }] : undefined}
            >
              <Input />
            </Form.Item>
          ))}
          {canManage ? (
            <Button type="primary" htmlType="submit" loading={save.isPending}>
              Lưu
            </Button>
          ) : null}
        </Form>
      </Card>
    </>
  );
}
