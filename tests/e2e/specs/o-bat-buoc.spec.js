import { test, expect } from '../fixtures.js';
import DANH_SACH from './o-bat-buoc.data.js';

/**
 * Luật của hệ thống: ô nào bắt buộc thì label phải có dấu sao đỏ, VÀ bỏ trống ô đó rồi bấm Lưu
 * thì phải bị chặn kèm thông báo.
 *
 * Dấu sao là chữ, không chặn được gì. Thứ chặn thật là luật `rules` truyền vào tk.form(). Hai thứ
 * đó nằm ở hai chỗ khác nhau trong cùng file nên rất dễ lệch: thêm ô mới, chép cái label có sao,
 * quên khai luật. Lúc đó người dùng thấy dấu sao, tưởng hệ thống sẽ nhắc, rồi lưu ra bản ghi rỗng.
 *
 * Bài này mở TỪNG form ở trạng thái thêm mới, bấm Lưu khi chưa nhập gì, và soát từng ô có sao.
 *
 * Chỉ soát những ô THẬT SỰ đang trống. Nhiều select (Trạng thái, Loại…) luôn có sẵn lựa chọn đầu
 * tiên nên không bao giờ trống được — đòi chúng báo lỗi là đòi một điều vô nghĩa.
 */

/** Mở form ở chế độ thêm mới. Mọi trang danh mục đều theo cùng một quy ước: nút gọi oc.open(null). */
async function moFormThemMoi(page, route) {
  await page.goto(route);

  const moDuoc = await page.evaluate(() => {
    if (typeof window.oc?.open !== 'function') return false;
    window.oc.open(null);
    return true;
  });
  expect(moDuoc, `${route}: không gọi được oc.open(null) — form thêm mới không mở được`).toBe(true);

  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

/** Giá trị hiện có của một ô, đọc qua DOM để đúng cả với select và textarea. */
async function giaTri(page, ten) {
  return page.evaluate((n) => {
    const el = document.querySelector(`#frm [name="${CSS.escape(n)}"]`);
    return el ? String(el.value ?? '') : null;
  }, ten);
}

for (const trang of DANH_SACH) {
  test(`${trang.route} — ô bắt buộc bỏ trống phải báo lỗi`, async ({ trang: page }) => {
    await moFormThemMoi(page, trang.route);

    // Ô nào đang trống thì mới có chuyện "bỏ trống". Ô có sẵn giá trị mặc định thì bỏ qua.
    const canSoat = [];
    for (const o of trang.fields) {
      const v = await giaTri(page, o.ten);
      expect(v, `${trang.route}: không tìm thấy ô "${o.ten}" (nhãn "${o.nhan}") trong form`).not.toBeNull();
      if (v.trim() === '') canSoat.push(o);
    }

    test.skip(canSoat.length === 0, 'Mọi ô bắt buộc của form này đều có sẵn giá trị mặc định.');

    await page.locator('#frm button[type="submit"]').click();

    // Bấm Lưu khi thiếu dữ liệu thì form phải ở nguyên đó. Đóng lại nghĩa là đã gửi đi.
    await expect(page.locator('#oc'), `${trang.route}: form đã đóng, tức là lưu được dù còn ô trống`)
      .toHaveClass(/show/);

    const khongBaoLoi = [];
    for (const o of canSoat) {
      const bi = await page.locator(`#frm [name="${o.ten}"]`).evaluate((el) => el.classList.contains('is-invalid'));
      if (!bi) khongBaoLoi.push(`${o.nhan} (${o.ten})`);
    }

    expect(khongBaoLoi, `${trang.route}: có dấu sao đỏ nhưng bỏ trống vẫn không báo lỗi`).toEqual([]);
  });
}
