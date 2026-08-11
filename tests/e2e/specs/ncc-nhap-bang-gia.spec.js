import { test, expect } from '../fixtures.js';

/**
 * Nhập bảng giá dịch vụ của NCC từ tệp theo mẫu.
 *
 * Chủ dự án chốt bốn luật, bài này kiểm cả bốn:
 *  1. Excel/CSV đi theo MẪU do hệ thống đưa ra (đường PDF/DOCX dùng AI là bước sau).
 *  2. Dòng sai KHÔNG chặn cả lô — dòng đúng vẫn nhập được.
 *  3. Luôn có bước XEM TRƯỚC, không ghi thẳng.
 *  4. Nhập là NỐI THÊM, không thay bảng giá đang có.
 */
const KHACH_SAN = '1';

test.describe('Nhập bảng giá từ tệp', () => {
  test('Tải được tệp mẫu và mẫu mang đúng cột của loại NCC', async ({ trang: page }) => {
    await moTaoMoi(page);
    await page.selectOption('[name="Input.Type"]', KHACH_SAN);

    // Panel dịch vụ chỉ hiện khi SỬA, nên lấy mẫu qua chính đường dẫn nút đang trỏ tới.
    const res = await page.request.get('/nha-cung-cap/loai/tat-ca?handler=MauNhap&loaiNcc=1');
    expect(res.ok(), 'không tải được tệp mẫu').toBeTruthy();

    const noiDung = await res.text();
    expect(noiDung).toContain('Tên gói giá');
    expect(noiDung).toContain('Giá hợp đồng');
    expect(noiDung, 'mẫu khách sạn phải có cột riêng của khách sạn').toContain('Loại ngày');
    expect(noiDung, 'mẫu khách sạn không được có cột của vé máy bay').not.toContain('Hành trình');
  });

  test('Nhập tệp: xem trước rồi ghi, dòng sai được liệt kê riêng', async ({ trang: page }) => {
    const ma = 'E2E-NHAP-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, KHACH_SAN);
    await moSuaTheoMa(page, ma);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    const truoc = await page.locator('#ds-dv .tk-dong-dv').count();

    // Ba dòng: hai đúng, một thiếu giá hợp đồng.
    const csv = [
      'Tên gói giá,Số khách,Giá hợp đồng,Giá công bố,Tiền tệ,Ghi chú,Giai đoạn từ,Giai đoạn đến,Loại ngày,Chi phí NET/ngày,Giá bán/ngày',
      'Phòng Deluxe,2,1200000,1500000,VND,,2026-06-01,2026-08-31,Ngày lễ,1100000,1400000',
      'Phòng thiếu giá,2,,,,,,,,,',
      'Phòng Superior,2,900000,1100000,VND,,,,Ngày thường,,',
    ].join('\n');

    await page.locator('#tep-nhap').setInputFiles({
      name: 'bang-gia.csv', mimeType: 'text/csv', buffer: Buffer.from(csv, 'utf8'),
    });

    // --- Xem trước: KHÔNG được ghi gì ở bước này ---
    await expect(page.locator('#xem-truoc')).toBeVisible({ timeout: 20_000 });
    await expect(page.locator('#xt-tom-tat')).toContainText('2 dòng');
    await expect(page.locator('#xt-tom-tat')).toContainText('1 dòng lỗi');
    await expect(page.locator('#xt-hong')).toContainText('Dòng 3');
    await expect(page.locator('#ds-dv .tk-dong-dv'), 'xem trước mà đã ghi vào bảng giá').toHaveCount(truoc);

    // --- Ghi ---
    await page.locator('#btn-ghi-nhap').click();
    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(truoc + 2, { timeout: 25_000 });
    await expect(page.locator(`#ds-dv [name$=".PriceName"][value="Phòng Deluxe"]`)).toHaveCount(1);

    // Trường riêng theo loại phải đi cùng, không rơi mất khi qua đường nhập tệp.
    const dong = page.locator('#ds-dv .tk-dong-dv').filter({ has: page.locator('[value="Phòng Deluxe"]') });
    await expect(dong.locator('[name$=".DayType"]')).toHaveValue('Ngày lễ');
    await expect(dong.locator('[name$=".PeriodFrom"]')).toHaveValue('2026-06-01');
  });

  /** Nhập là NỐI THÊM: dòng đang có phải còn nguyên sau khi nhập. */
  test('Nhập không xoá mất bảng giá đang có', async ({ trang: page }) => {
    const ma = 'E2E-NHAP2-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, KHACH_SAN);
    await moSuaTheoMa(page, ma);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    // Dòng có sẵn, thêm bằng tay rồi lưu.
    await page.locator('#btn-them-dv').click();
    await page.locator('#ds-dv .tk-dong-dv').last().locator('[name$=".PriceName"]').fill('Dòng có sẵn');
    await page.locator('#ds-dv .tk-dong-dv').last().locator('[name$=".ContractPrice"]').fill('100000');
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(1, { timeout: 20_000 });

    await page.locator('#tep-nhap').setInputFiles({
      name: 'them.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from('Tên gói giá,Giá hợp đồng\nDòng nhập thêm,200000\n', 'utf8'),
    });
    await expect(page.locator('#xem-truoc')).toBeVisible({ timeout: 20_000 });
    await page.locator('#btn-ghi-nhap').click();

    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(2, { timeout: 25_000 });
    await expect(page.locator('#ds-dv [name$=".PriceName"][value="Dòng có sẵn"]')).toHaveCount(1);
  });

  test('Bấm Bỏ ở bảng xem trước thì không ghi gì', async ({ trang: page }) => {
    const ma = 'E2E-NHAP3-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, KHACH_SAN);
    await moSuaTheoMa(page, ma);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    await page.locator('#tep-nhap').setInputFiles({
      name: 'bo.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from('Tên gói giá,Giá hợp đồng\nKhông nên ghi,300000\n', 'utf8'),
    });
    await expect(page.locator('#xem-truoc')).toBeVisible({ timeout: 20_000 });

    await page.locator('#btn-huy-nhap').click();
    await expect(page.locator('#xem-truoc')).toBeHidden();
    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(0);
  });

  test('Tệp thiếu cột bắt buộc thì báo một lần, không nhận dòng nào', async ({ trang: page }) => {
    const ma = 'E2E-NHAP4-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, KHACH_SAN);
    await moSuaTheoMa(page, ma);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    await page.locator('#tep-nhap').setInputFiles({
      name: 'thieu-cot.csv',
      mimeType: 'text/csv',
      buffer: Buffer.from('Tên gói giá\nPhòng A\nPhòng B\n', 'utf8'),
    });

    await expect(page.locator('#xem-truoc')).toBeVisible({ timeout: 20_000 });
    await expect(page.locator('#xt-hong')).toContainText('Giá hợp đồng');
    await expect(page.locator('#btn-ghi-nhap')).toBeDisabled();
  });
});

async function moTaoMoi(page) {
  await page.goto('/nha-cung-cap/loai/tat-ca');
  await page.evaluate(() => window.oc.open(null));
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

async function taoNcc(page, ma, loai) {
  await moTaoMoi(page);
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

/**
 * Đường AI: tài liệu báo giá (PDF/DOC/DOCX) không có mẫu cố định — mỗi NCC trình bày một kiểu — nên
 * model dựng lại thành bảng theo đúng cột của mẫu, rồi đi tiếp qua CHÍNH lớp kiểm của đường CSV.
 *
 * Gắn @ai vì bài này gọi model thật. Khẳng định cố ý LỎNG: model đọc được bao nhiêu dòng là chuyện
 * của model, thứ bài này chốt là đường đi có thông suốt không — bóc chữ, dựng bảng đúng cột, ra bảng
 * xem trước, và KHÔNG ghi gì trước khi người dùng duyệt.
 */
test.describe('@ai Nhập bảng giá từ tài liệu', () => {
  test('Tải file Word báo giá thì AI dựng được bảng xem trước', async ({ trang: page }) => {
    test.setTimeout(120_000);

    const ma = 'E2E-AI-' + String(Date.now()).slice(-6);
    await taoNcc(page, ma, KHACH_SAN);
    await moSuaTheoMa(page, ma);
    await expect(page.locator('#pnl-dv')).toBeVisible({ timeout: 20_000 });

    await page.locator('#tep-nhap').setInputFiles('fixtures/bao-gia-khach-san.docx');

    // Model mất vài giây tới vài chục giây; chờ rộng tay.
    await expect(page.locator('#xem-truoc')).toBeVisible({ timeout: 90_000 });
    await expect(page.locator('#xt-tom-tat')).toContainText(/Đọc được \d+ dòng/);

    // Chưa duyệt thì chưa được ghi gì — luật này không phụ thuộc model đoán đúng hay sai.
    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(0);

    const soDong = await page.locator('#xt-dong tr').count();
    expect(soDong, 'AI không dựng được dòng nào từ tài liệu').toBeGreaterThan(0);

    await page.locator('#btn-ghi-nhap').click();
    await expect(page.locator('#ds-dv .tk-dong-dv')).toHaveCount(soDong, { timeout: 30_000 });
  });
});
