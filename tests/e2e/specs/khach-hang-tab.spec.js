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
   * Nội dung ô KHÔNG được lộ mã HTML ra ngoài.
   *
   * tk.trunc TRẢ VỀ HTML và đã tự escape bên trong. Bọc thêm tk.escape quanh nó là escape luôn cả
   * thẻ <span>, nên người dùng đọc thấy nguyên đoạn mã thay vì nội dung. Bảng vẫn dựng, vẫn có dòng,
   * nên mọi bài kiểm thử "có dữ liệu chưa" đều xanh — chỉ mắt người mới thấy sai.
   */
  test('Ô nội dung không lộ mã HTML ra ngoài', async ({ trang: page }) => {
    await moKhach(page);

    for (const [tab, bang] of [['#pane-don-hang', '#tbl-don'], ['#pane-cham-soc', '#tbl-cham']]) {
      await page.locator(`[data-bs-target="${tab}"]`).click();
      await expect(page.locator(`${bang} tbody tr`)).not.toHaveCount(0, { timeout: 20_000 });

      const chu = await page.locator(`${bang} tbody`).innerText();
      expect(chu, `${bang}: mã HTML hiện ra như chữ thường`).not.toContain('<span');
      expect(chu, `${bang}: mã HTML hiện ra như chữ thường`).not.toContain('class=');
    }
  });

  /**
   * Tiêu đề cột không được bị bóp xuống thành từng chữ một dòng.
   *
   * DataTables tự chia bề rộng khi cột không khai width, nên tiêu đề dài như "Đã thu / Còn nợ" bị
   * ép xuống nhiều dòng và bảng trông vỡ. Quy ước §3b đã ghi phải đặt width cố định.
   */
  test('Tiêu đề cột không bị bóp xuống nhiều dòng', async ({ trang: page }) => {
    await moKhach(page);
    await page.locator('[data-bs-target="#pane-don-hang"]').click();
    await expect(page.locator('#tbl-don tbody tr')).not.toHaveCount(0, { timeout: 20_000 });

    // Một ô tiêu đề một dòng cao khoảng 20–28px. Cao gấp đôi nghĩa là chữ đã xuống dòng.
    for (const nhan of ['Tour / Ngày đi', 'Đã thu / Còn nợ']) {
      const o = page.locator('#tbl-don thead th', { hasText: nhan }).first();
      const h = (await o.boundingBox())?.height ?? 0;
      expect(h, `tiêu đề "${nhan}" cao ${h}px — đang bị bóp xuống nhiều dòng`).toBeLessThan(56);
    }
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
