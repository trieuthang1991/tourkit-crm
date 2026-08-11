/**
 * Quét ô nhập thiếu PLACEHOLDER hoặc GIÁ TRỊ MẶC ĐỊNH trong toàn bộ Razor Pages.
 *
 *   node tools/quet-placeholder.mjs          # danh sách theo tệp
 *   node tools/quet-placeholder.mjs --json   # dạng JSON cho bài kiểm thử đọc
 *
 * Vì sao cần: ô trống trơn không nói cho người dùng biết phải nhập gì và nhập theo định dạng nào.
 * Với ô ngày hay ô mã thì đó là khác biệt giữa "gõ được ngay" và "gõ sai rồi mới biết". Luật này dễ
 * quên vì thiếu placeholder KHÔNG làm gì hỏng — màn hình vẫn chạy, chỉ khó dùng hơn.
 *
 * Quy ước tính là ĐỦ:
 * - input: có `placeholder`, hoặc có `value=` (giá trị mặc định).
 * - textarea: có `placeholder`.
 * - select: LUÔN có một lựa chọn đang được chọn, nên bản thân nó đã có "mặc định" — trừ hai trường
 *   hợp thật sự để người dùng nhìn vào một ô trống trơn:
 *     · không có <option> nào (ô gọi server) mà cũng không khai `data-placeholder`;
 *     · lựa chọn đầu tiên là <option value=""></option> rỗng, không chữ.
 *   Đừng đòi "— Chọn … —" cho select liệt kê (Trạng thái, Loại…): lựa chọn đầu tiên ở đó chính là
 *   giá trị mặc định, thêm dòng rỗng là bịa ra một trạng thái không nên tồn tại.
 *
 * Bỏ qua: input hidden/checkbox/radio/submit/button/file/reset — không có gì để gợi ý.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const GOC = 'src/TourKit.Api/Pages';

/** Ô nằm trong khối chỉ hiện với MỘT loại bản ghi — xem chú thích ở quet-o-bat-buoc.mjs. */
const BO_QUA_KHOI = /class="tk-loai /;

function duyet(d) {
  const ra = [];
  for (const t of readdirSync(d)) {
    const p = join(d, t);
    if (statSync(p).isDirectory()) { ra.push(...duyet(p)); }
    else if (t.endsWith('.cshtml')) { ra.push(p); }
  }
  return ra;
}

/** Tên ô để người đọc biết phải sửa chỗ nào. */
function ten(thuocTinh) {
  return (thuocTinh.match(/\b(?:name|id)="([^"]+)"/) || [])[1] || '(không tên)';
}

const ra = [];

for (const tep of duyet(GOC)) {
  const html = readFileSync(tep, 'utf8');
  const thieu = [];

  for (const m of html.matchAll(/<input\b([^>]*)>/g)) {
    const a = m[1];
    if (/type=["'](hidden|checkbox|radio|submit|button|file|reset)["']/.test(a)) { continue; }
    if (a.includes('placeholder') || /\bvalue=/.test(a)) { continue; }
    thieu.push({ loai: 'input', ten: ten(a) });
  }

  for (const m of html.matchAll(/<textarea\b([^>]*)>/g)) {
    if (!m[1].includes('placeholder')) { thieu.push({ loai: 'textarea', ten: ten(m[1]) }); }
  }

  // select: xét cả phần thân để tìm dòng nhắc.
  for (const m of html.matchAll(/<select\b([^>]*)>([\s\S]*?)<\/select>/g)) {
    const [, thuoc, than] = m;
    if (thuoc.includes('data-placeholder')) { continue; }
    // Thẻ dựng bằng chuỗi JS ("' + ten + '") không phải markup tĩnh — option của nó ghép lúc
    // chạy nên đọc ở đây luôn thấy rỗng. Bỏ qua, chứ không thì bộ quét báo nhầm rồi bị ngó lơ.
    if (/'\s*\+/.test(thuoc) || /'\s*\+/.test(than)) { continue; }

    const coOption = /<option[ >]/.test(than) || than.includes('@');   // @ = vòng lặp Razor sinh option
    const dauRong = /^\s*<option value=""\s*>\s*<\/option>/.test(than);

    if (coOption && !dauRong) { continue; }
    thieu.push({ loai: 'select', ten: ten(thuoc), vi: coOption ? 'lựa chọn đầu rỗng, không chữ' : 'không có lựa chọn nào và không khai data-placeholder' });
  }

  if (thieu.length) {
    ra.push({ tep: tep.split('\\').join('/').replace(GOC + '/', ''), thieu });
  }
}

ra.sort((a, b) => b.thieu.length - a.thieu.length);
const tong = ra.reduce((s, x) => s + x.thieu.length, 0);

if (process.argv.includes('--json')) {
  console.log(JSON.stringify({ tong, tep: ra }, null, 2));
} else {
  console.log(`${tong} ô thiếu placeholder/mặc định trên ${ra.length} tệp\n`);
  for (const x of ra) {
    console.log(`${String(x.thieu.length).padStart(3)}  ${x.tep}`);
    for (const o of x.thieu) { console.log(`       ${o.loai.padEnd(9)} ${o.ten}${o.vi ? '  — ' + o.vi : ''}`); }
  }
}
