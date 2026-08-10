import { test, expect } from '../fixtures.js';
import DANH_SACH from './o-bat-buoc.data.js';
import { LAN, moThemMoi, dienO, choKetQuaLuu } from './_form.js';

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
 * LƯU Ý: bài này TẠO DỮ LIỆU THẬT trong CSDL đang chạy. Mọi bản ghi đều mang tiền tố "e2e-" để tìm
 * và dọn được.
 */
for (const trang of DANH_SACH) {
  test(`${trang.route} — chỉ điền ô bắt buộc thì phải lưu được`, async ({ trang: page }) => {
    await moThemMoi(page, trang.route);

    const khongDienDuoc = [];
    for (const o of trang.fields) {
      const kq = await dienO(page, o.ten, LAN);
      if (kq !== 'ok') khongDienDuoc.push(`${o.nhan} (${o.ten}): ${kq}`);
    }

    // Không điền được ô bắt buộc nào thì không kết luận được gì — nói rõ ra thay vì báo xanh giả.
    test.skip(khongDienDuoc.length > 0, `Chưa điền được: ${khongDienDuoc.join('; ')}`);

    await page.locator('#frm button[type="submit"]').click();

    const loi = await choKetQuaLuu(page);
    expect(loi, `${trang.route}: chỉ điền ô bắt buộc mà không lưu được`).toBeNull();
  });
}
