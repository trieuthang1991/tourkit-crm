import { test, expect } from '../fixtures.js';

/**
 * Đường dẫn sâu từ màn Nhà cung cấp sang Bảng giá NCC phải lọc đúng NCC đó.
 *
 * Nút "Bảng giá dịch vụ" trên menu hành động của mỗi NCC điều hướng kèm ?providerId=…, và handler
 * dữ liệu phía server ĐÃ đọc tham số đó từ lâu. Nhưng lưới gọi handler bằng URL tương đối
 * '?handler=Data' — mà URL bắt đầu bằng '?' THAY THẾ toàn bộ chuỗi truy vấn — nên providerId của
 * trang không bao giờ đi kèm. Người dùng chọn đúng NCC ở màn trước, sang màn sau thấy bảng giá của
 * TẤT CẢ NCC và phải gõ lại tên lần nữa.
 *
 * Bài này chốt ở chỗ quan trọng nhất: tham số có THỰC SỰ tới server không. Kiểm ô lọc hiển thị gì
 * thì mới chỉ chứng minh giao diện, còn dữ liệu trả về vẫn có thể là của mọi NCC.
 */
test('/bang-gia-ncc?providerId= lọc đúng nhà cung cấp đó', async ({ trang: page }) => {
  // Lấy một NCC có thật, không ghi cứng id — dữ liệu mẫu dựng lại là id đổi.
  await page.goto('/nha-cung-cap/loai/tat-ca');
  const ncc = await page.evaluate(async () => {
    const r = await fetch('?handler=Data&draw=1&start=0&length=1');
    const j = await r.json();
    return (j.data || [])[0];
  });
  test.skip(!ncc?.id, 'Chưa có nhà cung cấp nào trong dữ liệu.');

  // Bắt mọi lượt gọi dữ liệu của lưới bảng giá.
  const goiDuLieu = [];
  page.on('request', (r) => {
    if (/bang-gia-ncc.*handler=Data/.test(r.url())) goiDuLieu.push(r.url());
  });

  await page.goto(`/bang-gia-ncc?providerId=${ncc.id}`);

  // Ô lọc phải hiện sẵn tên NCC — không có nhãn thì người dùng chỉ thấy một ô trống và tưởng chưa lọc.
  await expect(page.locator('#select2-f-providerId-container'))
    .toContainText(ncc.name ?? '', { timeout: 20_000 });

  // Và quan trọng hơn: lượt gọi dữ liệu ĐẦU TIÊN đã mang theo providerId. Nạp sau khi lưới đã tải
  // thì người dùng thấy danh sách nhảy một nhịp và tốn một lượt truy vấn thừa.
  await expect.poll(() => goiDuLieu.length, { timeout: 20_000 }).toBeGreaterThan(0);
  expect(goiDuLieu[0], 'lượt gọi dữ liệu đầu tiên không mang providerId').toContain(`providerId=${ncc.id}`);
});
