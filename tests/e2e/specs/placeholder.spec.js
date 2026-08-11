import { test, expect } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import { join } from 'node:path';

/**
 * Mọi ô nhập phải có PLACEHOLDER hoặc GIÁ TRỊ MẶC ĐỊNH.
 *
 * Luật này dễ quên vì thiếu placeholder KHÔNG làm gì hỏng — màn hình vẫn chạy, chỉ là người dùng
 * nhìn vào một ô trống trơn và không biết phải nhập gì, theo định dạng nào. Không có bài canh thì nó
 * trôi lại ngay lần thêm màn tiếp theo.
 *
 * Chạy chính bộ quét trong tools/ chứ không chép lại luật ở đây: hai bản luật sẽ lệch nhau, và bản ở
 * bài kiểm thử thì không ai chạy tay bao giờ để phát hiện.
 *
 * Không cần trình duyệt nên không dùng fixture `trang`.
 */
test('Không ô nhập nào thiếu placeholder hoặc giá trị mặc định', () => {
  const goc = join(process.cwd(), '..', '..');

  const raw = execFileSync('node', ['tools/quet-placeholder.mjs', '--json'], {
    cwd: goc,
    encoding: 'utf8',
    maxBuffer: 8 * 1024 * 1024,
  });

  const kq = JSON.parse(raw);

  // Liệt kê thẳng chỗ thiếu vào thông báo lỗi: bắt người đọc chạy lại công cụ mới biết sai ở đâu là
  // cách nhanh nhất khiến bài kiểm thử bị bỏ qua.
  const chiTiet = kq.tep
    .map((t) => `  ${t.tep}\n` + t.thieu.map((o) => `      ${o.loai} ${o.ten}${o.vi ? ' — ' + o.vi : ''}`).join('\n'))
    .join('\n');

  expect(kq.tong, `Còn ô thiếu placeholder/mặc định:\n${chiTiet}\n\nChạy: node tools/quet-placeholder.mjs`)
    .toBe(0);
});
