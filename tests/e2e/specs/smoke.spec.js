import { test, expect } from '../fixtures.js';

/**
 * Mở lần lượt các màn chính và bắt lỗi JavaScript.
 *
 * Bài này tổng quát hoá một lỗi thật: mọi trang Auth nạp pages-auth.js — mã mẫu của theme gọi một
 * thư viện chưa từng được nạp — nên mỗi lần mở trang đăng nhập trình duyệt đều ném lỗi. Không ai
 * thấy vì lỗi nằm trong console. Việc kiểm tra do fixture "loiTrang" làm tự động cho MỌI bài.
 */
const MAN_CHINH = [
  ['Tổng quan', '/tong-quan'],
  ['Bàn làm việc', '/ban-lam-viec'],
  ['Khách hàng', '/khach-hang'],
  ['Cơ hội bán hàng', '/co-hoi'],
  ['Đơn hàng', '/don-hang'],
  ['Báo giá', '/bao-gia'],
  ['Chuyến đi', '/chuyen-di'],
  ['Công nợ khách', '/cong-no-khach'],
  ['Công nợ nhà cung cấp', '/cong-no-ncc'],
  ['Dòng tiền', '/dong-tien'],
  ['Báo cáo tổng hợp', '/bao-cao-tong-hop'],
  ['Quỹ vé', '/quy-ve'],
  ['Nhà cung cấp', '/nha-cung-cap'],
  ['Công việc', '/cong-viec'],
  ['Lịch hẹn', '/lich-hen'],
  ['Thông báo', '/thong-bao'],
];

test.describe('Màn chính mở được, không lỗi JavaScript', () => {
  for (const [ten, duong] of MAN_CHINH) {
    test(ten, async ({ trang }) => {
      const res = await trang.goto(duong, { waitUntil: 'domcontentloaded' });

      expect(res?.status(), `${duong} trả mã lỗi`).toBeLessThan(400);
      // Bị đá về đăng nhập nghĩa là thiếu quyền hoặc mất phiên — không phải "mở được".
      expect(trang.url(), `${duong} bị chuyển về đăng nhập`).not.toContain('dang-nhap');
      await expect(trang.locator('body')).toBeVisible();
    });
  }
});

test('Trang đăng nhập không có lỗi JavaScript', async ({ page }) => {
  await page.goto('/dang-nhap');
  await expect(page.locator('#formAuthentication')).toBeVisible();
});

/**
 * Sai mật khẩu phải ở lại trang đăng nhập kèm thông báo. Trước đây form không có kiểm tra phía
 * trình duyệt nào cả, nên đây là lớp bảo vệ duy nhất còn lại của luồng này.
 */
test('Sai mật khẩu thì báo lỗi, không cho vào', async ({ page }) => {
  await page.goto('/dang-nhap');
  // Bám tên ô, không bám thứ tự: form đã bỏ ô "mã doanh nghiệp" (email nay là định danh toàn hệ
  // thống) và cách đếm theo vị trí làm bài này chết câm suốt từ đó.
  await page.locator('#formAuthentication [name="Input.Email"]').fill('admin@demo.vn');
  await page.locator('#formAuthentication [name="Input.Password"]').fill('sai-mat-khau-hoan-toan');
  await page.locator('#formAuthentication button[type="submit"]').click();

  await expect(page.locator('.alert-danger')).toBeVisible({ timeout: 20_000 });
  expect(page.url()).toContain('dang-nhap');
});
