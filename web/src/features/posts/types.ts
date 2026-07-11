import { z } from 'zod';

export const postCategorySchema = z.object({
  id: z.string().uuid(),
  name: z.string(),
  slug: z.string(),
  sortOrder: z.number(),
  status: z.number(),
});
export type PostCategory = z.infer<typeof postCategorySchema>;

export const postCategoryCreateSchema = z.object({
  name: z.string().min(1, 'Bắt buộc'),
  slug: z.string().min(1, 'Bắt buộc'),
  sortOrder: z.number(),
});
export type PostCategoryCreateForm = z.infer<typeof postCategoryCreateSchema>;

export const postSchema = z.object({
  id: z.string().uuid(),
  title: z.string(),
  slug: z.string(),
  summary: z.string().nullable(),
  body: z.string(),
  categoryId: z.string().nullable(),
  categoryName: z.string().nullable(),
  status: z.number(),
  publishedAt: z.string().nullable(),
  likeCount: z.number(),
});
export type Post = z.infer<typeof postSchema>;

export const postFormSchema = z.object({
  title: z.string().min(1, 'Bắt buộc'),
  slug: z.string().min(1, 'Bắt buộc'),
  summary: z.string().nullable().transform((v) => (v ? v : null)),
  body: z.string().min(1, 'Bắt buộc'),
  categoryId: z.string().nullable(),
  status: z.number(),
  likeCount: z.number(),
});
export type PostForm = z.infer<typeof postFormSchema>;

export const postCommentSchema = z.object({
  id: z.string().uuid(),
  postId: z.string().uuid(),
  authorName: z.string(),
  content: z.string(),
  isApproved: z.boolean(),
  createdAt: z.string(),
});
export type PostComment = z.infer<typeof postCommentSchema>;

export const postCommentCreateSchema = z.object({
  authorName: z.string().min(1, 'Bắt buộc'),
  content: z.string().min(1, 'Bắt buộc'),
  isApproved: z.boolean(),
});
export type PostCommentCreateForm = z.infer<typeof postCommentCreateSchema>;

export const POST_STATUS_OPTIONS = [
  { value: 0, label: 'Nháp' },
  { value: 1, label: 'Đã xuất bản' },
];
export const postStatusLabel = (v: number) => POST_STATUS_OPTIONS.find((o) => o.value === v)?.label ?? '';
