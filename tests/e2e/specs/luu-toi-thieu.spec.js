import { test, expect } from '../fixtures.js';
import DANH_SACH from './o-bat-buoc.data.js';

/**
 * Điền ĐÚNG những ô bắt buộc, bỏ trống mọi ô còn lại, rồi lưu.
 *
 * Bài o-bat-buoc.spec.js chứng minh chiều ngược lại: thiếu ô bắt buộc thì bị chặn. Còn đây là câu
 * hỏi thật của người dùng — "tôi chỉ điền những chỗ có dấu sao, có lưu được không?". Nếu không, thì
 * dấu sao đang nói dối: có những ô không đánh dấu nhưng thực ra vẫn bắt buộc, và người dùng chỉ phát
 * hiện ra sau khi bấm Lưu và nhận một câu lỗi khó hiểu.
 *
 * Đây cũng là chỗ lộ ra lỗi phía server mà không bài nào khác chạm tới: cột NOT NULL quên đặt mặc
 * định, tham chiếu null, ép kiểu chuỗi rỗng… tất cả chỉ nổ khi có người lưu một bản ghi tối thiểu.
 *
 * LƯU Ý: bài này TẠO DỮ LIỆU THẬT trong CSDL đang chạy. Mọi bản ghi đều mang tiền tố "e2e-" để
 * tìm và dọn được.
 */

/** Hậu tố duy nhất cho mỗi lần chạy — tránh đụng ràng buộc "mã đã tồn tại" ở lần chạy sau. */
const LAN = String(Date.now()).slice(-6);

async function moFormThemMoi(page, route) {
  await page.goto(route);
  const moDuoc = await page.evaluate(() => {
    if (typeof window.oc?.open !== 'function') return false;
    window.oc.open(null);
    return true;
  });
  expect(moDuoc, `${route}: không mở được form thêm mới`).toBe(true);
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

/**
 * Điền một ô theo đúng loại của nó.
 *
 * Phải đi qua API của thư viện chứ không gán thẳng .value: flatpickr giữ ngày trong instance riêng,
 * Quill giữ nội dung trong editor rồi mới đồng bộ ngược về textarea, Select2 chỉ cập nhật giao diện
 * khi nhận sự kiện 'change' của jQuery. Gán thẳng thì ô trông như đã điền mà form gửi đi vẫn rỗng.
 */
async function dienO(page, ten, lan) {
  return page.evaluate(({ n, l }) => {
    const el = document.querySelector(`#frm [name="${CSS.escape(n)}"]`);
    if (!el) return 'khong-thay-o';

    if (el.tagName === 'SELECT') {
      const op = [...el.options].find((o) => o.value !== '' && !o.disabled);
      if (!op) return 'khong-co-lua-chon';
      const $ = window.jQuery;
      if ($ && ($(el).hasClass('tk-s2') || $(el).hasClass('tk-s2-tags'))) {
        $(el).val($(el).prop('multiple') ? [op.value] : op.value).trigger('change');
      } else {
        el.value = op.value;
        el.dispatchEvent(new Event('change', { bubbles: true }));
      }
      return 'ok';
    }

    if (el._flatpickr) { el._flatpickr.setDate(new Date(), true); return 'ok'; }
    if (el._quill) { el._quill.setText('Nội dung thử tự động ' + l); return 'ok'; }

    // Giá trị hợp lệ cho phần lớn ràng buộc: chữ thường + gạch nối (đúng cả mẫu slug), mang hậu tố
    // theo lần chạy để không đụng ràng buộc "đã tồn tại".
    //
    // Ô số KHÔNG được điền 1: các danh mục đều có sẵn mã 1, 2, 3… trong dữ liệu mẫu, và bài test sẽ
    // đỏ vì "mã đã tồn tại" — một câu trả lời ĐÚNG của hệ thống, nhưng chẳng liên quan gì tới điều
    // đang cần chứng minh.
    let v = 'e2e-' + l;
    if (el.type === 'number') {
      // Tôn trọng min/max của chính ô đó. Nhồi 90000 vào ô "% hoa hồng" (0–100) thì bị luật nghiệp
      // vụ chặn — một câu trả lời đúng của hệ thống, nhưng không nói gì về điều đang cần chứng minh.
      const min = el.min === '' ? null : Number(el.min);
      const max = el.max === '' ? null : Number(el.max);
      let so = 90000 + (Number(l) % 9000);
      if (max !== null && so > max) so = max;
      if (min !== null && so < min) so = min;
      if (max !== null && min !== null && max > min) so = Math.min(max, Math.max(min + 1, so));
      v = String(so);
    }
    else if (el.type === 'email' || /email/i.test(n)) v = `e2e${l}@vidu.vn`;
    else if (/phone|dienthoai|sodt/i.test(n)) v = '09' + l.padEnd(8, '0');
    else if (el.type === 'password') v = `E2e@${l}xyz`;

    // Tôn trọng maxlength: mã tiền tệ chỉ 3 ký tự, nhồi dài hơn là bị luật nghiệp vụ chặn.
    const gh = el.maxLength;
    if (gh && gh > 0 && v.length > gh) v = v.slice(-gh);

    el.value = v;
    el.dispatchEvent(new Event('input', { bubbles: true }));
    el.dispatchEvent(new Event('change', { bubbles: true }));
    return 'ok';
  }, { n: ten, l: lan });
}

/**
 * Chờ tới khi form đóng (thành công) hoặc hộp lỗi hiện ra.
 *
 * Một số màn gọi location.reload() sau khi lưu, làm ngữ cảnh JS bị huỷ giữa chừng. Trang tải lại
 * thì cũng có nghĩa là đã lưu xong, nên bắt lỗi đó và coi là thành công thay vì để bài test đỏ oan.
 */
async function choKetQuaLuu(page) {
  try {
    await page.waitForFunction(() => {
      const oc = document.querySelector('#oc');
      const daDong = !oc || !oc.classList.contains('show');
      const loi = document.querySelector('.swal2-icon-error');
      return daDong || !!(loi && loi.offsetParent !== null);
    }, null, { timeout: 25_000 });
  } catch (e) {
    if (!/context was destroyed|Execution context/i.test(String(e))) throw e;
    await page.waitForLoadState('domcontentloaded');
    return null;   // trang đã tải lại ⇒ đã lưu
  }

  const hopLoi = page.locator('.swal2-popup:has(.swal2-icon-error)');
  if (await hopLoi.count() && await hopLoi.first().isVisible()) {
    return (await hopLoi.first().innerText()).replace(/\s+/g, ' ').trim();
  }
  return null;
}

for (const trang of DANH_SACH) {
  test(`${trang.route} — chỉ điền ô bắt buộc thì phải lưu được`, async ({ trang: page }) => {
    await moFormThemMoi(page, trang.route);

    const khongDienDuoc = [];
    for (const o of trang.fields) {
      const kq = await dienO(page, o.ten, LAN);
      if (kq !== 'ok') khongDienDuoc.push(`${o.nhan} (${o.ten}): ${kq}`);
    }

    // Ô bắt buộc là danh sách chọn mà rỗng ⇒ chưa có dữ liệu nền để tạo bản ghi này. Không phải lỗi
    // của form, nhưng cũng không kết luận được gì — nói rõ ra thay vì báo xanh giả.
    test.skip(khongDienDuoc.length > 0, `Chưa điền được: ${khongDienDuoc.join('; ')}`);

    await page.locator('#frm button[type="submit"]').click();

    const loi = await choKetQuaLuu(page);
    expect(loi, `${trang.route}: chỉ điền ô bắt buộc mà không lưu được`).toBeNull();
  });
}
