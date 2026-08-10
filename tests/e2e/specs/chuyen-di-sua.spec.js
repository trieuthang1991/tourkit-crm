import { test, expect } from '../fixtures.js';

/**
 * Mở sửa một chuyến thì ô NGÀY phải nạp lại được ngày cũ.
 *
 * Lưới chỉ trả về departureDateText/endDateText — chuỗi đã định dạng để hiển thị. Ô nhập tên là
 * Input.DepartureDate nên tk.form.open(data) đi tìm khoá 'departureDate', không thấy, ô mở ra TRỐNG.
 * Bấm Lưu là gửi rỗng và server ghi null đè lên ngày cũ: mở sửa rồi lưu mà KHÔNG ĐỔI GÌ cũng xoá
 * trắng ngày khởi hành và ngày về của chuyến.
 *
 * Bài o-bat-buoc/sua-xoa không bắt được vì hai ô này không mang dấu sao đỏ — không bắt buộc nhập,
 * nhưng mất thì vẫn là mất dữ liệu.
 */
test('/chuyen-di — mở sửa thì ô ngày phải giữ nguyên ngày cũ', async ({ trang: page }) => {
  await page.goto('/chuyen-di/loai/tat-ca');

  // Tìm một chuyến CÓ ngày khởi hành — chuyến chưa đặt ngày thì ô trống là đúng, không kết luận được.
  const chuyen = await page.evaluate(async () => {
    const r = await fetch('?handler=Data&draw=1&start=0&length=50');
    const j = await r.json();
    return (j.data || []).find((x) => x.departureDate);
  });
  test.skip(!chuyen, 'Chưa có chuyến nào đặt ngày khởi hành.');

  const dong = page.locator('.tabulator-row:not(.tabulator-calcs)', { hasText: chuyen.code }).first();
  await dong.waitFor({ state: 'visible', timeout: 20_000 });
  await dong.locator('.tabulator-cell[tabulator-field="__act"]').click();

  const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Sửa/ }).first();
  await muc.waitFor({ state: 'visible', timeout: 10_000 });
  await muc.click();
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });

  // Đọc giá trị THẬT trong ô gốc, không đọc ô hiển thị của flatpickr: ô gốc mới là thứ được gửi lên.
  const ngay = await page.evaluate(() =>
    document.querySelector('#frm [name="Input.DepartureDate"]')?.value ?? null);

  expect(ngay, 'mở sửa mà ô ngày khởi hành trống — bấm Lưu sẽ xoá mất ngày cũ').toBe(chuyen.departureDate);
});
