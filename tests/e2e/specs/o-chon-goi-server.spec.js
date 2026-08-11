import { test, expect } from '../fixtures.js';

/**
 * Ô chọn nhà cung cấp GỌI SERVER thay vì nạp cả danh mục vào <option>.
 *
 * Nạp hết thì phải đặt trần, mà vượt trần là hỏng im lặng: ô vẫn hiện bình thường, chỉ thiếu lựa
 * chọn, người dùng thấy "không có NCC đó" rồi kết luận sai là dữ liệu chưa nhập.
 *
 * Nhưng đổi sang gọi server lại mở ra một bẫy KHÁC, nặng hơn: lúc mở sửa bản ghi cũ, ô rỗng vì chưa
 * gõ gì để nạp. Bấm Lưu là ghi null đè lên khoá ngoại — mất liên kết mà không có gì báo. Đúng kiểu
 * mất dữ liệu đã xảy ra ở form sửa chuyến đi và ở ô tỉnh thành. Bài này chốt cả hai chiều.
 */
test.describe('Ô chọn NCC gọi server', () => {
  test('Trang không còn nhúng cả danh mục NCC vào HTML', async ({ trang: page }) => {
    await page.goto('/ve-may-bay-doan');

    const soOption = await page.locator('#f-providerRef option').count();
    expect(soOption, 'ô lọc NCC vẫn đang nhúng sẵn option — chưa chuyển sang gọi server')
      .toBeLessThanOrEqual(1);
  });

  test('Gõ từ khoá thì server trả kết quả khớp', async ({ trang: page }) => {
    await page.goto('/ve-may-bay-doan');

    // Lấy tên một NCC có thật để gõ tìm, không ghi cứng.
    const res = await page.request.get('/ve-may-bay-doan?handler=ProviderLookup&q=');
    expect(res.ok(), 'endpoint tra cứu NCC không trả về được').toBeTruthy();

    const ds = (await res.json()).results;
    expect(ds.length, 'không có nhà cung cấp nào để thử').toBeGreaterThan(0);

    // Trần 20 dòng mỗi lượt: đây là ô gợi ý, không phải danh sách để đọc.
    expect(ds.length).toBeLessThanOrEqual(20);

    const tu = String(ds[0].text).slice(0, 4);
    const loc = await page.request.get('/ve-may-bay-doan?handler=ProviderLookup&q=' + encodeURIComponent(tu));
    const kq = (await loc.json()).results;
    expect(kq.length, `gõ "${tu}" mà không ra kết quả nào`).toBeGreaterThan(0);
  });

  /**
   * Mở sửa một vé đã gắn NCC: ô phải hiện sẵn TÊN nhà cung cấp đó, không được rỗng và không được
   * hiện ra một chuỗi id. Rỗng là dấu hiệu lần Lưu kế tiếp sẽ xoá mất liên kết.
   */
  test('Mở sửa bản ghi cũ thì ô NCC hiện sẵn tên, không rỗng', async ({ trang: page }) => {
    await page.goto('/ve-may-bay-doan');

    const dong = page.locator('.tabulator-row:not(.tabulator-calcs)');
    await dong.first().waitFor({ state: 'visible', timeout: 20_000 });

    // Tìm dòng có tên NCC hiển thị trong lưới.
    const coNcc = dong.filter({ has: page.locator('.tabulator-cell[tabulator-field="providerName"]') });
    const n = await coNcc.count();
    test.skip(n === 0, 'Chưa có vé nào gắn nhà cung cấp.');

    await coNcc.first().locator('.tabulator-cell[tabulator-field="__act"]').click();
    const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Sửa/ }).first();
    await muc.waitFor({ state: 'visible', timeout: 10_000 });
    await muc.click();
    await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });

    const $sel = page.locator('#frm [name="Input.ProviderRef"]');
    const gt = await $sel.inputValue();
    test.skip(!gt, 'Vé này không gắn nhà cung cấp.');

    // Có giá trị thì phải có đúng một option mang giá trị đó, và nhãn không phải chính cái id.
    const nhan = await $sel.locator('option:checked').innerText();
    expect(nhan.trim(), 'ô NCC hiện id thay vì tên — người dùng không đọc được').not.toBe(gt);
    expect(nhan.trim().length, 'ô NCC rỗng khi mở sửa — lần Lưu kế tiếp sẽ xoá mất liên kết')
      .toBeGreaterThan(0);
  });

  /** Endpoint tra cứu phải đòi quyền, không phải cửa sau đọc toàn bộ danh mục NCC. */
  test('Chưa đăng nhập thì không tra cứu được', async ({ page }) => {
    const res = await page.request.get('/ve-may-bay-doan?handler=ProviderLookup&q=a', { maxRedirects: 0 });
    expect([401, 403, 302].includes(res.status()),
      `endpoint tra cứu trả ${res.status()} cho người chưa đăng nhập`).toBeTruthy();
  });
});
