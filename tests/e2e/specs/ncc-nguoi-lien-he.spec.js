import { test, expect } from '../fixtures.js';

/**
 * Khối "Thông tin liên hệ" nhiều dòng — hệ cũ có ở CẢ 4 màn sửa NCC, dựng một dòng mỗi người
 * (`foreach (var item in dataServices)` trong EditHotel.aspx). Bản mới trước đó chỉ có một ô chuỗi.
 *
 * Bài này kiểm thứ kiểm thử đơn vị không chạm tới: đánh số name="Contacts[i]." khi thêm/bỏ dòng,
 * model binding danh sách, cột jsonb, và nạp ngược vào form.
 */
test.describe('Người liên hệ nhiều dòng', () => {
  test('Thêm hai người rồi lưu thì mở lại còn đủ cả hai', async ({ trang: page }) => {
    const ma = 'E2E-LH-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'NCC ' + ma);

    await themNguoi(page, { ten: 'Nguyễn A', chucVu: 'Giám đốc', sdt: '0900000001' });
    await themNguoi(page, { ten: 'Trần B', chucVu: 'Kế toán', sdt: '0900000002' });

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#ds-lh .tk-dong-lh')).toHaveCount(2, { timeout: 20_000 });
    await expect(page.locator('#ds-lh [name="Contacts[0].FullName"]')).toHaveValue('Nguyễn A');
    await expect(page.locator('#ds-lh [name="Contacts[1].Position"]')).toHaveValue('Kế toán');
  });

  /**
   * Bỏ người ĐẦU rồi lưu: dòng còn lại phải tụt chỉ số về 0. Model binding của .NET dừng ở chỉ số
   * đầu tiên bị khuyết, nên quên đánh số lại là người phía sau biến mất mà không báo gì.
   */
  test('Bỏ người đầu thì người còn lại không biến mất', async ({ trang: page }) => {
    const ma = 'E2E-LH2-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'NCC ' + ma);

    await themNguoi(page, { ten: 'Bỏ đi', chucVu: 'Tạm' });
    await themNguoi(page, { ten: 'Giữ lại', chucVu: 'Trưởng phòng' });

    await page.locator('#ds-lh .tk-dong-lh').first().locator('.js-xoa-lh').click();
    await expect(page.locator('#ds-lh [name="Contacts[0].FullName"]')).toHaveValue('Giữ lại');

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#ds-lh .tk-dong-lh')).toHaveCount(1, { timeout: 20_000 });
    await expect(page.locator('#ds-lh [name="Contacts[0].FullName"]')).toHaveValue('Giữ lại');
  });

  /** Bấm "Thêm người" rồi bỏ trống thì không lưu dòng rỗng — nếu không, form đầy dòng trống dần. */
  test('Dòng bỏ trống hoàn toàn thì không được lưu', async ({ trang: page }) => {
    const ma = 'E2E-LH3-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'NCC ' + ma);

    await themNguoi(page, { ten: 'Có thật', chucVu: 'Giám đốc' });
    await page.locator('#btn-them-lh').click();   // dòng thứ hai để trống

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#ds-lh .tk-dong-lh')).toHaveCount(1, { timeout: 20_000 });
  });

  /**
   * Chỉ khai danh sách mà bỏ trống ô "Người liên hệ" thì cột ContactPerson lấy người đầu — nếu
   * không, NCC đó biến mất khỏi kết quả tìm theo tên người liên hệ (tìm chạy ở SQL trên cột đó).
   */
  test('Bỏ trống ô người liên hệ thì lấy người đầu trong danh sách', async ({ trang: page }) => {
    const ma = 'E2E-LH4-' + String(Date.now()).slice(-6);
    const ten = 'Phạm D ' + String(Date.now()).slice(-4);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'NCC ' + ma);
    await themNguoi(page, { ten: ten, chucVu: 'Giám đốc' });

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    // Tìm theo TÊN NGƯỜI LIÊN HỆ — chạy ở SQL trên cột ContactPerson.
    await page.goto('/nha-cung-cap/loai/tat-ca?q=' + encodeURIComponent(ten));
    await expect(page.locator('.tabulator-row:not(.tabulator-calcs)').filter({ hasText: ma }))
      .toHaveCount(1, { timeout: 20_000 });
  });

  test('Mở NCC khác thì không sót người liên hệ của NCC trước', async ({ trang: page }) => {
    const ma = 'E2E-LH5-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);
    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'NCC ' + ma);
    await themNguoi(page, { ten: 'Chỉ của NCC này', chucVu: 'Giám đốc' });
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#ds-lh .tk-dong-lh')).toHaveCount(1, { timeout: 20_000 });

    await page.evaluate(() => window.oc.open(null));
    await expect(page.locator('#ds-lh .tk-dong-lh')).toHaveCount(0);
    await expect(page.locator('#lh-trong')).toBeVisible();
  });
});

async function themNguoi(page, { ten, chucVu, sdt }) {
  await page.locator('#btn-them-lh').click();
  const dong = page.locator('#ds-lh .tk-dong-lh').last();
  if (ten) { await dong.locator('[name$=".FullName"]').fill(ten); }
  if (chucVu) { await dong.locator('[name$=".Position"]').fill(chucVu); }
  if (sdt) { await dong.locator('[name$=".Phone"]').fill(sdt); }
}

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
