import { clsx, type ClassValue } from 'clsx';
import { twMerge } from 'tailwind-merge';

/** Gộp class Tailwind (clsx + tailwind-merge) — nền cho toàn bộ primitive shadcn. */
export function cn(...inputs: ClassValue[]): string {
  return twMerge(clsx(inputs));
}
