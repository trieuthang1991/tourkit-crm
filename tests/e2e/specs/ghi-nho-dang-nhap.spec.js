import { test, expect } from '../fixtures.js';
import { TAI_KHOAN } from '../fixtures.js';

/**
 * Ô "Ghi nhớ đăng nhập" quyết định cookie phiên hay cookie có hạn.
 *
 * Chỉ nhìn thấy được ở trình duyệt thật: cookie phiên không mang Expires/Max-Age nên Playwright trả
 * `expires === -1`, còn cookie có hạn thì trả mốc thời gian. Kiểm thử ở tầng C# không thấy khác biệt
 * này vì cả hai đường đều "đăng nhập thành công".
 *
 * Không dùng fixture `trang` (đã đăng nhập sẵn) — bài này cần tự điều khiển ô tick.
 */
const TEN_COOKIE = 'tourkit_auth';

async function dangNhap(page, ghiNho) {
  await page.goto('/dang-nhap');
  await page.locator('#formAuthentication [name="Input.Email"]').fill(TAI_KHOAN.email);
  await page.locator('#formAuthentication [name="Input.Password"]').fill(TAI_KHOAN.matKhau);

  if (ghiNho) {
    await page.locator('#remember-me').check();
  }

  await page.locator('#formAuthentication button[type="submit"]').click();
  await page.waitForURL(/tong-quan|ban-lam-viec/, { timeout: 30_000 });
}

async function cookieDangNhap(page) {
  const ds = await page.context().cookies();
  const c = ds.find((x) => x.name === TEN_COOKIE);
  expect(c, `không tìm thấy cookie ${TEN_COOKIE}`).toBeTruthy();
  return c;
}

test.describe('Ghi nhớ đăng nhập', () => {
  test('Không tick thì cookie chỉ sống trong phiên trình duyệt', async ({ page }) => {
    await dangNhap(page, false);

    const c = await cookieDangNhap(page);
    expect(c.expires, 'cookie có hạn dù không tick ghi nhớ').toBe(-1);
  });

  test('Có tick thì cookie có hạn, sống qua lần đóng trình duyệt', async ({ page }) => {
    await dangNhap(page, true);

    const c = await cookieDangNhap(page);
    expect(c.expires, 'tick ghi nhớ mà cookie vẫn chỉ theo phiên').toBeGreaterThan(0);
  });

  /**
   * Cookie đăng nhập phải HttpOnly: JavaScript không đọc được thì kịch bản chèn mã cũng không lấy
   * được phiên của người dùng. Đây là lý do phiên nằm ở cookie chứ không phải localStorage.
   */
  test('Cookie đăng nhập không đọc được từ JavaScript', async ({ page }) => {
    await dangNhap(page, false);

    const c = await cookieDangNhap(page);
    expect(c.httpOnly, 'cookie đăng nhập không đặt HttpOnly').toBe(true);
    expect(c.sameSite, 'cookie đăng nhập nên đặt SameSite=Lax').toBe('Lax');

    const doDuoc = await page.evaluate((ten) => document.cookie.includes(ten), TEN_COOKIE);
    expect(doDuoc, 'JavaScript đọc được cookie đăng nhập').toBe(false);
  });

  /** Không có gì nhạy cảm nằm lại ở kho lưu trữ phía trình duyệt. */
  test('Không lưu thông tin nhạy cảm ở localStorage / sessionStorage', async ({ page }) => {
    await dangNhap(page, false);
    await page.goto('/nha-cung-cap/loai/tat-ca');

    const kho = await page.evaluate(() => {
      const gom = (s) => Object.keys(s).map((k) => k + '=' + s.getItem(k)).join('\n');
      return gom(localStorage) + '\n' + gom(sessionStorage);
    });

    for (const xau of [/password/i, /matkhau/i, /apikey/i, /secret/i, /bearer/i, /xai-/, /sk-[A-Za-z0-9]{16,}/]) {
      expect(kho, `kho phía trình duyệt chứa dữ liệu khớp ${xau}`).not.toMatch(xau);
    }

    expect(kho, 'mật khẩu tài khoản bị lưu ở phía trình duyệt').not.toContain(TAI_KHOAN.matKhau);
  });
});
