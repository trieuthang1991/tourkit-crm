import { test, expect } from '../fixtures.js';

/**
 * Trường riêng theo LOẠI nhà cung cấp — tái lập 4 màn sửa của hệ cũ trong một form.
 *
 * Hệ cũ (KojiCRM) có Edit/EditHotel/EditPlaneTicket/EditVoucher dùng chung ~14 ô ở đầu form rồi mỗi
 * màn thêm ô của riêng nó: khách sạn có "Năm xây dựng"/"Quốc gia", xe có "Thông tin xe"/"Loại xe"
 * chọn nhiều hạng ghế. Bản mới gộp một form và ẩn/hiện khối theo loại đang chọn.
 *
 * Bài này kiểm chuỗi đầy đủ mà kiểm thử đơn vị không chạm tới: ẩn/hiện ở trình duyệt, tên ô gửi lên,
 * model binding danh sách nhiều giá trị, cột JSON, rồi nạp NGƯỢC lại vào form khi mở sửa.
 */
const KHACH_SAN = '1';
const VAN_CHUYEN = '2';
const VOUCHER = '7';

async function moTaoMoi(page) {
  await page.goto('/nha-cung-cap/loai/tat-ca');
  await page.evaluate(() => window.oc.open(null));
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

test.describe('Trường riêng theo loại NCC', () => {
  test('Đổi loại thì khối trường tương ứng hiện, khối kia ẩn', async ({ trang: page }) => {
    await moTaoMoi(page);

    await page.selectOption('[name="Input.Type"]', KHACH_SAN);
    await expect(page.locator('#frm .tk-loai-ks')).toBeVisible();
    await expect(page.locator('#frm .tk-loai-xe')).toBeHidden();

    await page.selectOption('[name="Input.Type"]', VAN_CHUYEN);
    await expect(page.locator('#frm .tk-loai-xe')).toBeVisible();
    await expect(page.locator('#frm .tk-loai-ks')).toBeHidden();

    // Loại không có trường riêng thì không khối nào hiện — đừng bày ô rỗng vô nghĩa.
    await page.selectOption('[name="Input.Type"]', '3');   // Nhà hàng
    await expect(page.locator('#frm .tk-loai-ks')).toBeHidden();
    await expect(page.locator('#frm .tk-loai-xe')).toBeHidden();
  });

  test('Khách sạn: lưu năm xây dựng + quốc gia thì mở lại vẫn còn', async ({ trang: page }) => {
    const ma = 'E2E-KS-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'Khách sạn ' + ma);
    await page.selectOption('[name="Input.Type"]', KHACH_SAN);
    await page.fill('[name="Input.BuiltYear"]', '2015');
    await page.fill('[name="Input.Country"]', 'Việt Nam');
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#frm .tk-loai-ks')).toBeVisible();
    await expect(page.locator('[name="Input.BuiltYear"]')).toHaveValue('2015');
    await expect(page.locator('[name="Input.Country"]')).toHaveValue('Việt Nam');
  });

  test('Vận chuyển: chọn nhiều hạng ghế thì mở lại đủ dấu tích', async ({ trang: page }) => {
    const ma = 'E2E-XE-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'Nhà xe ' + ma);
    await page.selectOption('[name="Input.Type"]', VAN_CHUYEN);
    await page.selectOption('[name="Input.VehicleOwnership"]', 'Xe đối tác');
    await page.check('#frm [name="Input.VehicleTypes"][value="16 chỗ"]');
    await page.check('#frm [name="Input.VehicleTypes"][value="45 chỗ"]');
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#frm .tk-loai-xe')).toBeVisible();
    await expect(page.locator('[name="Input.VehicleOwnership"]')).toHaveValue('Xe đối tác');
    await expect(page.locator('#frm [name="Input.VehicleTypes"][value="16 chỗ"]')).toBeChecked();
    await expect(page.locator('#frm [name="Input.VehicleTypes"][value="45 chỗ"]')).toBeChecked();
    await expect(page.locator('#frm [name="Input.VehicleTypes"][value="4 chỗ"]')).not.toBeChecked();
  });

  /**
   * Dấu tích của bản ghi TRƯỚC không được sót lại khi mở bản ghi sau. Bộ điền của tk.form gán .val()
   * cho từng ô, việc đó không đụng tới thuộc tính checked — nên nếu form không tự xoá tích thì lỗi
   * này chỉ lộ ra khi mở lần lượt hai NCC, đúng thao tác thật của người dùng.
   */
  test('Mở NCC khác thì không sót dấu tích của NCC trước', async ({ trang: page }) => {
    const ma = 'E2E-XE2-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);
    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'Nhà xe ' + ma);
    await page.selectOption('[name="Input.Type"]', VAN_CHUYEN);
    await page.check('#frm [name="Input.VehicleTypes"][value="29 chỗ"]');
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#frm [name="Input.VehicleTypes"][value="29 chỗ"]')).toBeChecked();

    // Mở sang một NCC khác ngay sau đó.
    await page.evaluate(() => window.oc.open({ id: null, type: 2, name: 'tạm', code: 'tam' }));
    await expect(page.locator('#frm [name="Input.VehicleTypes"][value="29 chỗ"]')).not.toBeChecked();
  });
});

/**
 * Voucher là loại NCC hệ cũ có màn sửa riêng (EditVoucher.aspx) mà bản mới còn thiếu.
 *
 * Hai ô của nó được hệ cũ đánh dấu * — nên ở form gộp chúng bắt buộc CÓ ĐIỀU KIỆN: chỉ khi loại là
 * Voucher. Đây là chỗ dễ sai nhất, vì "bắt buộc" mà ô đang ẩn thì người dùng thấy báo lỗi nhưng
 * không biết ô nằm đâu.
 */
test.describe('Loại NCC Voucher', () => {
  test('Chọn Voucher thì hiện Class Hotel + Tên dự án', async ({ trang: page }) => {
    await moTaoMoi(page);

    await page.selectOption('[name="Input.Type"]', VOUCHER);
    await expect(page.locator('#frm .tk-loai-vc')).toBeVisible();
    await expect(page.locator('#frm .tk-loai-ks')).toBeHidden();
  });

  test('Là Voucher mà bỏ trống hai ô đó thì phải báo lỗi, không lưu', async ({ trang: page }) => {
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', 'E2E-VC-' + String(Date.now()).slice(-6));
    await page.fill('[name="Input.Name"]', 'Voucher thiếu ô');
    await page.selectOption('[name="Input.Type"]', VOUCHER);
    await page.locator('#frm button[type="submit"]').click();

    // Form phải còn mở — chưa lưu.
    await expect(page.locator('#oc')).toHaveClass(/show/);
    await expect(page.locator('#frm [name="Input.HotelClass"]')).toHaveClass(/is-invalid/);
  });

  /**
   * Chiều ngược lại, quan trọng không kém: 6 loại còn lại KHÔNG được bị đòi điền hai ô đang ẩn.
   * Nếu luật bắt buộc gắn cứng thay vì theo điều kiện, mọi NCC khách sạn sẽ không lưu nổi.
   */
  test('Loại khác thì không bị đòi điền hai ô đó', async ({ trang: page }) => {
    const ma = 'E2E-KS3-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'Khách sạn bình thường');
    await page.selectOption('[name="Input.Type"]', KHACH_SAN);
    await page.locator('#frm button[type="submit"]').click();

    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });
  });

  test('Lưu Voucher đủ hai ô thì mở lại vẫn còn', async ({ trang: page }) => {
    const ma = 'E2E-VC2-' + String(Date.now()).slice(-6);
    await moTaoMoi(page);

    await page.fill('[name="Input.Code"]', ma);
    await page.fill('[name="Input.Name"]', 'Gói nghỉ dưỡng ' + ma);
    await page.selectOption('[name="Input.Type"]', VOUCHER);
    await page.fill('[name="Input.HotelClass"]', '4 sao');
    await page.fill('[name="Input.ProjectName"]', 'Dự án Hạ Long');
    await page.locator('#frm button[type="submit"]').click();
    await expect(page.locator('#oc')).not.toHaveClass(/show/, { timeout: 25_000 });

    await moSuaTheoMa(page, ma);
    await expect(page.locator('#frm .tk-loai-vc')).toBeVisible();
    await expect(page.locator('[name="Input.HotelClass"]')).toHaveValue('4 sao');
    await expect(page.locator('[name="Input.ProjectName"]')).toHaveValue('Dự án Hạ Long');
  });

  /** Tab lọc theo loại phải có Voucher, và route /nha-cung-cap/loai/voucher phải mở được. */
  test('Có tab Voucher và route riêng của nó chạy được', async ({ trang: page }) => {
    await page.goto('/nha-cung-cap/loai/voucher');
    await expect(page.locator('.nav-link[data-type="7"]')).toHaveCount(1);
  });
});

/** Tìm đúng NCC theo mã rồi mở form sửa qua menu hành động của lưới. */
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
