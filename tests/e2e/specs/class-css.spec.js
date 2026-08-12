import { test, expect } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import { join } from 'node:path';

/**
 * Không màn nào được dùng class `tk-*` mà chẳng có ở đâu khác.
 *
 * Gõ nhầm hoặc TỰ BỊA một tên class không làm hỏng gì: trang vẫn render, chỉ mất hẳn phần định dạng
 * đó. Người viết thường không nhận ra vì họ nhìn màn mình vừa sửa chứ không so với màn khác — chủ dự
 * án phát hiện, không phải bộ kiểm thử. Đã xảy ra hai lần trong một buổi: một thanh lọc mất padding
 * vì tên class không tồn tại, và một trang báo cáo mượn class của màn danh sách.
 *
 * Chạy chính bộ quét trong tools/ chứ không chép lại luật: hai bản luật sẽ lệch nhau, và bản nằm
 * trong bài kiểm thử thì không ai chạy tay bao giờ để phát hiện.
 *
 * Không cần trình duyệt nên không dùng fixture `trang`.
 */
test('Không màn nào dùng class tk-* không tồn tại', () => {
  const goc = join(process.cwd(), '..', '..');

  const raw = execFileSync('node', ['tools/quet-class-tk.mjs', '--json'], {
    cwd: goc,
    encoding: 'utf8',
    maxBuffer: 8 * 1024 * 1024,
  });

  const kq = JSON.parse(raw);

  const chiTiet = kq.tep.map((t) => `  ${t.tep}\n      ${t.classes.join(', ')}`).join('\n');

  expect(kq.tong, `Class tk-* không có định nghĩa CSS, cũng không phải móc JS:\n${chiTiet}\n\n` +
    `Chạy: node tools/quet-class-tk.mjs`).toBe(0);
});
