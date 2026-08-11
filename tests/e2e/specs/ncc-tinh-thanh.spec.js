import { test, expect } from '../fixtures.js';

/**
 * Ô "Tỉnh thành" là DANH SÁCH, không phải gõ tay.
 *
 * Gõ tay thì "Hà Nội", "hà nội", "HN", "TP Hà Nội" thành bốn giá trị khác nhau, mà bộ lọc ở màn danh
 * sách chạy Contains trên chính cột đó — thống kê theo địa bàn hết tin được. Dữ liệu thật lúc chuyển
 * đổi còn lẫn cả thành phố/thị xã ("Đà Lạt", "Nha Trang", "Hạ Long") chứ không riêng tỉnh.
 */
test.describe('Tỉnh thành chọn từ danh sách', () => {
  test('Ô tỉnh thành là select và có đủ 34 đơn vị cấp tỉnh', async ({ trang: page }) => {
    await moTaoMoi(page);

    const o = page.locator('#frm [name="Input.Province"]');
    await expect(o).toHaveJSProperty('tagName', 'SELECT');

    // 34 đơn vị + một dòng "— Không —".
    await expect(o.locator('option')).toHaveCount(35);
    await expect(o.locator('option[value="Hà Nội"]')).toHaveCount(1);
    await expect(o.locator('option[value="Đồng Nai"]')).toHaveCount(1);

    // Đơn vị đã sáp nhập từ 01/07/2025 thì không được còn trong danh sách.
    await expect(o.locator('option[value="Bà Rịa - Vũng Tàu"]')).toHaveCount(0);
    await expect(o.locator('option[value="Bình Dương"]')).toHaveCount(0);
  });

  test('Chọn rồi lưu thì mở lại vẫn đúng tỉnh đó', async ({ trang: page }) => {
    const ma = 'E2E-TT-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'NCC ' + ma);
    await page.selectOption('#frm [name="Input.Province"]', 'Khánh Hòa');
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#frm [name="Input.Province"]')).toHaveValue('Khánh Hòa');
  });

  /**
   * Bản ghi cũ mang giá trị NGOÀI danh sách phải giữ được.
   *
   * Chuyển ô gõ tay sang select mà không xử lý thì mở sửa những bản ghi đó select rỗng, bấm Lưu là
   * xoá trắng tỉnh thành và không có gì báo. Đây là kiểu mất dữ liệu im lặng đã xảy ra một lần ở màn
   * chuyến đi, nên chốt bằng bài kiểm thử chứ không tin vào mắt.
   */
  test('Giá trị cũ ngoài danh sách không bị xoá khi mở sửa rồi lưu', async ({ trang: page }) => {
    const ma = 'E2E-TT2-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);
    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'NCC ' + ma);
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    // Giả lập bản ghi di trú: mở form với giá trị không có trong danh sách.
    await page.goto('/nha-cung-cap/loai/tat-ca?q=' + encodeURIComponent(ma));
    await page.evaluate((m) => window.oc.open({ id: null, code: m, name: 'NCC ' + m, type: 1, province: 'Đà Lạt' }), ma);
    await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });

    const o = page.locator('#frm [name="Input.Province"]');
    await expect(o).toHaveValue('Đà Lạt');
    // Nhãn do tk.form.keepLegacyOption sinh — dùng lại cơ chế sẵn có, không tự viết bản thứ hai.
    await expect(o.locator('option[value="Đà Lạt"]')).toContainText('ngoài danh mục');
  });

  test('Bộ lọc ở màn danh sách cũng là danh sách, không phải ô gõ', async ({ trang: page }) => {
    await page.goto('/nha-cung-cap/loai/tat-ca');
    await expect(page.locator('#f-province')).toHaveJSProperty('tagName', 'SELECT');
  });
});

async function moTaoMoi(page) {
  await page.goto('/nha-cung-cap/loai/tat-ca');
  await page.evaluate(() => window.oc.open(null));
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

async function moSuaTheoMa(page, ma) {
  await page.goto('/nha-cung-cap/loai/tat-ca?q=' + encodeURIComponent(ma));
  const dong = page.locator('.tabulator-row:not(.tabulator-calcs)').filter({ hasText: ma });
  await dong.first().waitFor({ state: 'visible', timeout: 20_000 });
  await dong.first().locator('.tabulator-cell[tabulator-field="__act"]').click();
  const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Sửa/ }).first();
  await muc.waitFor({ state: 'visible', timeout: 10_000 });
  await muc.click();
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}
