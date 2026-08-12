import { test, expect } from '../fixtures.js';

/**
 * Soát THAO TÁC THẬT trên từng màn danh sách, không chỉ "trang mở được".
 *
 * Trang mở được không chứng minh gì: lưới có thể rỗng vì truy vấn hỏng, ô lọc có thể gõ vào mà không
 * lọc, nút "Thêm" có thể bấm không ra gì. Ba thứ đó đều trông bình thường trên ảnh chụp màn hình.
 *
 * Dùng expect.soft để một màn báo ĐỦ mọi vấn đề trong một lần chạy, thay vì sửa một lỗi rồi mới thấy
 * lỗi tiếp theo.
 */
const MAN = [
  { ten: 'Khách hàng', duong: '/khach-hang', luoi: '#grid-customers' },
  { ten: 'Cơ hội bán hàng', duong: '/co-hoi', luoi: '#grid-opp' },
  { ten: 'Khách tiềm năng', duong: '/khach-tiem-nang', luoi: '#grid-leads' },
  { ten: 'Nhà cung cấp', duong: '/nha-cung-cap', luoi: '#grid-providers' },
  { ten: 'Đơn hàng', duong: '/don-hang' },
  { ten: 'Báo giá', duong: '/bao-gia' },
  { ten: 'Chuyến đi', duong: '/chuyen-di' },
  { ten: 'Quỹ vé', duong: '/quy-ve' },
  { ten: 'Công việc', duong: '/cong-viec' },
  { ten: 'Lịch hẹn', duong: '/lich-hen' },
  { ten: 'Phiếu thu', duong: '/phieu-thu' },
  { ten: 'Phiếu chi', duong: '/phieu-chi' },
  { ten: 'Bảng giá NCC', duong: '/bang-gia-ncc' },
];

/** Số dòng đang hiện, không phụ thuộc lưới dựng bằng Tabulator hay DataTables. */
async function demDong(trang) {
  return trang.evaluate(() => {
    const q = (s) => document.querySelectorAll(s).length;
    return q('.tabulator-row') + q('table tbody tr:not(.dataTables_empty)');
  });
}

test.describe('Soát màn danh sách', () => {
  for (const man of MAN) {
    test(man.ten, async ({ trang }) => {
      await trang.goto(man.duong, { waitUntil: 'domcontentloaded' });
      await trang.waitForTimeout(2500);   // chờ lưới nạp dữ liệu qua AJAX

      // 1. Có lưới, và lưới có dòng HOẶC nói rõ là chưa có dữ liệu.
      const dong = await demDong(trang);
      const chuTrang = await trang.locator('body').innerText();
      const noiRong = /chưa có dữ liệu|không tìm thấy|không có dữ liệu|chưa có/i.test(chuTrang);
      expect.soft(dong > 0 || noiRong, `${man.ten}: lưới trống mà không nói gì`).toBe(true);

      // 2. Không có chữ rác của lập trình lọt ra màn hình. "undefined"/"NaN"/"[object Object]" là dấu
      //    hiệu một cột đọc sai tên trường — dữ liệu vẫn hiện nên rất dễ lọt qua mắt.
      const rac = ['undefined', 'NaN', '[object Object]', 'null,'].filter((r) => chuTrang.includes(r));
      expect.soft(rac, `${man.ten}: có chữ rác trên màn hình`).toEqual([]);

      // 3. Trang KHÔNG được có thanh cuộn ngang. Bảng rộng phải tự cuộn trong khung của nó.
      const tranNgang = await trang.evaluate(() =>
        document.documentElement.scrollWidth > document.documentElement.clientWidth + 2);
      expect.soft(tranNgang, `${man.ten}: cả trang bị cuộn ngang`).toBe(false);

      // 4. Ô chọn không có lựa chọn nào là điều khiển vô dụng — bấm vào không chọn được gì.
      //    TRỪ select2 gọi server: rỗng lúc đầu là đúng thiết kế, gõ vào mới nạp. Không loại trừ
      //    nhóm này thì phép soát báo nhầm hàng loạt và nhanh chóng bị bỏ qua.
      const oChonRong = await trang.evaluate(() => {
        const $ = window.jQuery;
        return Array.from(document.querySelectorAll('select'))
          .filter((s) => {
            if (s.multiple || s.options.length > 1 || s.offsetParent === null) { return false; }
            const cfg = $ && $(s).data('select2') && $(s).data('select2').options;
            return !(cfg && cfg.options && cfg.options.ajax);
          })
          .map((s) => s.id || s.name || '(không tên)');
      });
      expect.soft(oChonRong, `${man.ten}: ô chọn không có lựa chọn nào`).toEqual([]);

      // 5. Nút hành động chính phải MỞ ĐƯỢC thứ gì đó. Nút bấm không ra gì là lỗi im lặng điển hình:
      //    người dùng bấm, không có gì xảy ra, và không ai biết báo lỗi kiểu nào.
      //    Hai kiểu đều hợp lệ: mở panel tại chỗ, hoặc chuyển sang trang soạn riêng (Báo giá, Hoá đơn).
      //    Chỉ tính là hỏng khi bấm xong KHÔNG có gì thay đổi.
      const nutThem = trang.locator('button:has-text("Thêm"), a:has-text("Thêm")').first();
      if (await nutThem.count() && await nutThem.isVisible()) {
        const urlTruoc = trang.url();
        await nutThem.click();
        await trang.waitForTimeout(1500);

        const moPanel = await trang.evaluate(() =>
          document.querySelectorAll('.offcanvas.show, .modal.show').length > 0);
        const doiTrang = trang.url() !== urlTruoc;

        expect.soft(moPanel || doiTrang, `${man.ten}: bấm "Thêm" không mở ra gì`).toBe(true);

        if (doiTrang) {
          // Chuyển sang một trang 404 thì cũng là "đổi trang" — phải kiểm tra trang đích sống thật.
          const ma = await trang.evaluate(async (u) => (await fetch(u)).status, trang.url());
          expect.soft(ma, `${man.ten}: "Thêm" dẫn tới trang lỗi ${ma}`).toBeLessThan(400);
          await trang.goto(man.duong, { waitUntil: 'domcontentloaded' });
        } else {
          await trang.keyboard.press('Escape');
          await trang.waitForTimeout(400);
        }
      }

      // 6. Nút bấm không có nhãn (chỉ icon, không title/aria-label) — không ai đoán được nó làm gì.
      const nutCam = await trang.evaluate(() => Array.from(document.querySelectorAll('button'))
        .filter((b) => b.offsetParent !== null
          && !b.textContent.trim()
          && !b.getAttribute('title')
          && !b.getAttribute('aria-label')
          && !b.classList.contains('btn-close')
          && !b.closest('.tabulator, .dataTables_wrapper, .swal2-container'))
        .map((b) => b.className.split(' ').slice(0, 2).join('.'))
        .slice(0, 5));
      expect.soft(nutCam, `${man.ten}: nút chỉ có icon, không nhãn không tooltip`).toEqual([]);
    });
  }
});

test.describe('Ô lọc từ khoá thật sự lọc', () => {
  for (const man of MAN.filter((m) => m.luoi)) {
    test(man.ten, async ({ trang }) => {
      await trang.goto(man.duong, { waitUntil: 'domcontentloaded' });
      await trang.waitForTimeout(2500);

      const truoc = await demDong(trang);
      test.skip(truoc === 0, 'màn chưa có dữ liệu để lọc');

      const o = trang.locator('#f-q');
      await expect(o, `${man.ten}: không có ô tìm kiếm`).toBeVisible();

      // Chuỗi chắc chắn không khớp gì. Lọc đúng thì phải về 0 dòng.
      await o.fill('zzqqxx-khong-ton-tai-9999');
      await trang.waitForTimeout(2000);
      const sau = await demDong(trang);

      expect(sau, `${man.ten}: gõ chuỗi vô nghĩa mà vẫn ra ${sau} dòng — ô lọc không có tác dụng`)
        .toBeLessThan(truoc);

      // Xoá lọc thì phải quay lại như cũ.
      await o.fill('');
      await trang.waitForTimeout(2000);
      expect(await demDong(trang), `${man.ten}: xoá ô lọc mà danh sách không trở lại`).toBe(truoc);
    });
  }
});
