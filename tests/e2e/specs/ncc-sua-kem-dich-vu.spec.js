import { test, expect } from '../fixtures.js';

/**
 * Sửa nhà cung cấp CÙNG bảng dịch vụ trong một form — tái lập EditHotel.aspx của hệ cũ.
 *
 * Hệ cũ (KojiCRM) để thông tin NCC và panel "SẢN PHẨM/DỊCH VỤ" chung một màn, uspInsertHotel ghi cả
 * hai trong một transaction. Bản mới từng tách đôi: sửa NCC ở một màn, sửa bảng giá ở màn khác.
 */
test.describe('Sửa NCC kèm bảng dịch vụ', () => {
  async function moSuaNccDau(page) {
    await page.goto('/nha-cung-cap/loai/tat-ca');

    const o = page.locator('.tabulator-row:not(.tabulator-calcs) .tabulator-cell[tabulator-field="__act"]').first();
    await o.waitFor({ state: 'visible', timeout: 20_000 });
    await o.click();

    const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Sửa/ }).first();
    await muc.waitFor({ state: 'visible', timeout: 10_000 });
    await muc.click();
    await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
  }

  test('Mở sửa NCC thì panel dịch vụ hiện và nạp bảng giá của chính NCC đó', async ({ trang: page }) => {
    await moSuaNccDau(page);

    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    // Hoặc có dòng, hoặc có dòng chữ "chưa có dịch vụ nào" — im lặng mới là hỏng.
    const coDong = page.locator('#ds-dv .tk-dong-dv');
    const trong = page.locator('#dv-trong:visible');
    await expect(coDong.first().or(trong)).toBeVisible({ timeout: 20_000 });
  });

  /**
   * Panel KHÔNG được hiện khi tạo mới: form tạo chưa có đường ghi kèm dịch vụ, bày ra một panel mà
   * bấm Lưu không ghi gì là nói dối người dùng.
   */
  test('Tạo mới NCC thì panel dịch vụ không hiện', async ({ trang: page }) => {
    await page.goto('/nha-cung-cap/loai/tat-ca');
    await page.evaluate(() => window.oc.open(null));
    await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });

    await expect(page.locator('#pnl-dv')).toBeHidden();
  });

  /**
   * Thêm một dòng rồi lưu: NCC và dịch vụ phải ghi cùng lượt, và dòng mới phải còn đó khi mở lại.
   *
   * Đây là điều bài kiểm thử đơn vị KHÔNG chứng minh được — nó kiểm tầng dịch vụ, còn đây kiểm cả
   * chuỗi: đánh số name="Services[i]", model binding, transaction, rồi nạp lại.
   */
  test('Thêm một dòng dịch vụ rồi lưu thì dòng đó còn khi mở lại', async ({ trang: page }) => {
    await moSuaNccDau(page);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    const truoc = await page.locator('#ds-dv .tk-dong-dv').count();
    const ten = 'e2e-goi-' + String(Date.now()).slice(-6);

    await page.locator('#btn-them-dv').click();
    const dongMoi = page.locator('#ds-dv .tk-dong-dv').last();
    await dongMoi.locator('[name$=".PriceName"]').fill(ten);
    await dongMoi.locator('[name$=".ContractPrice"]').fill('123000');
    await dongMoi.locator('[name$=".PublicPrice"]').fill('150000');

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    // Mở lại chính NCC đó — dòng vừa thêm phải có mặt.
    await moSuaNccDau(page);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });
    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(truoc + 1, { timeout: 20_000 });
    await expect(page.locator(`#ds-dv [name$=".PriceName"][value="${ten}"]`)).toHaveCount(1);
  });
});
