import { describe, expect, it } from 'vitest';
import { postCommentCreateSchema, postCommentSchema, postFormSchema, postSchema } from './types';

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
      likeCount: 12,
    });
    expect(p.title).toBe('Khuyến mãi hè');
    expect(p.status).toBe(1);
    expect(p.likeCount).toBe(12);
  });

  it('form requires title, slug and body', () => {
    expect(postFormSchema.safeParse({ title: '', slug: 's', summary: null, body: 'b', categoryId: null, status: 0, likeCount: 0 }).success).toBe(false);
    expect(postFormSchema.safeParse({ title: 't', slug: 's', summary: null, body: '', categoryId: null, status: 0, likeCount: 0 }).success).toBe(false);
    expect(postFormSchema.safeParse({ title: 't', slug: 's', summary: '', body: 'b', categoryId: null, status: 0, likeCount: 0 }).success).toBe(true);
  });
});

describe('post comment schemas', () => {
  it('parses a comment', () => {
    const c = postCommentSchema.parse({
      id: crypto.randomUUID(),
      postId: crypto.randomUUID(),
      authorName: 'Nguyễn Văn A',
      content: 'Tour rất tuyệt!',
      isApproved: true,
      createdAt: new Date().toISOString(),
    });
    expect(c.authorName).toBe('Nguyễn Văn A');
    expect(c.isApproved).toBe(true);
  });

  it('create form requires author name and content', () => {
    expect(postCommentCreateSchema.safeParse({ authorName: '', content: 'x', isApproved: true }).success).toBe(false);
    expect(postCommentCreateSchema.safeParse({ authorName: 'A', content: '', isApproved: true }).success).toBe(false);
    expect(postCommentCreateSchema.safeParse({ authorName: 'A', content: 'x', isApproved: false }).success).toBe(true);
  });
});
