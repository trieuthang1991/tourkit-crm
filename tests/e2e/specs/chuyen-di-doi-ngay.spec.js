import { test, expect } from '../fixtures.js';

/**
 * Đổi NGÀY KHỞI HÀNH của chuyến ĐÃ CÓ KHÁCH: cảnh báo, nhưng vẫn cho sửa.
 *
 * Luật do chủ dự án chốt (2026-08-12). Dời ngày là chuyện bình thường trong nghề nên KHÔNG chặn —
 * chặn cứng thì người điều hành lách bằng cách mở chuyến mới rồi chuyển khách sang, vừa mất lịch sử
 * vừa dễ sai hơn. Nhưng lặng lẽ cho đổi cũng sai: khách đã đặt lệch lịch, mà phân công HDV/xe giữ
 * ngày riêng nên không tự dời theo.
 *
 * Bài này canh CẢ BA vế, vì hỏng vế nào cũng không có gì báo:
 *  1. có khách + đổi ngày  → phải hỏi lại;
 *  2. bấm Huỷ              → KHÔNG được lưu (nếu vẫn lưu thì cảnh báo chỉ là trang trí);
 *  3. không đụng tới ngày  → KHÔNG được hỏi (hỏi thừa thì người dùng quen tay bấm bừa, tới lúc
 *     cảnh báo thật cũng bấm bừa nốt).
 */

/** Lấy một chuyến ĐÃ CÓ chỗ giữ/bán và có sẵn ngày khởi hành. */
async function chuyenCoKhach(page) {
  return page.evaluate(async () => {
    const r = await fetch('?handler=Data&draw=1&start=0&length=100');
    const j = await r.json();
    return (j.data || []).find((x) => x.departureDate && ((x.seatHeld || 0) + (x.seatSold || 0)) > 0) || null;
  });
}

/**
 * Lọc qua ô tìm kiếm TRƯỚC khi tìm dòng.
 *
 * Lưới phân trang từ server, mà chuyến có khách hiếm hơn chuyến trống nên rất dễ nằm ở trang 2 —
 * lúc đó dòng không có trong DOM và bài kiểm thử đỏ vì lý do chẳng liên quan gì tới luật đang canh.
 * Đúng cái bẫy đã gặp ở /loai-xe.
 */
async function moSua(page, ma) {
  await page.locator('#f-q').fill(ma);
  await page.locator('#btn-search').click();

  const dong = page.locator('.tabulator-row:not(.tabulator-calcs)', { hasText: ma }).first();
  await dong.waitFor({ state: 'visible', timeout: 20_000 });
  await dong.locator('.tabulator-cell[tabulator-field="__act"]').click();

  const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Sửa/ }).first();
  await muc.waitFor({ state: 'visible', timeout: 10_000 });
  await muc.click();
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

/** Gán thẳng vào ô gốc rồi báo cho flatpickr — .fill() chỉ chạm được ô hiển thị d/m/Y. */
async function datNgay(page, iso) {
  await page.evaluate((v) => {
    const el = document.querySelector('#frm [name="Input.DepartureDate"]');
    if (el._flatpickr) { el._flatpickr.setDate(v, true); } else { el.value = v; }
  }, iso);
}

test.describe('Đổi ngày khởi hành chuyến đã có khách', () => {
  test('Đổi ngày thì phải hỏi lại, và bấm Huỷ thì không lưu', async ({ trang: page }) => {
    await page.goto('/chuyen-di/loai/tat-ca');

    const c = await chuyenCoKhach(page);
    test.skip(!c, 'Chưa có chuyến nào vừa đặt ngày vừa có khách.');

    await moSua(page, c.code);

    // Dời sang ngày khác hẳn để chắc chắn không trùng ngày cũ.
    const moi = '2027-03-15' === c.departureDate ? '2027-03-16' : '2027-03-15';
    await datNgay(page, moi);
    await page.locator('#frm button[type="submit"]').click();

    const hop = page.locator('.swal2-popup');
    await expect(hop, 'đổi ngày chuyến đã có khách mà không hỏi lại gì').toBeVisible({ timeout: 10_000 });
    await expect(hop).toContainText('đã có khách');

    await page.locator('.swal2-cancel').click();
    await expect(hop).toBeHidden({ timeout: 10_000 });

    // Bấm Huỷ xong thì form vẫn mở và ngày trên SERVER phải y như cũ.
    await expect(page.locator('#oc'), 'bấm Huỷ mà form đóng luôn — người dùng mất chỗ đang sửa')
      .toHaveClass(/show/);

    // Hỏi thẳng server theo MÃ, đừng quét 100 dòng đầu: chuyến đang thử có thể nằm ở trang sau và
    // find() trả undefined — bài sẽ đỏ như thể dữ liệu bị đổi, trong khi thực ra chỉ là không thấy.
    const sau = await page.evaluate(async (ma) => {
      const r = await fetch('?handler=Data&draw=1&start=0&length=20&q=' + encodeURIComponent(ma));
      const j = await r.json();
      return (j.data || []).find((x) => x.code === ma)?.departureDate ?? null;
    }, c.code);
    expect(sau, 'bấm Huỷ ở hộp cảnh báo mà chuyến vẫn bị đổi ngày').toBe(c.departureDate);
  });

  test('Không đụng tới ngày thì lưu thẳng, không hỏi', async ({ trang: page }) => {
    await page.goto('/chuyen-di/loai/tat-ca');

    const c = await chuyenCoKhach(page);
    test.skip(!c, 'Chưa có chuyến nào vừa đặt ngày vừa có khách.');

    await moSua(page, c.code);

    // Chỉ sửa tên, giữ nguyên ngày.
    await page.locator('#frm [name="Input.Title"]').fill((c.title || 'Chuyến') + ' ');
    await page.locator('#frm button[type="submit"]').click();

    await expect(page.locator('#oc'), 'lưu xong mà form không đóng — có thể đang kẹt ở hộp hỏi thừa')
      .not.toHaveClass(/show/, { timeout: 15_000 });
    await expect(page.locator('.swal2-popup'),
      'không đổi ngày mà vẫn bị hỏi — cảnh báo thừa làm người dùng quen tay bấm bừa').toHaveCount(0);
  });
});
