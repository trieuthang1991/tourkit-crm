import { test, expect, idKhachHangBatKy } from '../fixtures.js';

/**
 * Ba tab ở màn chi tiết khách hàng phải THỰC SỰ chuyển nội dung.
 *
 * Trước đây chúng là markup chết copy từ theme: href="javascript:void(0)", không tab-pane, không
 * JavaScript nào bắt sự kiện. Bấm vào không xảy ra gì — và người dùng không có cách nào phân biệt
 * "khách này chưa có đơn nào" với "tính năng chưa làm". Bài này chốt lại sự phân biệt đó.
 */
test.describe('Tab ở màn chi tiết khách hàng', () => {
  async function moKhach(page) {
    const id = await idKhachHangBatKy(page);
    await page.goto(`/khach-hang/${id}`);
    await expect(page.locator('#pane-tong-quan')).toBeVisible();
    return id;
  }

  test('Bấm "Đơn hàng" thì hiện bảng đơn, không phải không có gì', async ({ trang: page }) => {
    await moKhach(page);

    await page.locator('[data-bs-target="#pane-don-hang"]').click();

    await expect(page.locator('#pane-don-hang')).toBeVisible();
    await expect(page.locator('#pane-tong-quan')).toBeHidden();

    // Bảng phải được DataTables dựng xong: hoặc có dòng dữ liệu, hoặc có dòng "không có dữ liệu".
    // Cả hai đều là câu trả lời; im lặng mới là hỏng.
    await expect(page.locator('#tbl-don tbody tr')).not.toHaveCount(0, { timeout: 20_000 });
  });

  test('Bấm "Chăm sóc" thì hiện lịch chăm sóc', async ({ trang: page }) => {
    await moKhach(page);

    await page.locator('[data-bs-target="#pane-cham-soc"]').click();

    await expect(page.locator('#pane-cham-soc')).toBeVisible();
    await expect(page.locator('#tbl-cham tbody tr')).not.toHaveCount(0, { timeout: 20_000 });
  });

  /**
   * Hai bảng chỉ được gọi khi người dùng mở đúng tab của nó. Nạp sẵn cả ba lúc vào trang là bắt mọi
   * lần xem hồ sơ phải trả giá cho hai truy vấn mà phần lớn thời gian không ai dùng tới.
   */
  test('Không gọi dữ liệu đơn/chăm sóc khi chưa mở tab', async ({ trang: page }) => {
    const goi = [];
    page.on('request', (r) => {
      if (/handler=(Orders|Cares)/.test(r.url())) goi.push(r.url());
    });

    await moKhach(page);
    await page.waitForTimeout(1500);

    expect(goi, 'gọi dữ liệu của tab chưa mở').toEqual([]);

    await page.locator('[data-bs-target="#pane-don-hang"]').click();
    await expect(page.locator('#tbl-don tbody tr')).not.toHaveCount(0, { timeout: 20_000 });
    expect(goi.some((u) => /handler=Orders/.test(u)), 'mở tab rồi mà không gọi dữ liệu').toBe(true);
    expect(goi.some((u) => /handler=Cares/.test(u)), 'gọi cả tab chưa mở').toBe(false);
  });
});
