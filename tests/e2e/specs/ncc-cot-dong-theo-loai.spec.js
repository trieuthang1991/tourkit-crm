import { test, expect } from '../fixtures.js';

/**
 * Cột riêng của DÒNG bảng giá theo loại NCC — bước 3 của việc gộp "Nhà cung cấp" + "Dịch vụ của NCC".
 *
 * Hệ cũ có hai mức trường riêng: mức NCC ở đầu form (năm xây dựng, loại xe) và mức DÒNG nằm trong
 * hàng tiêu đề bảng giá — khách sạn có "Giai đoạn từ/Đến"/"Loại ngày"/"Chi phí NET/Ngày", vé máy bay
 * có "Hành trình vé"/"Giờ đi-về"/"Hạn cắt cọc"/"Hành lý". Mỗi gói giá một bộ giá trị riêng.
 *
 * Bài này kiểm chuỗi mà kiểm thử đơn vị không chạm tới: ẩn/hiện ở trình duyệt, đánh số
 * name="Services[i]." cho ô mới, model binding DateOnly, cột jsonb, rồi nạp NGƯỢC vào form.
 */
const KHACH_SAN = '1';
const HANG_KHONG = '5';

test.describe('Cột riêng của dòng bảng giá', () => {
  test('Khách sạn: lưu giai đoạn + loại ngày + giá NET/ngày thì mở lại vẫn còn', async ({ trang: page }) => {
    const ma = 'E2E-DG-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, KHACH_SAN);
    await moSuaTheoMa(page, ma);

    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });
    await page.locator('#btn-them-dv').click();

    const dong = page.locator('#ds-dv .tk-dong-dv').last();
    await expect(dong.locator('.tk-cot-ks')).toBeVisible();
    await expect(dong.locator('.tk-cot-ve')).toBeHidden();

    await dong.locator('[name$=".PriceName"]').fill('Phòng Deluxe');
    await dong.locator('[name$=".PeriodFrom"]').fill('2026-06-01');
    await dong.locator('[name$=".PeriodTo"]').fill('2026-08-31');
    await dong.locator('[name$=".DayType"]').selectOption('Ngày lễ');
    await dong.locator('[name$=".NetCostPerDay"]').fill('1200000');

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    const lai = page.locator('#ds-dv .tk-dong-dv').first();
    await expect(lai.locator('[name$=".PeriodFrom"]')).toHaveValue('2026-06-01', { timeout: 20_000 });
    await expect(lai.locator('[name$=".PeriodTo"]')).toHaveValue('2026-08-31');
    await expect(lai.locator('[name$=".DayType"]')).toHaveValue('Ngày lễ');
    await expect(lai.locator('[name$=".NetCostPerDay"]')).toHaveValue('1200000');
  });

  test('Hàng không: hiện cột vé, không hiện cột khách sạn, và lưu lại đủ', async ({ trang: page }) => {
    const ma = 'E2E-VE-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, HANG_KHONG);
    await moSuaTheoMa(page, ma);

    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });
    await page.locator('#btn-them-dv').click();

    const dong = page.locator('#ds-dv .tk-dong-dv').last();
    await expect(dong.locator('.tk-cot-ve')).toBeVisible();
    await expect(dong.locator('.tk-cot-ks')).toBeHidden();

    await dong.locator('[name$=".PriceName"]').fill('VN1546 series');
    await dong.locator('[name$=".TicketType"]').selectOption('Vé series');
    await dong.locator('[name$=".Route"]').fill('SGN-HAN-SGN');
    await dong.locator('[name$=".DepartTime"]').fill('08:30');
    await dong.locator('[name$=".DepositDeadline"]').fill('2026-05-20');
    await dong.locator('[name$=".Baggage"]').fill('23kg');

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    const lai = page.locator('#ds-dv .tk-dong-dv').first();
    await expect(lai.locator('[name$=".Route"]')).toHaveValue('SGN-HAN-SGN', { timeout: 20_000 });
    await expect(lai.locator('[name$=".TicketType"]')).toHaveValue('Vé series');
    await expect(lai.locator('[name$=".DepartTime"]')).toHaveValue('08:30');
    await expect(lai.locator('[name$=".DepositDeadline"]')).toHaveValue('2026-05-20');
    await expect(lai.locator('[name$=".Baggage"]')).toHaveValue('23kg');
  });

  /**
   * Thêm hai dòng rồi bỏ dòng ĐẦU: các ô mới cũng phải được đánh số lại. Model binding của .NET dừng
   * ở chỉ số đầu tiên bị khuyết, nên nếu quên thì dòng còn lại mất sạch trường riêng mà không báo gì.
   */
  test('Bỏ dòng giữa chừng thì dòng còn lại vẫn giữ đủ trường riêng', async ({ trang: page }) => {
    const ma = 'E2E-DG2-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, KHACH_SAN);
    await moSuaTheoMa(page, ma);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    await page.locator('#btn-them-dv').click();
    await page.locator('#ds-dv .tk-dong-dv').last().locator('[name$=".PriceName"]').fill('Bỏ đi');
    await page.locator('#btn-them-dv').click();

    const giuLai = page.locator('#ds-dv .tk-dong-dv').last();
    await giuLai.locator('[name$=".PriceName"]').fill('Giữ lại');
    await giuLai.locator('[name$=".DayType"]').selectOption('Cuối tuần');

    // Bỏ dòng ĐẦU — dòng "Giữ lại" tụt chỉ số từ 1 xuống 0.
    await page.locator('#ds-dv .tk-dong-dv').first().locator('.js-xoa-dv').click();

    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(1, { timeout: 20_000 });
    await expect(page.locator('#ds-dv [name$=".PriceName"]')).toHaveValue('Giữ lại');
    await expect(page.locator('#ds-dv [name$=".DayType"]')).toHaveValue('Cuối tuần');
  });
});

async function taoNcc(page, ma, loai) {
  await page.goto('/nha-cung-cap/loai/tat-ca');
  await page.evaluate(() => window.oc.open(null));
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });

  await page.fill('[name="Input.Code"]', ma);
  await page.fill('[name="Input.Name"]', 'NCC ' + ma);
  await page.selectOption('[name="Input.Type"]', loai);
  await page.locator('#frm button[type="submit"]').click();
  await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });
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
