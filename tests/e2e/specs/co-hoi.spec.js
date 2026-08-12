import { test, expect } from '../fixtures.js';

/**
 * Màn Cơ hội bán hàng — phễu chốt đơn, bám /booking-ticket của hệ cũ.
 *
 * Ba thứ canh ở đây đều là chỗ đã từng sai ở hệ cũ hoặc dễ sai khi dựng lại:
 *  - GIÁ TRỊ PHỄU: không có tiền thì màn này không trả lời được câu người bán hỏi mỗi sáng;
 *  - HUỶ phải kèm lý do — nguồn của báo cáo thống kê lý do mất khách;
 *  - CHỐT ĐƠN không đặt tay được, vì hệ cũ đánh dấu nó từ trong luồng tạo đơn.
 */

async function moThemMoi(page) {
  await page.evaluate(() => window.oc.open(null));
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

test.describe('Cơ hội bán hàng', () => {
  test('Cột phễu lấy từ danh mục, có đủ Huỷ và Chốt đơn', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    const ten = await page.locator('#f-stageCode option').allInnerTexts();
    // Cột do người dùng cấu hình nên KHÔNG ghi cứng danh sách ở đây; chỉ chốt hai cột hệ thống mà
    // luật bám vào — thiếu chúng là hỏng cả luật huỷ lẫn luật chốt đơn.
    expect(ten, 'thiếu cột Huỷ').toContain('Huỷ');
    expect(ten, 'thiếu cột Chốt đơn').toContain('Chốt đơn');
  });

  test('Giá trị dự kiến cộng đủ bốn bậc khách ngay khi gõ', async ({ trang: page }) => {
    await page.goto('/co-hoi');
    await moThemMoi(page);

    // Ô tiền giữ số thô ở ô ẩn nên phải gõ vào ô NHÌN THẤY (data-tien), đúng cách người dùng làm.
    await page.locator('#frm [name="Input.AdultQty"]').fill('2');
    await page.locator('#frm [data-tien$=".PriceAdult"]').fill('1000000');
    await page.locator('#frm [name="Input.ChildSmallQty"]').fill('3');
    await page.locator('#frm [data-tien$=".PriceChildSmall"]').fill('500000');

    // 2×1.000.000 + 3×500.000 = 3.500.000. Bậc "trẻ nhỏ" là bậc hệ cũ KHÔNG có ở phiếu — bỏ nó thì
    // con số này thiếu 1.500.000 mà không có gì báo.
    await expect(page.locator('#tong-gia-tri')).toHaveText('3.500.000');
  });

  test('Thẻ thống kê có giá trị đang mở, tách khỏi tổng', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    const dangMo = page.locator('[data-stat="Giá trị đang mở"]');
    await expect(dangMo).toBeVisible();

    const so = (t) => Number(String(t).replace(/\./g, '')) || 0;
    const mo = so(await dangMo.innerText());
    const tong = so(await page.locator('[data-stat="Tổng giá trị"]').innerText());

    // Tổng gộp cả cơ hội đã huỷ nên luôn >= phần đang mở. Bằng nhau vẫn hợp lệ (chưa huỷ cái nào),
    // nhưng nhỏ hơn thì chắc chắn sai công thức.
    expect(mo, 'giá trị đang mở lớn hơn tổng — sai công thức').toBeLessThanOrEqual(tong);
  });

  /**
   * Luật huỷ. Chặn ở SERVER chứ không chỉ ở hộp thoại: gọi thẳng handler mà không kèm lý do vẫn
   * phải bị từ chối, nếu không thì mọi đường ghi khác đều lách được.
   */
  test('Huỷ không kèm lý do thì server từ chối', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    const kq = await page.evaluate(async () => {
      const r = await fetch('/co-hoi?handler=Data&page=1&size=1');
      const j = await r.json();
      const row = (j.data || []).find((x) => !x.convertedOrderId);
      if (!row) { return null; }

      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const fd = new FormData();
      fd.append('id', row.id);
      fd.append('stageCode', '5');           // Huỷ
      const res = await fetch('/co-hoi?handler=Move', {
        method: 'POST', body: fd, headers: token ? { RequestVerificationToken: token } : {},
      });
      return await res.json();
    });

    test.skip(kq === null, 'Chưa có cơ hội nào để thử.');
    expect(kq.isSuccess, 'huỷ mà không cần lý do — báo cáo lý do mất khách sẽ rỗng').toBeFalsy();
    expect(String(kq.message)).toContain('lý do');
  });

  test('Không đặt tay sang Chốt đơn được', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    const kq = await page.evaluate(async () => {
      const r = await fetch('/co-hoi?handler=Data&page=1&size=1');
      const j = await r.json();
      const row = (j.data || []).find((x) => !x.convertedOrderId);
      if (!row) { return null; }

      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const fd = new FormData();
      fd.append('id', row.id);
      fd.append('stageCode', '6');           // Chốt đơn
      const res = await fetch('/co-hoi?handler=Move', {
        method: 'POST', body: fd, headers: token ? { RequestVerificationToken: token } : {},
      });
      return await res.json();
    });

    test.skip(kq === null, 'Chưa có cơ hội nào để thử.');
    // Cho kéo thẻ sang "Chốt đơn" là sinh ra cơ hội đã chốt mà không có đơn nào phía sau — doanh thu
    // dự kiến đẹp lên trong khi không có gì để thu.
    expect(kq.isSuccess, 'đặt tay chốt đơn được — sẽ có cơ hội chốt mà không có đơn').toBeFalsy();
  });

  test('Hộp huỷ chặn ngay ở giao diện khi chưa chọn lý do', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    const dong = page.locator('.tabulator-row:not(.tabulator-calcs)');
    await dong.first().waitFor({ state: 'visible', timeout: 20_000 });
    await dong.first().locator('.tabulator-cell[tabulator-field="__act"]').click();

    const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Huỷ cơ hội/ }).first();
    test.skip(await muc.count() === 0, 'Dòng đầu không huỷ được (đã huỷ hoặc đã chốt).');
    await muc.click();

    const hop = page.locator('#huy-modal');
    await expect(hop).toBeVisible({ timeout: 10_000 });
    await page.locator('#huy-ok').click();

    // Vẫn mở, vì chưa chọn lý do — bấm xong mà hộp đóng nghĩa là đã ghi thiếu lý do.
    await expect(hop, 'bấm xác nhận khi chưa chọn lý do mà hộp vẫn đóng').toBeVisible();
  });
});
