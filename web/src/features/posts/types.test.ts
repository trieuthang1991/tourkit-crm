import { describe, expect, it } from 'vitest';
import { postFormSchema, postSchema } from './types';

describe('post schemas', () => {
  it('parses a post', () => {
    const p = postSchema.parse({
      id: crypto.randomUUID(),
      title: 'Khuyến mãi hè',
      slug: 'khuyen-mai-he',
      summary: null,
      body: 'Nội dung',
      categoryId: null,
      categoryName: null,
      status: 1,
      publishedAt: new Date().toISOString(),
    });
    expect(p.title).toBe('Khuyến mãi hè');
    expect(p.status).toBe(1);
  });

  it('form requires title, slug and body', () => {
    expect(postFormSchema.safeParse({ title: '', slug: 's', summary: null, body: 'b', categoryId: null, status: 0 }).success).toBe(false);
    expect(postFormSchema.safeParse({ title: 't', slug: 's', summary: null, body: '', categoryId: null, status: 0 }).success).toBe(false);
    expect(postFormSchema.safeParse({ title: 't', slug: 's', summary: '', body: 'b', categoryId: null, status: 0 }).success).toBe(true);
  });
});
