import { test, expect } from '../fixtures.js';

/**
 * Vòng đời đầy đủ trên các màn danh mục: TẠO → thấy trong lưới → XOÁ → biến mất.
 *
 * Chọn màn danh mục vì form của chúng đơn giản (một hai trường), nên bài kiểm thử soát được đúng
 * cái cần soát — đường đi tạo/xoá — thay vì sa vào việc điền hai chục ô.
 *
 * Mỗi bài tự dọn thứ nó tạo ra. Bài kiểm thử để lại rác thì lần chạy sau danh sách dài dần và cuối
 * cùng có người tắt nó đi.
 */
const DANH_MUC = [
  { ten: 'Nguồn khách', duong: '/nguon-khach' },
  { ten: 'Nhãn khách', duong: '/nhan-khach' },
  { ten: 'Chi nhánh', duong: '/chi-nhanh' },
  { ten: 'Phòng ban', duong: '/phong-ban' },
  { ten: 'Chức vụ', duong: '/chuc-vu' },
  { ten: 'Loại xe', duong: '/loai-xe' },
  { ten: 'Nhóm tour', duong: '/nhom-tour' },
  { ten: 'Lý do chuyển', duong: '/ly-do-chuyen' },
];

test.describe('Danh mục: tạo rồi xoá', () => {
  for (const man of DANH_MUC) {
    test(man.ten, async ({ trang }) => {
      const ten = `E2E ${Date.now() % 1000000}`;

      await trang.goto(man.duong, { waitUntil: 'domcontentloaded' });
      await trang.waitForTimeout(2000);

      const nutThem = trang.locator('button:has-text("Thêm"), a:has-text("Thêm")').first();
      test.skip(await nutThem.count() === 0, 'màn này không có nút thêm');
      await nutThem.click();

      const panel = trang.locator('.offcanvas.show, .modal.show').first();
      await expect(panel, `${man.ten}: bấm Thêm không mở form`).toBeVisible({ timeout: 10_000 });

      // Điền MỌI ô chữ đang trống. Danh mục chỉ có tên (đôi khi thêm mã/mô tả) nên cách này đủ,
      // và không phải ghi cứng tên trường của từng màn.
      const o = panel.locator('input[type="text"]:visible, input:not([type]):visible');
      const soO = await o.count();
      expect(soO, `${man.ten}: form không có ô nhập nào`).toBeGreaterThan(0);
      for (let i = 0; i < soO; i++) {
        if (!(await o.nth(i).inputValue())) { await o.nth(i).fill(ten); }
      }

      // Ô số phải điền số hợp lệ. Vài danh mục có ràng buộc nghiệp vụ "> 0" (Loại xe: số ghế),
      // để trống hoặc điền chữ là bị chặn đúng — và bài test đỏ vì lý do không liên quan.
      const oSo = panel.locator('input[type="number"]:visible');
      for (let i = 0; i < await oSo.count(); i++) {
        // Số phải DUY NHẤT, và không gian phải ĐỦ RỘNG.
        //
        // Bản cũ dùng 900 + (Date.now() % 90) — chỉ 90 giá trị. Mỗi lượt chạy tạo một bản ghi rồi
        // xoá, nhưng xoá là xoá MỀM và chỉ mục duy nhất trong CSDL không lọc theo IsDeleted, nên
        // dòng đã xoá vẫn giữ chỗ mã vĩnh viễn. Chạy đủ nhiều lần là cả 90 ô bị chiếm và bài test đỏ
        // mãi mãi với lý do "giá trị đã tồn tại" — chẳng liên quan gì tới điều nó muốn kiểm.
        const v = await oSo.nth(i).inputValue();
        if (!v || Number(v) <= 0) {
          const tran = Number(await oSo.nth(i).getAttribute('max')) || 0;
          let so = 1000 + (Date.now() % 900_000);
          if (tran > 0 && so > tran) { so = Math.max(1, tran - (Date.now() % tran)); }
          await oSo.nth(i).fill(String(so));
        }
      }

      await panel.locator('button[type="submit"], button:has-text("Lưu")').last().click();

      // Hỏng thì hiện popup lỗi — bắt nó để báo đúng nguyên nhân thay vì chờ hết giờ.
      const popup = trang.locator('.swal2-popup:has-text("lỗi"), .swal2-popup:has-text("Không")');
      await Promise.race([
        popup.waitFor({ timeout: 15_000 }).catch(() => {}),
        expect(panel).not.toBeVisible({ timeout: 15_000 }).catch(() => {}),
      ]);
      if (await popup.count()) {
        throw new Error(`${man.ten}: không tạo được — ${(await popup.innerText()).replace(/\n/g, ' | ')}`);
      }

      // Bằng chứng thật: tải lại trang rồi tìm. Lưới tự làm tươi không chứng minh đã ghi xuống CSDL.
      await trang.goto(man.duong, { waitUntil: 'domcontentloaded' });
      await trang.waitForTimeout(2000);

      // LỌC trước khi khẳng định. Bảng danh mục phân trang phía client (20 dòng/trang), nên khi danh
      // mục đã nhiều bản ghi thì dòng vừa tạo nằm ở trang sau và KHÔNG có trong DOM — bài test đỏ
      // với lý do "không thấy trong danh sách" trong khi bản ghi đã ghi xuống CSDL đàng hoàng.
      const oTim = trang.locator('#tbl_filter input, .dataTables_filter input, #f-q').first();
      if (await oTim.count()) {
        await oTim.fill(ten);
        await trang.waitForTimeout(800);
      }
      await expect(trang.locator('body'), `${man.ten}: tạo xong nhưng không thấy trong danh sách`)
        .toContainText(ten, { timeout: 15_000 });

      // --- Dọn: xoá bản ghi vừa tạo ---
      const dong = trang.locator(`.tabulator-row:has-text("${ten}"), table tbody tr:has-text("${ten}")`).first();
      await dong.hover();

      const nutXoa = dong.locator('button:has(.ti-trash), a:has(.ti-trash), [title*="Xoá" i], [title*="Xóa" i]').first();
      test.skip(await nutXoa.count() === 0, 'màn này không có nút xoá trên dòng');
      await nutXoa.click();

      // Xoá luôn phải hỏi lại — xoá thẳng không hỏi là mất dữ liệu chỉ vì trượt tay.
      const xacNhan = trang.locator('.swal2-confirm');
      await expect(xacNhan, `${man.ten}: xoá mà không hỏi lại`).toBeVisible({ timeout: 10_000 });
      await xacNhan.click();

      await trang.waitForTimeout(2500);
      await trang.goto(man.duong, { waitUntil: 'domcontentloaded' });
      await trang.waitForTimeout(2000);

      // Bằng chứng là TÊN biến mất, không phải số dòng giảm: dự án dùng ba kiểu lưới khác nhau
      // (Tabulator, DataTables, bảng HTML thường) nên đếm dòng không đáng tin, còn tên thì luôn đúng.
      await expect(trang.locator('body'), `${man.ten}: xoá xong vẫn còn trong danh sách`)
        .not.toContainText(ten);
    });
  }
});
