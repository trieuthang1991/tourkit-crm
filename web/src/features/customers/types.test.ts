import { describe, expect, it } from 'vitest';
import { customerFormSchema, customerSchema, customerTypeLabel } from './types';

describe('customer schemas', () => {
  it('parses a customer with CRM lists + aggregates', () => {
    const c = customerSchema.parse({
      id: crypto.randomUUID(),
      code: 'KH_ABCD1234',
      fullName: 'Nguyễn A',
      phone: null,
      customerType: 1,
      source: 'Facebook',
      tag: 'VIP',
      tempBalance: 500000,
      email: 'a@x.com',
      address: 'Hà Nội',
      dateOfBirth: null,
      idCardNumber: '0123',
      passportNumber: 'B123',
      passportExpiry: null,
      nationality: 'Việt Nam',
      gender: 'Nam',
      city: 'Hà Nội',
      marketGroup: null,
      initialNeed: null,
      collaboratorName: null,
      campaign: null,
      branch: null,
      group: null,
      department: null,
      createdBy: null,
      createdByName: 'Admin',
      segments: ['B2B', 'VIP'],
      tags: ['Nóng'],
      assignedTo: ['u1'],
      assignedToNames: ['Trần B'],
      createdAt: new Date().toISOString(),
      purchaseCount: 3,
      revenue: 15000000,
      lastCareAt: null,
      lastCareContent: null,
    });
    expect(c.code).toBe('KH_ABCD1234');
    expect(c.segments).toEqual(['B2B', 'VIP']);
    expect(c.assignedToNames).toEqual(['Trần B']);
    expect(c.purchaseCount).toBe(3);
  });

  it('form requires fullName and accepts list fields', () => {
    const base = {
      phone: null,
      customerType: 0,
      source: null,
      tag: null,
      tempBalance: 0,
      email: null,
      address: null,
      dateOfBirth: null,
      idCardNumber: null,
      passportNumber: null,
      passportExpiry: null,
      nationality: null,
      gender: null,
      city: null,
      marketGroup: null,
      initialNeed: null,
      collaboratorName: null,
      campaign: null,
      branch: null,
      group: null,
      department: null,
      segments: [],
      tags: [],
      assignedTo: [],
    };
    expect(customerFormSchema.safeParse({ ...base, fullName: '' }).success).toBe(false);
    expect(customerFormSchema.safeParse({ ...base, fullName: 'A', segments: ['B2B'], assignedTo: ['u1'] }).success).toBe(true);
  });

  it('maps customer type labels', () => {
    expect(customerTypeLabel(0)).toBe('Cá nhân');
    expect(customerTypeLabel(3)).toBe('CTV');
  });
});
