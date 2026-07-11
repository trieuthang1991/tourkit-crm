import { describe, expect, it } from 'vitest';
import { messageTemplateFormSchema, messageTemplateSchema } from './types';

describe('message template schemas', () => {
  it('parses a template', () => {
    const t = messageTemplateSchema.parse({
      id: crypto.randomUUID(),
      name: 'Chào mừng KH mới',
      channel: 1,
      subject: 'Xin chào',
      body: 'Cảm ơn quý khách',
    });
    expect(t.name).toBe('Chào mừng KH mới');
    expect(t.channel).toBe(1);
  });

  it('form requires name and body; empty subject → null', () => {
    expect(messageTemplateFormSchema.safeParse({ name: '', channel: 2, subject: null, body: 'b' }).success).toBe(false);
    expect(messageTemplateFormSchema.safeParse({ name: 't', channel: 2, subject: null, body: '' }).success).toBe(false);
    const ok = messageTemplateFormSchema.parse({ name: 't', channel: 2, subject: '', body: 'b' });
    expect(ok.subject).toBeNull();
  });
});
