import { test, expect } from '../fixtures.js';
import { LAN, moThemMoi, dienO, giaTri, choKetQuaLuu } from './_form.js';

/**
 * Xoá một danh mục rồi tạo lại đúng mã đó phải nhận câu lỗi NÓI ĐƯỢC, không phải lỗi hệ thống.
 *
 * Chỉ mục duy nhất trong CSDL không kèm điều kiện lọc IsDeleted (34 chỉ mục như vậy), nên bản ghi đã
 * xoá mềm vẫn chiếm chỗ mã. Hàm chặn trùng của service truy vấn qua bộ lọc toàn cục nên không nhìn
 * thấy chúng: nó bảo "mã chưa dùng", CSDL bảo "trùng", và người dùng nhận 500 "Đã có lỗi xảy ra."
 *
 * Bài này KHÔNG khẳng định việc tái sử dụng mã phải thành công — đó là quyết định nghiệp vụ chưa
 * chốt. Nó chỉ chốt một điều: dù cho phép hay không, người dùng phải hiểu chuyện gì vừa xảy ra.
 */
test('/loai-xe — xoá rồi tạo lại cùng mã thì báo lỗi rõ, không phải "Đã có lỗi xảy ra"', async ({ trang: page, loiTrang }) => {
  const rieng = String(Number(LAN) + 31);

  async function taoLoaiXe() {
    await moThemMoi(page, '/loai-xe');
    for (const o of [{ ten: 'Input.Code' }, { ten: 'Input.Name' }]) {
      expect(await dienO(page, o.ten, rieng), `không điền được ${o.ten}`).toBe('ok');
    }
    const ma = await giaTri(page, 'Input.Code');
    await page.locator('#frm button[type="submit"]').click();
    return { ma, loi: await choKetQuaLuu(page) };
  }

  // --- Lần 1: tạo mới, phải thành công ---
  const lan1 = await taoLoaiXe();
  expect(lan1.loi, 'tạo loại xe lần đầu đã thất bại').toBeNull();
  expect(lan1.ma).toBeTruthy();

  // --- Xoá (xoá MỀM: bản ghi vẫn nằm trong bảng và vẫn chiếm chỗ chỉ mục) ---
  await page.goto('/loai-xe');
  const dong = page.locator('#tbl tbody tr', { hasText: lan1.ma });
  await expect(dong).toHaveCount(1, { timeout: 20_000 });
  await dong.locator('.js-del').click();
  await page.locator('.swal2-confirm').click();
  await expect(page.locator('#tbl tbody tr', { hasText: lan1.ma })).toHaveCount(0, { timeout: 20_000 });

  // --- Lần 2: tạo lại ĐÚNG mã vừa xoá ---
  const lan2 = await taoLoaiXe();

  // Thành công cũng được (nếu sau này chỉ mục được lọc theo IsDeleted). Nhưng nếu thất bại thì câu
  // lỗi phải chỉ ra vấn đề là TRÙNG GIÁ TRỊ — "Đã có lỗi xảy ra." không cho người dùng đường nào.
  if (lan2.loi !== null) {
    expect(lan2.loi, 'trùng mã sau khi xoá vẫn trả lỗi hệ thống mù mờ')
      .not.toContain('Đã có lỗi xảy ra');
    expect(lan2.loi.toLowerCase()).toContain('đã tồn tại');
  }

  // 409 là câu trả lời ĐÚNG mà bài này cố ý gây ra, không phải trang bị lỗi. Gỡ đúng dòng đó khỏi
  // danh sách lỗi trang — gỡ hẹp ở một bài, không nới lỏng BO_QUA chung của fixture, vì nới chung là
  // vô hiệu hoá đúng thứ bộ e2e sinh ra để bắt.
  for (let i = loiTrang.length - 1; i >= 0; i--) {
    if (/status of 409/.test(loiTrang[i])) { loiTrang.splice(i, 1); }
  }
});
