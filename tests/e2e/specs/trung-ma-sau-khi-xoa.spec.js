import { test, expect } from '../fixtures.js';
import { LAN, moThemMoi, dienO, giaTri, choKetQuaLuu, locVaTimDong } from './_form.js';

/**
 * Xoá một danh mục rồi tạo lại đúng mã đó phải nhận câu lỗi NÓI ĐƯỢC, không phải lỗi hệ thống.
 *
 * Trước đây chỉ mục duy nhất KHÔNG lọc theo IsDeleted, nên bản ghi đã xoá mềm vẫn chiếm chỗ mã. Hàm
 * chặn trùng của service truy vấn qua bộ lọc toàn cục nên không nhìn thấy chúng: nó bảo "mã chưa
 * dùng", CSDL bảo "trùng", và người dùng nhận 500 "Đã có lỗi xảy ra."
 *
 * Luật do chủ dự án chốt: DANH MỤC được tái dùng mã sau khi xoá, ĐƠN HÀNG thì không. Migration
 * FilterCatalogUniqueIndexesByIsDeleted đã lọc 16 chỉ mục danh mục; IX_Orders_TenantId_Code cố ý
 * giữ nguyên không lọc.
 *
 * Bài này chốt vế danh mục. Vế đơn hàng chốt bằng chính việc chỉ mục đó không có bộ lọc.
 */
test('/loai-xe — xoá danh mục rồi tạo lại đúng mã đó phải được', async ({ trang: page }) => {
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
  const dong = await locVaTimDong(page, lan1.ma);
  await expect(dong).toHaveCount(1, { timeout: 20_000 });
  await dong.locator('.js-del').click();
  await page.locator('.swal2-confirm').click();
  // Xoá xong trang nạp lại nên ô tìm kiếm trống trở lại — lọc lại trước khi khẳng định đã mất, nếu
  // không thì bảng quá 20 dòng sẽ cho kết quả 0 vì bản ghi ở trang 2 chứ không phải vì đã xoá.
  await expect(await locVaTimDong(page, lan1.ma)).toHaveCount(0, { timeout: 20_000 });

  // --- Lần 2: tạo lại ĐÚNG mã vừa xoá ---
  const lan2 = await taoLoaiXe();

  // Loại xe là DANH MỤC, và luật do chủ dự án chốt là danh mục ĐƯỢC tái dùng mã sau khi xoá.
  // Chỉ mục duy nhất nay lọc theo IsDeleted (migration FilterCatalogUniqueIndexesByIsDeleted) nên
  // dòng đã xoá không còn giữ chỗ. Tạo lại phải THÀNH CÔNG.
  //
  // Đơn hàng thì ngược lại — mã không tái dùng — nên IX_Orders_TenantId_Code cố ý KHÔNG lọc.
  expect(lan2.loi, 'xoá danh mục rồi tạo lại đúng mã mà vẫn bị chặn').toBeNull();
});
