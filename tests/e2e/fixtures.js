import { test as base, expect } from '@playwright/test';

export const TAI_KHOAN = {
  slug: process.env.TK_SLUG || 'demo-tour',
  email: process.env.TK_EMAIL || 'admin@demo.vn',
  matKhau: process.env.TK_PASSWORD || 'Demo@12345',
};

/**
 * Lỗi trình duyệt được BỎ QUA. Danh sách này phải luôn ngắn và mỗi dòng phải có lý do —
 * thêm bừa vào đây là vô hiệu hoá đúng cái mà bộ kiểm thử này sinh ra để bắt.
 */
const BO_QUA = [
  /favicon/i,                       // thiếu icon không ảnh hưởng nghiệp vụ
  /net::ERR_(ABORTED|CONNECTION)/,  // điều hướng cắt ngang request đang chạy
];

/**
 * Mọi bài kiểm thử đều theo dõi lỗi JavaScript và tự đỏ nếu có.
 *
 * Đây là lý do chính bộ e2e này tồn tại: bộ kiểm thử C# không chạm tới JavaScript, nên một file
 * không parse được hay một thư viện thiếu chỉ lộ ra khi có người mở trình duyệt. Đã có ba lỗi thật
 * lọt qua theo đúng kiểu đó.
 */
export const test = base.extend({
  loiTrang: [async ({ page }, use) => {
    const loi = [];
    page.on('pageerror', (e) => loi.push(String(e.message)));
    page.on('console', (m) => { if (m.type() === 'error') { loi.push(m.text()); } });

    await use(loi);

    const that = loi.filter((l) => !BO_QUA.some((r) => r.test(l)));
    expect(that, 'Trang có lỗi JavaScript').toEqual([]);
  }, { auto: true }],

  /** Trang đã đăng nhập sẵn. */
  trang: async ({ page }, use) => {
    await dangNhap(page);
    await use(page);
  },
});

export { expect };

/**
 * Đăng nhập qua đúng giao diện thật, không đi tắt bằng cookie dựng sẵn.
 *
 * Bám vào TÊN ô chứ không phải thứ tự. Bản trước đếm theo vị trí (ô số 0, ô số 1), nên khi bỏ ô
 * "mã doanh nghiệp" — email nay là định danh toàn hệ thống — nó lặng lẽ điền mã vào ô email và cả
 * bộ e2e chết ở bước đăng nhập, mọi bài đều báo "hết giờ chờ chuyển trang" thay vì nói ra nguyên do.
 */
export async function dangNhap(page, tk = TAI_KHOAN) {
  await page.goto('/dang-nhap');
  await page.locator('#formAuthentication [name="Input.Email"]').fill(tk.email);
  await page.locator('#formAuthentication [name="Input.Password"]').fill(tk.matKhau);
  await page.locator('#formAuthentication button[type="submit"]').click();

  // Chờ CẢ thành công lẫn báo lỗi. Chỉ chờ chuyển trang thì khi sai mật khẩu, bài test treo tới hết
  // giờ rồi báo "timeout" — che mất thông báo lỗi đang hiện ngay trên màn hình.
  const loi = page.locator('#formAuthentication .text-danger, .alert-danger');
  await Promise.race([
    page.waitForURL(/tong-quan|ban-lam-viec/, { timeout: 30_000 }),
    loi.first().waitFor({ state: 'visible', timeout: 30_000 }),
  ]);

  if (!/tong-quan|ban-lam-viec/.test(page.url())) {
    throw new Error(`Đăng nhập thất bại với ${tk.email}: ${(await loi.first().innerText().catch(() => '')) || 'không rõ lý do'}`);
  }
}

/**
 * Lấy id một khách hàng có thật từ danh sách.
 *
 * Cố ý KHÔNG ghi cứng id: dữ liệu mẫu dựng lại là id đổi, và một bài kiểm thử đỏ vì id cũ không
 * còn thì người đọc tưởng tính năng hỏng.
 */
export async function idKhachHangBatKy(page) {
  await page.goto('/khach-hang');

  // Link nằm trong ô lưới do JavaScript dựng, không phải HTML sẵn có. Bám đúng lớp của ô để không
  // vớ nhầm nút tĩnh "/khach-hang/ban-cu" nằm trên thanh công cụ.
  const link = page.locator('a.tk-cell-main[href*="/khach-hang/"]');
  await link.first().waitFor({ timeout: 30_000 });

  const href = await link.first().getAttribute('href');
  const id = (href || '').match(/([0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12})/i)?.[1];
  expect(id, 'Không tìm được khách hàng nào trong danh sách').toBeTruthy();
  return id;
}

/** Ghi một dòng trao đổi qua giao diện, để hồ sơ có dữ liệu cho AI đọc. */
export async function themTraoDoi(page, noiDung) {
  const o = page.locator('[data-comments] textarea').first();
  await o.waitFor({ timeout: 20_000 });
  await o.fill(noiDung);
  await page.locator('[data-comments] button:has-text("Gửi")').first().click();
  await expect(page.locator('[data-comments]')).toContainText(noiDung.slice(0, 30), { timeout: 20_000 });
}
