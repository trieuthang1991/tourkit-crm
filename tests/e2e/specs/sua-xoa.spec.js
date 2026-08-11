import { test, expect } from '../fixtures.js';
import DANH_SACH from './o-bat-buoc.data.js';
import { LAN, moThemMoi, dienO, giaTri, oBiAn, choKetQuaLuu, locVaTimDong } from './_form.js';

/**
 * Đường SỬA và XOÁ — hai luồng mà toàn bộ bài kiểm thử trước đó không chạm tới.
 *
 * Mọi bài trước chỉ đi đường thêm mới. Sửa là luồng khác hẳn: form phải nạp lại đúng giá trị cũ,
 * Select2 phải giữ được giá trị nằm ngoài danh mục, ô mật khẩu phải ẩn đi và để trống nghĩa là giữ
 * nguyên. Hỏng ở đây thì hậu quả nặng hơn hẳn thêm mới: người dùng mở một bản ghi có sẵn ra sửa,
 * thấy vài ô trống, bấm Lưu — và ghi đè mất dữ liệu cũ mà không hề có cảnh báo nào.
 */

/**
 * Mở form ở chế độ SỬA bằng đúng thao tác người dùng làm. Trả về false nếu danh sách đang trống.
 *
 * Hệ thống có HAI họ lưới và chúng mở sửa theo hai cách khác hẳn nhau:
 *  · bảng dựng sẵn phía server — nút .js-edit ngay trên dòng;
 *  · lưới Tabulator (tk.grid) — menu trên cột hành động, mục "Sửa …".
 * Chỉ tìm .js-edit là bỏ sót đúng một nửa hệ thống, mà lại là nửa gồm các màn lớn nhất.
 */
async function moSuaDongDau(page, route) {
  await page.goto(route);

  // CHỜ, không đếm ngay: lưới dựng dòng bằng JavaScript sau khi gọi dữ liệu, nên đếm ngay sau goto
  // luôn ra 0 và bài test bỏ qua nhầm — báo "danh sách trống" trong khi dữ liệu đang trên đường về.
  const nutTrucTiep = page.locator('.js-edit').first();
  // :not(.tabulator-calcs) là bắt buộc: Tabulator dựng DÒNG TỔNG bằng cùng loại phần tử và đặt nó
  // TRƯỚC các dòng dữ liệu, nên .first() không loại trừ sẽ trúng dòng tổng — mở ra form "sửa" với
  // các con số cộng dồn và không có id.
  const oTacVu = page.locator('.tabulator-row:not(.tabulator-calcs) .tabulator-cell[tabulator-field="__act"]').first();

  try {
    await expect(nutTrucTiep.or(oTacVu)).toBeVisible({ timeout: 20_000 });
  } catch {
    return false;   // hết giờ chờ ⇒ danh sách trống thật
  }

  if (await nutTrucTiep.count()) {
    await nutTrucTiep.click();
  } else {
    await oTacVu.click();
    const muc = page.locator('.tabulator-menu-item').filter({ hasText: /^Sửa/ }).first();
    try {
      await muc.waitFor({ state: 'visible', timeout: 10_000 });
    } catch {
      // Menu có nhưng không có mục "Sửa" ⇒ màn này cố ý không cho sửa tại chỗ (ví dụ Chuyến đi:
      // tầng dịch vụ chưa có UpdateAsync nên mục đó đã bị gỡ). Không có gì để kiểm, không phải lỗi.
      await page.keyboard.press('Escape');
      return false;
    }
    await muc.click();
  }

  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
  return true;
}

test.describe('Sửa bản ghi có sẵn', () => {
  for (const trang of DANH_SACH) {
    test(`${trang.route} — mở sửa thì form phải nạp lại dữ liệu cũ`, async ({ trang: page }) => {
      const moDuoc = await moSuaDongDau(page, trang.route);
      test.skip(!moDuoc, 'Danh sách đang trống nên chưa có gì để sửa.');

      // Dấu hiệu chắc chắn nhất của chế độ sửa: ô Id ẩn đã có giá trị. Thiếu nó thì lần Lưu kế tiếp
      // TẠO MỚI một bản ghi thay vì cập nhật — nhân đôi dữ liệu mà không ai nhận ra ngay.
      const id = await giaTri(page, 'Id');
      expect(id, `${trang.route}: mở sửa mà ô Id trống — bấm Lưu sẽ tạo bản ghi mới`).toBeTruthy();

      // Và mọi ô BẮT BUỘC đang hỏi người dùng phải có sẵn giá trị. Bản ghi đã tồn tại thì theo định
      // nghĩa nó đã qua được các luật bắt buộc, nên ô nào trống ở đây nghĩa là form không nạp được
      // giá trị cũ.
      //
      // Bỏ qua ô đang bị giấu: có ô cố ý ẩn ở chế độ sửa — mật khẩu người dùng chẳng hạn, để trống
      // nghĩa là giữ nguyên. Đòi nó phải có giá trị là đòi sai.
      const trong = [];
      for (const o of trang.fields) {
        if (await oBiAn(page, o.ten)) continue;
        const v = await giaTri(page, o.ten);
        if (v !== null && v.trim() === '') trong.push(`${o.nhan} (${o.ten})`);
      }

      expect(trong, `${trang.route}: mở sửa nhưng các ô bắt buộc này trống — lưu lại là mất dữ liệu cũ`)
        .toEqual([]);
    });
  }

  /**
   * Mật khẩu chỉ bắt buộc khi TẠO MỚI. Luật có điều kiện được thêm ở lần vá trước, nhưng chưa bài
   * nào chứng minh nó không chặn nhầm đường sửa — mà chặn nhầm thì không ai sửa nổi người dùng nào.
   */
  test('/thanh-vien — sửa người dùng không bị đòi nhập lại mật khẩu', async ({ trang: page }) => {
    const moDuoc = await moSuaDongDau(page, '/thanh-vien');
    test.skip(!moDuoc, 'Chưa có người dùng nào để sửa.');

    await expect(page.locator('#row-pass')).toBeHidden();

    await page.locator('#frm button[type="submit"]').click();
    const loi = await choKetQuaLuu(page);
    expect(loi, 'sửa người dùng mà không lưu được').toBeNull();
  });
});

/**
 * Vòng đời đầy đủ trên MỘT danh mục: tạo → sửa → xoá.
 *
 * Cố ý chỉ làm trên bản ghi do chính bài này tạo ra. Sửa/xoá một dòng có sẵn là đụng vào dữ liệu
 * thật của người dùng — một bài kiểm thử không được phép làm thế để chứng minh điều gì.
 */
test('/loai-xe — tạo rồi sửa rồi xoá được chính bản ghi vừa tạo', async ({ trang: page }) => {
  // Hậu tố RIÊNG, không dùng chung LAN với bài "lưu tối thiểu".
  //
  // Bài kia cũng tạo một loại xe mang mã 'e2e-<LAN>', mà LAN tính một lần lúc nạp _form.js nên hai
  // bài dùng chung đúng một giá trị. Chạy cả bộ thì bài này tạo trùng mã, server trả 400, và triệu
  // chứng lộ ra ở chỗ chẳng liên quan: fixture bắt được lỗi JavaScript "Failed to load resource".
  // Chạy riêng một file thì không có bản ghi kia nên không bao giờ tái hiện.
  // Phải là CHUỖI SỐ: có ô mã là type="number" (loại xe chẳng hạn), và dienO tính giá trị cho ô số
  // bằng Number(l) — thêm chữ vào là ra NaN, trình duyệt từ chối, ô thành rỗng.
  const rieng = String(Number(LAN) + 7);

  // --- Tạo ---
  await moThemMoi(page, '/loai-xe');
  for (const o of DANH_SACH.find((x) => x.route === '/loai-xe').fields) {
    expect(await dienO(page, o.ten, rieng), `không điền được ${o.ten}`).toBe('ok');
  }

  // ĐỌC LẠI mã thật sự nằm trong ô, không tự dựng lại chuỗi: dienO còn cắt theo maxlength của ô,
  // nên đoán giá trị là mở đường cho một lỗi khác đúng kiểu vừa gặp.
  const ma = await giaTri(page, 'Input.Code');
  expect(ma, 'không đọc được mã vừa điền').toBeTruthy();

  await page.locator('#frm button[type="submit"]').click();
  expect(await choKetQuaLuu(page), 'tạo loại xe thất bại').toBeNull();

  // --- Tìm đúng dòng vừa tạo ---
  await page.goto('/loai-xe');
  const dong = await locVaTimDong(page, ma);
  await expect(dong, 'không tìm thấy bản ghi vừa tạo trong danh sách').toHaveCount(1, { timeout: 20_000 });

  // --- Sửa ---
  await dong.locator('.js-edit').click();
  await expect(page.locator('#oc')).toHaveClass(/show/, { timeout: 15_000 });
  expect(await giaTri(page, 'Id'), 'mở sửa mà Id trống').toBeTruthy();

  const tenMoi = 'e2e-da-sua-' + rieng;
  await page.locator('#frm [name="Input.Name"]').fill(tenMoi);
  await page.locator('#frm button[type="submit"]').click();
  expect(await choKetQuaLuu(page), 'sửa loại xe thất bại').toBeNull();

  await page.goto('/loai-xe');
  await expect(page.locator('#tbl tbody tr', { hasText: tenMoi }),
    'sửa xong nhưng danh sách vẫn hiện tên cũ').toHaveCount(1, { timeout: 20_000 });

  // --- Xoá --- (hộp xác nhận là SweetAlert, không phải confirm của trình duyệt)
  await page.locator('#tbl tbody tr', { hasText: tenMoi }).locator('.js-del').click();
  await page.locator('.swal2-confirm').click();

  await expect(page.locator('#tbl tbody tr', { hasText: tenMoi }),
    'xoá xong nhưng bản ghi vẫn còn trong danh sách').toHaveCount(0, { timeout: 20_000 });
});
