import { test, expect } from '../fixtures.js';

/**
 * Xuất file trên các màn danh sách.
 *
 * Đây là chức năng hầu như không ai thử lại sau khi viết xong: bấm ra file, mở lên xem một lần rồi
 * thôi. Nhưng nó dùng chung bộ lọc với lưới, nên mỗi lần đổi bộ lọc là một lần có thể vỡ mà không
 * ai biết — người dùng chỉ phát hiện khi cần nộp báo cáo.
 *
 * Ba thứ phải đúng: tải được thật, có nội dung, và tên file gợi được nội dung.
 */
const MAN = [
  { ten: 'Khách hàng', duong: '/khach-hang' },
  { ten: 'Cơ hội bán hàng', duong: '/co-hoi' },
  { ten: 'Nhà cung cấp', duong: '/nha-cung-cap' },
  { ten: 'Đơn hàng', duong: '/don-hang' },
  { ten: 'Quỹ vé', duong: '/quy-ve' },
  { ten: 'Phiếu thu', duong: '/phieu-thu' },
  { ten: 'Phiếu chi', duong: '/phieu-chi' },
  { ten: 'Công nợ khách', duong: '/cong-no-khach' },
  { ten: 'Công nợ nhà cung cấp', duong: '/cong-no-ncc' },
];

test.describe('Xuất file', () => {
  for (const man of MAN) {
    test(man.ten, async ({ trang }) => {
      await trang.goto(man.duong, { waitUntil: 'domcontentloaded' });
      await trang.waitForTimeout(2000);

      // Bám nhãn ĐẦY ĐỦ và chỉ lấy phần tử đang hiện. Bắt mỗi chữ "Xuất" thì vớ nhầm cả mục menu
      // đang thu gọn lẫn nút "Đăng xuất" — hai thứ đó khiến bài test đỏ vì lý do hoàn toàn khác.
      const nut = trang.locator(
        '#btn-export:visible, a:visible:has-text("Xuất file"), button:visible:has-text("Xuất file"), '
        + 'a:visible:has-text("Xuất CSV"), button:visible:has-text("Xuất CSV")').first();
      test.skip(await nut.count() === 0, 'màn này không có nút xuất');

      const tai = trang.waitForEvent('download', { timeout: 30_000 });
      await nut.click();

      const file = await tai;
      const duong = await file.path();
      expect(duong, `${man.ten}: bấm Xuất nhưng không có file nào tải về`).toBeTruthy();

      const { size } = await import('node:fs').then((fs) => fs.promises.stat(duong));
      // File CSV chỉ có dòng tiêu đề cũng vài chục byte; dưới 20 byte là rỗng thật.
      expect(size, `${man.ten}: file tải về rỗng`).toBeGreaterThan(20);

      const ten = file.suggestedFilename();
      expect(ten, `${man.ten}: tên file không có đuôi`).toMatch(/\.(csv|xlsx?|zip)$/i);
    });
  }
});
