import { z } from 'zod';

export function pagedSchema<T extends z.ZodTypeAny>(item: T) {
  return z.object({
    items: z.array(item),
    total: z.number(),
    page: z.number(),
    size: z.number(),
  });
}

export type Paged<T> = { items: T[]; total: number; page: number; size: number };

export type PageParams = { page: number; size: number };

export const DEFAULT_PAGE: PageParams = { page: 1, size: 20 };
