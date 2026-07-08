import { describe, expect, it } from 'vitest';
import { registerTenantFormSchema } from './registrationApi';

describe('registerTenantFormSchema', () => {
  it('accepts a valid registration form', () => {
    const result = registerTenantFormSchema.safeParse({
      companyName: 'Công ty ABC',
      slug: 'abc-tour',
      adminEmail: 'admin@abc.vn',
      adminPassword: 'matkhau123',
      adminFullName: 'Nguyễn Văn A',
    });
    expect(result.success).toBe(true);
  });

  it('rejects an empty slug', () => {
    const result = registerTenantFormSchema.safeParse({
      companyName: 'Công ty ABC',
      slug: '',
      adminEmail: 'admin@abc.vn',
      adminPassword: 'matkhau123',
      adminFullName: 'Nguyễn Văn A',
    });
    expect(result.success).toBe(false);
  });

  it('rejects an invalid email', () => {
    const result = registerTenantFormSchema.safeParse({
      companyName: 'Công ty ABC',
      slug: 'abc-tour',
      adminEmail: 'not-an-email',
      adminPassword: 'matkhau123',
      adminFullName: 'Nguyễn Văn A',
    });
    expect(result.success).toBe(false);
  });

  it('rejects a short password', () => {
    const result = registerTenantFormSchema.safeParse({
      companyName: 'Công ty ABC',
      slug: 'abc-tour',
      adminEmail: 'admin@abc.vn',
      adminPassword: '123',
      adminFullName: 'Nguyễn Văn A',
    });
    expect(result.success).toBe(false);
  });
});
