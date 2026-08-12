/**
 * Quét class `tk-*` được dùng trong Razor/JS nhưng KHÔNG có định nghĩa trong CSS của dự án.
 *
 *   node tools/quet-class-tk.mjs          # danh sách chỗ dùng sai
 *   node tools/quet-class-tk.mjs --json   # dạng JSON cho bài kiểm thử đọc
 *
 * Vì sao cần: gõ nhầm hoặc TỰ BỊA một tên class không làm hỏng gì — trang vẫn render, chỉ là mất
 * hẳn phần định dạng đó. Không có gì báo, và người viết thường không nhận ra vì họ nhìn màn hình
 * ngay sau khi sửa nội dung chứ không so với màn khác. Đã xảy ra hai lần trong một buổi: `tk-toolbar`
 * (thanh lọc mất padding) và mượn `tk-surface`/`tk-stat-strip` sang trang báo cáo.
 *
 * Chỉ xét tiền tố `tk-` — đó là phần class DO DỰ ÁN sở hữu. Class của Bootstrap/Vuexy nằm ở vendor,
 * không quét được theo cách này và cũng không phải chỗ hay sai.
 *
 * LUẬT: một class chỉ bị báo khi nó xuất hiện TRONG MARKUP mà KHÔNG xuất hiện ở đâu khác — không
 * trong tệp CSS nào, không trong khối <style> của trang, và không trong bất kỳ selector JavaScript
 * nào. Nhiều class `tk-*` cố ý không có kiểu dáng: chúng là MÓC cho JS (`.tk-loai-ks` để ẩn/hiện
 * theo loại NCC). Đòi mọi class phải có CSS sẽ báo nhầm hàng loạt rồi bị ngó lơ — đúng cách một bộ
 * canh trở thành vô dụng.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const GOC_VIEW = 'src/TourKit.Api/Pages';
const GOC_JS = 'src/TourKit.Api/wwwroot/js';
const GOC_CSS = 'src/TourKit.Api/wwwroot/css';

function duyet(d, duoi) {
  const ra = [];
  for (const t of readdirSync(d)) {
    const p = join(d, t);
    if (statSync(p).isDirectory()) { ra.push(...duyet(p, duoi)); }
    else if (duoi.some((x) => t.endsWith(x))) { ra.push(p); }
  }
  return ra;
}

/**
 * Mọi nơi một class có thể được "công nhận": tệp CSS của dự án, khối <style> trong trang, và
 * selector trong JavaScript. Gom hết vào một tập rồi mới đối chiếu.
 */
const coNoiDung = new Set();
function nhat(text) {
  // `.tk-abc` trong CSS/selector — KHÔNG lấy từ thuộc tính class="..." (đó là chỗ DÙNG, không phải
  // chỗ khai), nên phải bỏ các đoạn class="…" ra trước.
  const sach = text.replace(/class\s*=\s*["'][^"']*["']/g, '');
  for (const m of sach.matchAll(/\.(tk-[a-z0-9-]+)/g)) { coNoiDung.add(m[1]); }
}

for (const f of duyet(GOC_CSS, ['.css'])) { nhat(readFileSync(f, 'utf8')); }
for (const f of duyet(GOC_JS, ['.js'])) { nhat(readFileSync(f, 'utf8')); }
for (const f of duyet(GOC_VIEW, ['.cshtml'])) { nhat(readFileSync(f, 'utf8')); }
// Công cụ trong tools/ bám class làm dấu nhận biết khối, nhưng viết dưới dạng thuộc tính chứ không
// phải selector — nên ở đây nhặt theo TÊN TRẦN, không đòi dấu chấm.
//
// BỎ QUA chính tệp này: chú thích đầu file nhắc tên các class từng dùng sai, quét cả nó thì chúng
// thành "hợp lệ" vĩnh viễn và bộ canh tự vô hiệu hoá đúng thứ nó sinh ra để bắt.
for (const f of duyet('tools', ['.mjs'])) {
  if (f.endsWith('quet-class-tk.mjs')) { continue; }
  for (const m of readFileSync(f, 'utf8').matchAll(/(tk-[a-z0-9-]+)/g)) { coNoiDung.add(m[1]); }
}

const ra = [];
for (const tep of duyet(GOC_VIEW, ['.cshtml'])) {
  const noiDung = readFileSync(tep, 'utf8');
  const thieu = new Map();

  // Chỉ lấy class trong thuộc tính class="..." — nhắc tới tên class trong chú thích không tính.
  for (const m of noiDung.matchAll(/class\s*=\s*["']([^"']*)["']/g)) {
    for (const c of m[1].split(/\s+/)) {
      if (!c.startsWith('tk-') || coNoiDung.has(c)) { continue; }
      thieu.set(c, (thieu.get(c) || 0) + 1);
    }
  }

  if (thieu.size) {
    ra.push({ tep: tep.split('\\').join('/'), classes: [...thieu.keys()].sort() });
  }
}

const tong = ra.reduce((s, x) => s + x.classes.length, 0);

if (process.argv.includes('--json')) {
  console.log(JSON.stringify({ tong, tep: ra }, null, 2));
} else {
  console.log(`${tong} class tk-* không có định nghĩa CSS, trên ${ra.length} tệp\n`);
  for (const x of ra) { console.log(`  ${x.tep}\n      ${x.classes.join(', ')}`); }
}
