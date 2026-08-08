import { defineConfig } from '@playwright/test';

const CONG = process.env.TK_PORT || '5199';
const BASE = process.env.TK_BASE_URL || `http://localhost:${CONG}`;

export default defineConfig({
  testDir: './specs',
  // Gọi AI thật mất 15-20 giây một lượt với model suy luận.
  timeout: 120_000,
  expect: { timeout: 15_000 },

  // KHÔNG chạy song song: cùng một cơ sở dữ liệu, và các bài AI dùng chung hạn mức theo người dùng.
  // Chạy song song thì hai bài sửa cùng một bản ghi, hoặc bài này làm bài kia chạm trần hạn mức.
  workers: 1,
  fullyParallel: false,

  // Thất bại vì mạng chập chờn thì thử lại; thất bại vì lỗi thật thì lần nào cũng đỏ.
  retries: process.env.CI ? 1 : 0,

  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : [['list'], ['html', { open: 'never' }]],

  use: {
    baseURL: BASE,
    locale: 'vi-VN',
    timezoneId: 'Asia/Ho_Chi_Minh',
    // Chạy hiện hình bằng: npm run xem
    trace: 'retain-on-failure',
    video: 'retain-on-failure',
    screenshot: 'only-on-failure',
    // Dùng Chrome đã cài trên máy, khỏi tải bản riêng của Playwright.
    channel: 'chrome',
    viewport: { width: 1600, height: 950 },
  },

  // Tự khởi động máy chủ nếu chưa chạy. Đang chạy sẵn thì dùng lại — khỏi phải chờ build mỗi lần.
  webServer: {
    command: `dotnet run --project ../../src/TourKit.Api --no-launch-profile --urls ${BASE}`,
    url: `${BASE}/dang-nhap`,
    reuseExistingServer: true,
    timeout: 180_000,
    env: { ASPNETCORE_ENVIRONMENT: 'Development' },
    stdout: 'ignore',
    stderr: 'pipe',
  },
});
