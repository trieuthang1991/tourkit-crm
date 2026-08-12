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
      // Quét rộng rồi mới lọc: lấy đúng một dòng rồi lọc là phụ thuộc thứ tự lưới — chỉ cần một cơ
      // hội đã chốt trôi lên đầu là bài âm thầm bị bỏ qua.
      const r = await fetch('/co-hoi?handler=Data&page=1&size=50');
      const j = await r.json();
      const row = (j.data || []).find((x) => !x.convertedOrderId && x.stageCode !== 5);
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
      // Quét rộng rồi mới lọc: lấy đúng một dòng rồi lọc là phụ thuộc thứ tự lưới — chỉ cần một cơ
      // hội đã chốt trôi lên đầu là bài âm thầm bị bỏ qua.
      const r = await fetch('/co-hoi?handler=Data&page=1&size=50');
      const j = await r.json();
      const row = (j.data || []).find((x) => !x.convertedOrderId && x.stageCode !== 5);
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

  /**
   * Chốt thành đơn — bước MỘT CHIỀU.
   *
   * Kiểm cả hai vế: thiếu chuyến/khách thì phải nói RÕ thiếu gì (chứ không im lặng hoặc báo chung
   * chung), và chốt xong thì cơ hội bị khoá — vì số liệu của nó đã đi vào đơn hàng.
   */
  test('Chốt đơn khi thiếu chuyến thì nói rõ thiếu gì', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    const kq = await page.evaluate(async () => {
      const r = await fetch('/co-hoi?handler=Data&page=1&size=50');
      const j = await r.json();
      const row = (j.data || []).find((x) => !x.convertedOrderId && !x.tourDepartureId);
      if (!row) { return null; }

      const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const fd = new FormData();
      fd.append('id', row.id);
      const res = await fetch('/co-hoi?handler=ChotDon', {
        method: 'POST', body: fd, headers: token ? { RequestVerificationToken: token } : {},
      });
      return await res.json();
    });

    test.skip(kq === null, 'Không có cơ hội nào thiếu chuyến để thử.');
    expect(kq.isSuccess).toBeFalsy();
    expect(String(kq.message), 'báo lỗi không nói thiếu chuyến').toContain('chuyến');
  });

  test('Chốt đơn thành công thì cơ hội bị khoá và sang cột Chốt đơn', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    // TỰ DỰNG cơ hội để thử, không mượn dữ liệu mẫu: chốt đơn là bước MỘT CHIỀU, mượn thì bài này
    // chỉ chạy đúng một lần rồi những lần sau âm thầm bị bỏ qua — tệ hơn là đỏ, vì không ai để ý.
    const kq = await page.evaluate(async () => {
      const token = () => document.querySelector('input[name="__RequestVerificationToken"]')?.value;
      const post = async (url, fd) => {
        const t = token();
        const res = await fetch(url, { method: 'POST', body: fd, headers: t ? { RequestVerificationToken: t } : {} });
        return await res.json();
      };

      const chuyen = (await (await fetch('/co-hoi?handler=DepartureLookup&q=')).json()).results?.[0];
      const khach = (await (await fetch('/khach-hang?handler=Data&draw=1&start=0&length=1')).json()).data?.[0];
      if (!chuyen || !khach) { return null; }

      const ma = 'CH-E2E-' + Date.now();
      const f = new FormData();
      f.append('Input.Code', ma);
      f.append('Input.Title', 'Cơ hội do bài kiểm thử tạo');
      f.append('Input.ContactName', 'Khách kiểm thử');
      f.append('Input.AdultQty', '1');
      f.append('Input.PriceAdult', '1000000');
      f.append('Input.TourDepartureId', chuyen.id);
      f.append('Input.CustomerId', khach.id);
      const tao = await post('/co-hoi?handler=Save', f);
      if (!tao.isSuccess) { return { loiTao: tao.message }; }

      const lay = async () => (await (await fetch('/co-hoi?handler=Data&page=1&size=50&q=' + ma)).json()).data || [];
      const row = (await lay()).find((x) => x.code === ma);
      if (!row) { return { loiTao: 'tạo xong không tìm thấy' }; }

      const goi = async (handler, extra) => {
        const fd = new FormData();
        fd.append('id', row.id);
        Object.entries(extra || {}).forEach(([k, v]) => fd.append(k, v));
        return await post('/co-hoi?handler=' + handler, fd);
      };

      const chot = await goi('ChotDon');
      const sau = (await lay()).find((x) => x.id === row.id);
      // Chốt rồi thì mọi đường ghi khác phải đóng lại.
      const chotLai = await goi('ChotDon');
      const chuyenCot = await goi('Move', { stageCode: '3' });
      const xoa = await goi('Delete');

      return { chot, stageCode: sau?.stageCode, coDon: !!sau?.convertedOrderId, chotLai, chuyenCot, xoa };
    });

    test.skip(kq === null, 'Chưa có chuyến hoặc khách hàng nào để dựng cơ hội thử.');
    expect(kq.loiTao, `không tạo được cơ hội để thử: ${kq.loiTao}`).toBeUndefined();

    expect(kq.chot.isSuccess, `chốt đơn thất bại: ${kq.chot.message}`).toBeTruthy();
    expect(kq.coDon, 'chốt xong mà cơ hội không gắn đơn nào').toBe(true);
    expect(kq.stageCode, 'chốt xong mà không sang cột Chốt đơn').toBe(6);

    expect(kq.chotLai.isSuccess, 'chốt lại lần hai vẫn được — sẽ đẻ ra hai đơn cho một cơ hội').toBeFalsy();
    expect(kq.chuyenCot.isSuccess, 'cơ hội đã chốt mà vẫn kéo sang cột khác được').toBeFalsy();
    expect(kq.xoa.isSuccess, 'cơ hội đã chốt mà vẫn xoá được — đơn hàng sẽ trỏ vào hư không').toBeFalsy();
  });

  test('Hộp huỷ chặn ngay ở giao diện khi chưa chọn lý do', async ({ trang: page }) => {
    await page.goto('/co-hoi');

    // Lọc xuống ĐÚNG một cơ hội còn huỷ được. Lấy dòng đầu thì bài này phụ thuộc thứ tự lưới: chỉ
    // cần một cơ hội đã chốt trôi lên đầu là bài âm thầm bị bỏ qua.
    const ma = await page.evaluate(async () => {
      const r = await fetch('/co-hoi?handler=Data&page=1&size=50');
      const j = await r.json();
      return (j.data || []).find((x) => !x.convertedOrderId && x.stageCode !== 5)?.code ?? null;
    });
    test.skip(!ma, 'Không có cơ hội nào còn huỷ được.');

    await page.locator('#f-q').fill(ma);
    await page.waitForTimeout(1200);

    const dong = page.locator('.tabulator-row:not(.tabulator-calcs)');
    await dong.first().waitFor({ state: 'visible', timeout: 20_000 });
    await dong.first().locator('.tabulator-cell[tabulator-field="__act"]').click();

    const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Huỷ cơ hội/ }).first();
    await muc.waitFor({ state: 'visible', timeout: 10_000 });
    await muc.click();

    const hop = page.locator('#huy-modal');
    await expect(hop).toBeVisible({ timeout: 10_000 });
    await page.locator('#huy-ok').click();

    // Vẫn mở, vì chưa chọn lý do — bấm xong mà hộp đóng nghĩa là đã ghi thiếu lý do.
    await expect(hop, 'bấm xác nhận khi chưa chọn lý do mà hộp vẫn đóng').toBeVisible();
  });
});
