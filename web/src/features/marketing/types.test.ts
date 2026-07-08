import { describe, expect, it } from 'vitest';
import { campaignCreateSchema, campaignSchema } from './types';

describe('campaign schemas', () => {
  it('parses a campaign', () => {
    const c = campaignSchema.parse({
      id: crypto.randomUUID(),
      name: 'Khuyến mãi hè',
      channel: 1,
      subject: 'Ưu đãi',
      body: 'Nội dung',
      status: 1,
    });
    expect(c.channel).toBe(1);
  });

  it('create schema requires name and body', () => {
    expect(
      campaignCreateSchema.safeParse({ name: '', channel: 1, subject: null, body: '' }).success,
    ).toBe(false);
  });
});
