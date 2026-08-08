import { test, expect } from '../fixtures.js';

/**
 * Mọi đường dẫn nội bộ trên màn chính phải đi tới nơi có thật.
 *
 * Bài này sinh ra từ một lỗi thật: nút "Thêm báo giá" trỏ `/bao-gia/Edit` trong khi route thật là
 * `/bao-gia/soan`. Bấm vào ra trang 404 — nghĩa là KHÔNG tạo được báo giá từ màn danh sách, một
 * chức năng chính chết hẳn. Đường dẫn ghi tay trong .cshtml không được trình biên dịch kiểm tra,
 * nên không có lớp nào khác bắt được.
 *
 * Chỉ soát link TĨNH trong HTML. Link do JavaScript dựng trong ô lưới nằm ngoài phạm vi — chúng
 * cần dữ liệu thật và được các bài khác chạm tới.
 */
const MAN = [
  '/tong-quan', '/ban-lam-viec', '/khach-hang', '/co-hoi', '/don-hang', '/bao-gia',
  '/chuyen-di', '/quy-ve', '/nha-cung-cap', '/bang-gia-ncc', '/cong-viec', '/lich-hen',
  '/phieu-thu', '/phieu-chi', '/hoa-don', '/cong-no-khach', '/cong-no-ncc', '/dong-tien',
  '/bao-cao-tong-hop', '/bao-cao-nhan-vien', '/thu-chi-theo-tour', '/cau-hinh',
  '/thanh-vien', '/vai-tro', '/dai-ly', '/chien-dich', '/du-an', '/nhat-ky',
];

test('Không có đường dẫn nội bộ nào dẫn tới 404', async ({ trang }) => {
  test.slow();

  const hong = [];
  const daThu = new Set();

  for (const man of MAN) {
    const res = await trang.goto(man, { waitUntil: 'domcontentloaded' });
    if ((res?.status() ?? 500) >= 400) {
      hong.push(`${man} — chính màn này trả ${res?.status()}`);
      continue;
    }

    const links = await trang.evaluate(() => Array.from(document.querySelectorAll('a[href]'))
      .map((a) => a.getAttribute('href'))
      .filter((h) => h
        && h.startsWith('/')          // chỉ link nội bộ
        && !h.startsWith('//')
        && !h.includes('{')           // mẫu route chưa thay tham số
        && !h.startsWith('/dang-xuat')));   // bấm vào là mất phiên, không thử

    for (const href of new Set(links)) {
      if (daThu.has(href)) { continue; }
      daThu.add(href);

      // Dùng chính phiên đăng nhập của trang; fetch trong ngữ cảnh trang nên cookie tự đi kèm.
      const ma = await trang.evaluate(async (u) => {
        try {
          const r = await fetch(u, { method: 'GET', redirect: 'follow' });
          return r.status;
        } catch {
          return 0;
        }
      }, href);

      if (ma >= 400) {
        hong.push(`${href} → ${ma}  (thấy ở ${man})`);
      }
    }
  }

  expect(hong, `Đường dẫn hỏng (${hong.length}):\n  ` + hong.join('\n  ')).toEqual([]);
});
