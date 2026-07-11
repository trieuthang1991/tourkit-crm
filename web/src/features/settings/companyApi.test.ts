import { describe, expect, it } from 'vitest';
import { companyProfileSchema } from './companyApi';

describe('companyProfileSchema', () => {
  it('parses a full profile', () => {
    const p = companyProfileSchema.parse({
      name: 'Công ty Du lịch ABC',
      shortName: 'ABC',
      address: '123 Lê Lợi',
      hotline: '1900 1234',
      email: 'info@abc.vn',
      website: 'abc.vn',
      taxCode: '0301234567',
      legalRepName: 'Nguyễn Văn A',
      legalRepTitle: 'Giám đốc',
      licenseNumber: 'GP-123',
      bankAccount: 'VCB 001',
    });
    expect(p.name).toBe('Công ty Du lịch ABC');
    expect(p.taxCode).toBe('0301234567');
  });

  it('allows an empty/unset profile (nullable optional fields)', () => {
    const p = companyProfileSchema.parse({
      name: '',
      shortName: null,
      address: null,
      hotline: null,
      email: null,
      website: null,
      taxCode: null,
      legalRepName: null,
      legalRepTitle: null,
      licenseNumber: null,
      bankAccount: null,
    });
    expect(p.name).toBe('');
    expect(p.taxCode).toBeNull();
  });
});
