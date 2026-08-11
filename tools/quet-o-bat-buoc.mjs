/**
 * Quét markup để lập danh sách "ô mang dấu sao đỏ" của từng form, cho bài e2e o-bat-buoc.spec.js.
 *
 * Chạy lại mỗi khi thêm/bớt ô bắt buộc:
 *   node tools/quet-o-bat-buoc.mjs > tests/e2e/specs/o-bat-buoc.data.js
 *
 * Vì sao sinh tự động thay vì viết tay: danh sách viết tay sẽ mục ngay lần đầu có người thêm ô mới
 * mà không nhớ cập nhật, và một bài kiểm thử bỏ sót thì im lặng — không ai biết nó đã ngừng bảo vệ.
 */
import { readFileSync, readdirSync, statSync } from 'node:fs';
import { join } from 'node:path';

const API = 'src/TourKit.Api';
const GOC = API + '/Pages';

// Route tiếng Việt: RouteMap.cs là nguồn duy nhất (xem chú thích trong chính file đó).
const rm = readFileSync(API + '/Routing/RouteMap.cs', 'utf8');
const route = new Map();
for (const m of rm.matchAll(/\("(\/[^"]+)",\s*"([^"]+)"\)/g)) route.set(m[1], m[2]);

function duyet(d) {
  const ra = [];
  for (const t of readdirSync(d)) {
    const p = join(d, t);
    if (statSync(p).isDirectory()) ra.push(...duyet(p));
    else if (t.endsWith('.cshtml')) ra.push(p);
  }
  return ra;
}

/**
 * Chỉ lấy phần bên trong <form id="frm">.
 *
 * Một trang có thể có nhiều form (ví dụ /bai-viet có thêm form bình luận). Quét cả file thì gán
 * nhầm ô của form khác vào form chính, và bài kiểm thử đỏ vì một ô không hề tồn tại ở đó.
 */
function thanForm(html) {
  const bd = html.search(/<form\b[^>]*id="frm"/);
  if (bd < 0) return null;
  const ket = html.indexOf('</form>', bd);
  return ket < 0 ? html.slice(bd) : html.slice(bd, ket);
}

/**
 * Bỏ các khối `class="tk-loai ..."` — ô chỉ hiện với MỘT loại bản ghi.
 *
 * Bài o-bat-buoc mở form ở trạng thái mặc định rồi kiểm "bỏ trống ô có dấu * thì phải báo lỗi". Ô
 * nằm trong khối đang ẩn không thoả điều đó: nó không hiện ra để điền, và luật kiểm tra của nó cũng
 * chỉ bật khi đúng loại. Giữ chúng lại là bài kiểm thử đỏ vì một ô người dùng không nhìn thấy.
 *
 * Chúng vẫn được kiểm — bằng bài e2e riêng của từng loại (ncc-truong-theo-loai.spec.js).
 */
function boKhoiTheoLoai(than) {
  if (!than) return than;

  // Cắt theo cặp <div> lồng nhau, không dùng regex tham lam: khối có div con bên trong.
  let ra = '';
  let i = 0;
  for (;;) {
    const bd = than.indexOf('class="tk-loai ', i);
    if (bd < 0) { ra += than.slice(i); return ra; }

    const moDau = than.lastIndexOf('<div', bd);
    ra += than.slice(i, moDau);

    let sau = than.indexOf('>', bd) + 1;
    let sau2 = sau;
    for (let sauDo = 1; sauDo > 0;) {
      const mo = than.indexOf('<div', sau2);
      const dong = than.indexOf('</div>', sau2);
      if (dong < 0) return ra;
      if (mo >= 0 && mo < dong) { sauDo++; sau2 = mo + 4; continue; }
      sauDo--; sau2 = dong + 6;
    }
    i = sau2;
  }
}

const ra = [];
for (const tep of duyet(GOC)) {
  const html = readFileSync(tep, 'utf8');
  if (!/offcanvas: *'#oc', *form: *'#frm'/.test(html)) continue;
  if (!/oc\.open\(null\)/.test(html)) continue;

  const than = boKhoiTheoLoai(thanForm(html));
  if (!than) continue;

  // Quy ước của repo: label nằm NGAY TRÊN field, nên mỗi field lấy label gần nhất đứng trước.
  const moc = [];
  for (const m of than.matchAll(/<label\b[^>]*>([\s\S]*?)<\/label>/g)) {
    moc.push({
      laLabel: true, vt: m.index,
      sao: /text-danger[^>]*>\s*\*/.test(m[1]),
      chu: m[1].replace(/<[^>]*>/g, '').replace(/\*/g, '').trim(),
    });
  }
  for (const m of than.matchAll(/<(input|select|textarea)\b([^>]*)>/g)) {
    if (/type=["'](hidden|submit|button)["']/.test(m[2])) continue;
    const ten = m[2].match(/name=["']([^"']+)["']/)?.[1];
    if (ten) moc.push({ laLabel: false, vt: m.index, ten });
  }
  moc.sort((a, b) => a.vt - b.vt);

  const fields = [];
  let lb = null;
  for (const x of moc) {
    if (x.laLabel) { lb = x; continue; }
    if (lb?.sao) fields.push({ ten: x.ten, nhan: lb.chu });
    lb = null;
  }
  if (!fields.length) continue;

  const trang = tep.split('\\').join('/').replace(GOC, '').replace('.cshtml', '');
  const r = route.get(trang);
  if (!r) { console.error(`Bỏ qua ${trang}: không có route trong RouteMap.cs`); continue; }
  ra.push({ route: '/' + r, fields });
}

ra.sort((a, b) => a.route.localeCompare(b.route));

console.log('/** SINH TỰ ĐỘNG — đừng sửa tay.');
console.log(' * Cập nhật: node tools/quet-o-bat-buoc.mjs > tests/e2e/specs/o-bat-buoc.data.js');
console.log(' */');
console.log('export default ' + JSON.stringify(ra, null, 2) + ';');
console.error(`${ra.length} form, ${ra.reduce((s, x) => s + x.fields.length, 0)} ô bắt buộc`);
