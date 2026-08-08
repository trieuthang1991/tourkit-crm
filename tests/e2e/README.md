# Kiểm thử giao diện (e2e)

Chạy trình duyệt thật trên ứng dụng thật. Đây là lớp duy nhất chạm tới **JavaScript** — `dotnet test`
không biết gì về nó.

## Vì sao cần

Ba lỗi thật đã lọt qua toàn bộ 763 bài kiểm thử C# và chỉ lộ ra khi có người mở trình duyệt:

| Lỗi | Vì sao C# không bắt được |
|---|---|
| `Url.Page("/khach-hang/Index")` sai kiểu tham số → sửa khách hàng không lưu được | `Url.Page` trả `null` chứ không ném lỗi; đường dẫn chỉ được giải lúc dựng trang |
| JS gọi `?handler=Review`, C# vẫn là `OnPostRunAsync` | Razor trả HTML kèm mã 200 cho handler không tồn tại — không có ngoại lệ nào |
| Trang đăng nhập nạp script mẫu gọi thư viện chưa nạp | Lỗi nằm trong console trình duyệt |

| Nút "Thêm báo giá" trỏ `/bao-gia/Edit` trong khi route thật là `/bao-gia/soan` → 404 | Đường dẫn ghi tay trong `.cshtml`, trình biên dịch không kiểm |

Hai lỗi đầu nay đã có bài kiểm thử C# canh riêng (`UrlPageTargetTests`, `AiHandlerNameTests`). Lỗi
thứ ba thì chỉ e2e mới thấy — và **mọi bài ở đây tự động đỏ nếu trang có lỗi JavaScript**.

## Chạy

```bash
cd tests/e2e
npm install          # lần đầu

npm test             # 22 bài, ~1,3 phút — KHÔNG gọi AI, không tốn tiền
npm run test:ai      #  7 bài, ~1,6 phút — GỌI MODEL THẬT, có tính phí
npm run test:all     # tất cả

npm run xem          # chạy có hiện cửa sổ Chrome để nhìn từng bước
npm run xem:ai
npm run bao-cao      # mở báo cáo HTML của lần chạy gần nhất
```

Máy chủ **tự khởi động** nếu chưa chạy; đang chạy sẵn thì dùng lại (`reuseExistingServer`).

## Cần gì

- Chrome đã cài trên máy (dùng `channel: 'chrome'`, không tải bản riêng của Playwright).
- PostgreSQL đang chạy kèm dữ liệu mẫu.
- Tài khoản `demo-tour / admin@demo.vn / Demo@12345` — đổi bằng biến môi trường `TK_SLUG`,
  `TK_EMAIL`, `TK_PASSWORD`.
- Riêng nhóm `@ai`: `Ai:Enabled = true` và có khoá thật trong `appsettings.json`.

## Nguyên tắc khi viết thêm bài

**Không ghi cứng id bản ghi.** Dữ liệu mẫu dựng lại là id đổi, và một bài đỏ vì id cũ không còn thì
người đọc tưởng tính năng hỏng. Dùng `idKhachHangBatKy()` hoặc đọc từ handler dữ liệu của màn.

**Chờ cả kết quả LẪN lỗi.** Chỉ chờ phần tử thành công thì khi hỏng, bài test treo tới hết giờ rồi
báo "timeout" — che mất thông báo lỗi thật đang hiện trên màn hình:

```js
const ket = trang.locator('.tk-ai-text');
const loi = trang.locator('.text-danger');
await expect(ket.or(loi).first()).toBeVisible();
if (await loi.count()) { throw new Error(await loi.first().innerText()); }
```

**Kích vào dòng lưới KHÔNG mở gì** — đó là quy ước của mọi lưới trong hệ thống. Mở bản ghi bằng
đường dẫn sâu (`/co-hoi?mo={id}`) hoặc nút trong thanh tác vụ.

**Kiểm tra dữ liệu đã lưu bằng cách tải lại trang.** Panel đóng lại không chứng minh được gì — nó
đóng cả khi lưu hỏng.

**Đừng nới danh sách bỏ qua lỗi JavaScript** (`BO_QUA` trong `fixtures.js`) để làm bài test xanh.
Thêm bừa vào đó là vô hiệu hoá đúng thứ mà bộ kiểm thử này sinh ra để bắt.

## Các nhóm bài

| Nhóm | Soát gì |
|---|---|
| `smoke` | 16 màn mở được, không lỗi JavaScript, sai mật khẩu bị chặn |
| `audit-lists` | Lưới có dòng hoặc nói rõ là rỗng · không có chữ rác `undefined`/`NaN` · trang không cuộn ngang · ô chọn có lựa chọn · nút "Thêm" mở được thật · nút icon phải có tooltip |
| `audit-lists` (lọc) | Gõ chuỗi vô nghĩa vào `#f-q` phải ra ít dòng hơn; xoá đi phải trở lại như cũ |
| `audit-links` | Mọi `<a href>` nội bộ trên 28 màn không được dẫn tới 404 |
| `customer-save` | Lưu từ màn chi tiết, kiểm tra bằng cách tải lại trang |
| `ai-*` | Trợ lý tra cứu và ba nút AI trên bản ghi |

## Chưa phủ

- Tải ảnh lên trong luồng trao đổi.
- Phân quyền trên giao diện (đã có 18 bài đơn vị ở `AiRecordAccessTests`).
- Bộ câu hỏi vàng cho trợ lý (spec §8 yêu cầu ~30 câu kèm đáp án, chạy thưa và so bằng tay).
