import { test, expect, idKhachHangBatKy } from '../fixtures.js';

/**
 * Lưu khách hàng từ MÀN CHI TIẾT.
 *
 * Bài này sinh ra từ một lỗi thật người dùng báo: điền đủ mọi trường có dấu * đỏ nhưng bấm Lưu chỉ
 * hiện "Lỗi kết nối, thử lại". Nguyên nhân là form gửi vào một handler không tồn tại; Razor trả HTML
 * kèm mã 200 nên JavaScript đọc JSON thất bại.
 *
 * Điểm mấu chốt: phải kiểm tra dữ liệu ĐÃ LƯU sau khi tải lại trang. Chỉ xem panel có đóng không thì
 * không đủ — nó đóng cả khi lưu hỏng.
 */
test.describe('Sửa và lưu khách hàng', () => {
  test('Lưu được từ màn chi tiết và dữ liệu còn sau khi tải lại', async ({ trang }) => {
    const id = await idKhachHangBatKy(trang);

    const goiLuu = [];
    trang.on('response', (r) => {
      if (r.url().includes('handler=Save')) {
        goiLuu.push({ ma: r.status(), kieu: (r.headers()['content-type'] || '').split(';')[0] });
      }
    });

    await trang.goto(`/khach-hang/${id}`);
    const form = trang.locator('#customerForm');
    await form.waitFor({ state: 'attached' });

    // Url.Page trả null thì rơi về "?handler=Save" — gửi vào chính trang này, nơi không có handler đó.
    const dich = await form.getAttribute('data-action');
    expect(dich, 'form phải gửi tới trang có handler Save').toContain('/khach-hang?handler=Save');

    await trang.locator('button:has-text("Sửa")').first().click();
    await expect(trang.locator('#ocEditCustomer')).toHaveClass(/show/);

    const tenCu = await trang.inputValue('#ocEditCustomer input[name="Input.FullName"]');
    const tenMoi = `${tenCu} [e2e ${Date.now() % 100000}]`;

    // Điền ĐỦ các trường bắt buộc, đúng như người dùng mô tả khi báo lỗi.
    await trang.fill('#ocEditCustomer input[name="Input.FullName"]', tenMoi);
    const oDienThoai = trang.locator('#ocEditCustomer input[name="Input.Phone"]');
    if (!(await oDienThoai.inputValue())) { await oDienThoai.fill('0900000000'); }

    await trang.locator('#ocEditCustomer button[type="submit"]').last().click();

    // Hỏng thì hiện popup "Không lưu được" — bắt nó để báo đúng nguyên nhân thay vì chờ hết giờ.
    const popup = trang.locator('.swal2-popup');
    await Promise.race([
      popup.waitFor({ timeout: 25_000 }).catch(() => {}),
      expect(trang.locator('#ocEditCustomer')).not.toHaveClass(/show/, { timeout: 25_000 }).catch(() => {}),
    ]);
    if (await popup.count()) {
      throw new Error('Không lưu được: ' + (await popup.innerText()).replace(/\n/g, ' | '));
    }

    expect(goiLuu.length, 'không có lời gọi lưu nào').toBeGreaterThan(0);
    expect(goiLuu.at(-1)).toMatchObject({ ma: 200, kieu: 'application/json' });

    // Bằng chứng thật: tải lại trang và đọc tên.
    await trang.goto(`/khach-hang/${id}`);
    await expect(trang.locator('body')).toContainText(tenMoi, { timeout: 20_000 });

    // Trả lại tên cũ để không làm bẩn dữ liệu cho lần chạy sau.
    await trang.locator('button:has-text("Sửa")').first().click();
    await expect(trang.locator('#ocEditCustomer')).toHaveClass(/show/);
    await trang.fill('#ocEditCustomer input[name="Input.FullName"]', tenCu);
    await trang.locator('#ocEditCustomer button[type="submit"]').last().click();
    await expect(trang.locator('#ocEditCustomer')).not.toHaveClass(/show/, { timeout: 25_000 });
  });

  test('Bỏ trống trường bắt buộc thì KHÔNG gửi đi', async ({ trang }) => {
    const id = await idKhachHangBatKy(trang);
    await trang.goto(`/khach-hang/${id}`);

    let daGui = false;
    trang.on('request', (r) => { if (r.url().includes('handler=Save')) { daGui = true; } });

    await trang.locator('button:has-text("Sửa")').first().click();
    await expect(trang.locator('#ocEditCustomer')).toHaveClass(/show/);

    await trang.fill('#ocEditCustomer input[name="Input.FullName"]', '');
    await trang.locator('#ocEditCustomer button[type="submit"]').last().click();
    await trang.waitForTimeout(1500);

    expect(daGui, 'thiếu trường bắt buộc mà vẫn gửi lên máy chủ').toBe(false);
    await expect(trang.locator('#ocEditCustomer')).toHaveClass(/show/);
  });
});
