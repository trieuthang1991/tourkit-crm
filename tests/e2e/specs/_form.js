import { expect } from '../fixtures.js';

/**
 * Thao tác chung với form offcanvas (#oc/#frm) — dùng cho cả bài "lưu tối thiểu" lẫn bài "sửa/xoá".
 *
 * Tên file bắt đầu bằng gạch dưới nên không khớp mẫu *.spec.js, Playwright không nhặt làm bài test.
 */

/** Hậu tố duy nhất cho mỗi lần chạy — tránh đụng ràng buộc "mã đã tồn tại" ở lần chạy sau. */
export const LAN = String(Date.now()).slice(-6);

/** Mở form ở chế độ THÊM MỚI. Mọi màn danh mục đều theo cùng quy ước: nút gọi oc.open(null). */
export async function moThemMoi(page, route) {
  await page.goto(route);
  const moDuoc = await page.evaluate(() => {
    if (typeof window.oc?.open !== 'function') return false;
    window.oc.open(null);
    return true;
  });
  expect(moDuoc, `${route}: không mở được form thêm mới`).toBe(true);
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
}

/** Giá trị hiện tại của một ô, đọc qua DOM để đúng cả với select và textarea. */
export function giaTri(page, ten) {
  return page.evaluate((n) => {
    const el = document.querySelector(`#frm [name="${CSS.escape(n)}"]`);
    return el ? String(el.value ?? '') : null;
  }, ten);
}

/**
 * Ô có đang bị GIẤU khỏi người dùng không (nằm trong khối display:none).
 *
 * Cần vì có ô cố ý ẩn ở một số chế độ: mật khẩu biến mất khi sửa người dùng, để trống nghĩa là giữ
 * nguyên mật khẩu cũ. Đòi một ô như thế phải có giá trị là đòi sai.
 *
 * Duyệt display:none chứ không dùng offsetParent: Select2 giấu thẻ select gốc bằng clip/position chứ
 * KHÔNG bằng display, cố ý để phần kiểm tra biểu mẫu vẫn nhìn thấy nó — dùng offsetParent sẽ coi mọi
 * ô Select2 là đã ẩn và bỏ sót hết.
 */
export function oBiAn(page, ten) {
  return page.evaluate((n) => {
    const el = document.querySelector(`#frm [name="${CSS.escape(n)}"]`);
    if (!el) return true;
    for (let x = el; x && x !== document.body; x = x.parentElement) {
      if (getComputedStyle(x).display === 'none') return true;
    }
    return false;
  }, ten);
}

/**
 * Điền một ô theo đúng loại của nó.
 *
 * Phải đi qua API của thư viện chứ không gán thẳng .value: flatpickr giữ ngày trong instance riêng,
 * Quill giữ nội dung trong editor rồi mới đồng bộ ngược ra textarea, Select2 chỉ cập nhật giao diện
 * khi nhận sự kiện 'change' của jQuery. Gán thẳng thì ô trông như đã điền mà form gửi đi vẫn rỗng.
 */
export function dienO(page, ten, lan = LAN) {
  return page.evaluate(async ({ n, l }) => {
    const el = document.querySelector(`#frm [name="${CSS.escape(n)}"]`);
    if (!el) return 'khong-thay-o';

    if (el.tagName === 'SELECT') {
      // Nhận diện Select2 bằng chính INSTANCE, không bằng class. Nhiều màn khởi tạo select2 bằng JS
      // riêng của trang nên ô không mang class tk-s2 nào — bám vào class là bỏ sót đúng những ô nạp
      // qua AJAX, tức là bỏ sót đúng phần khó.
      const $ = window.jQuery;
      const s2 = $ ? $(el).data('select2') : null;

      const op = [...el.options].find((o) => o.value !== '' && !o.disabled);
      if (op) {
        if (s2) $(el).val($(el).prop('multiple') ? [op.value] : op.value).trigger('change');
        else { el.value = op.value; el.dispatchEvent(new Event('change', { bubbles: true })); }
        return 'ok';
      }

      // Select2 nạp qua AJAX: chưa gõ tìm thì chưa có <option> nào. Đọc URL từ chính cấu hình
      // Select2 lúc chạy rồi lấy mục đầu tiên — đúng cho mọi màn, không ghi cứng từng đầu mối, và
      // không phải đoán từ khoá nào thì ra kết quả.
      if (s2) {
        const ajax = s2?.options?.options?.ajax;
        if (!ajax?.url) return 'select2-khong-ro-nguon';

        const url = ajax.url + (ajax.url.includes('?') ? '&' : '?') + 'q=';
        let j;
        try {
          const r = await fetch(url);
          if (!r.ok) return 'select2-goi-nguon-loi-' + r.status;
          j = await r.json();
        } catch (e) { return 'select2-goi-nguon-hong'; }

        const dau = (j?.results || [])[0];
        if (!dau) return 'select2-nguon-rong';

        el.add(new Option(dau.text, dau.id, true, true));
        $(el).trigger('change');
        return 'ok';
      }

      return 'khong-co-lua-chon';
    }

    if (el._flatpickr) { el._flatpickr.setDate(new Date(), true); return 'ok'; }
    if (el._quill) { el._quill.setText('Nội dung thử tự động ' + l); return 'ok'; }

    // Giá trị hợp lệ cho phần lớn ràng buộc: chữ thường + gạch nối (đúng cả mẫu slug), mang hậu tố
    // theo lần chạy để không đụng ràng buộc "đã tồn tại".
    let v = 'e2e-' + l;
    if (el.type === 'number') {
      // Tôn trọng min/max của chính ô đó. Nhồi 90000 vào ô "% hoa hồng" (0–100) thì bị luật nghiệp
      // vụ chặn — một câu trả lời đúng của hệ thống, nhưng không nói gì về điều đang cần chứng minh.
      const min = el.min === '' ? null : Number(el.min);
      const max = el.max === '' ? null : Number(el.max);
      // Chỉ giữ chữ số của hậu tố: người gọi có thể truyền hậu tố lẫn chữ để tách bạch dữ liệu của
      // mình, mà Number('123vd') là NaN — gán "NaN" vào ô số thì trình duyệt bỏ qua và ô thành RỖNG,
      // rồi bài test đỏ ở một chỗ chẳng liên quan.
      const chiSo = Number(String(l).replace(/\D/g, '')) || 0;
      let so = 90000 + (chiSo % 9000);
      if (max !== null && so > max) so = max;
      if (min !== null && so < min) so = min;
      if (max !== null && min !== null && max > min) so = Math.min(max, Math.max(min + 1, so));
      v = String(so);
    } else if (el.type === 'email' || /email/i.test(n)) v = `e2e${l}@vidu.vn`;
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
 * Chờ tới khi form đóng (thành công) hoặc hộp lỗi hiện ra. Trả về câu lỗi, hoặc null nếu đã lưu.
 *
 * Một số màn gọi location.reload() sau khi lưu, làm ngữ cảnh JS bị huỷ giữa chừng. Trang tải lại
 * thì cũng có nghĩa là đã lưu xong, nên bắt lỗi đó và coi là thành công thay vì để bài test đỏ oan.
 */
export async function choKetQuaLuu(page) {
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
    return null;
  }

  const hopLoi = page.locator('.swal2-popup:has(.swal2-icon-error)');
  if (await hopLoi.count() && await hopLoi.first().isVisible()) {
    return (await hopLoi.first().innerText()).replace(/\s+/g, ' ').trim();
  }
  return null;
}
