import axios from 'axios';

export function errorMessage(error: unknown, fallback = 'Đã có lỗi xảy ra'): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { title?: string; detail?: string; errors?: Record<string, string[]> } | undefined;
    if (data?.errors) {
      const first = Object.values(data.errors)[0]?.[0];
      if (first) {
        return first;
      }
    }
    return data?.detail ?? data?.title ?? error.message ?? fallback;
  }
  return error instanceof Error ? error.message : fallback;
}
