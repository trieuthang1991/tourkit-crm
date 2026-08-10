# EF Core / truy vấn dữ liệu

Trước khi viết hay sửa BẤT KỲ truy vấn nào, đọc `.claude/skills/ef-core/SKILL.md`.

## Luật bắt buộc của repo này

1. **Lọc / sắp xếp / phân trang / đếm / cộng phải làm ở SQL.** Cấm nạp cả bảng rồi
   `.Where()`/`.Skip()`/`.Count()`/`.Sum()` trong bộ nhớ. Nhớ rằng
   `IRepository.ListAsync(predicate)` **materialize ngay** — mọi thứ viết sau nó là LINQ-to-Objects.
   Cần đếm thì dùng `CountAsync`, cần kiểm tra tồn tại thì `AnyAsync`, cần phân trang thì `PageAsync`.

2. **Danh sách nghiệp vụ luôn phân trang từ server** (`tk.table` + `OnGetDataAsync` + `DtJson`).
   Chỉ danh mục nhỏ mới được `tk.tableClient`. Xem `no-get-all-server-paging`.

3. **Khung nhìn giàu (lịch, kanban, ma trận) nạp có biên**: kanban lấy top-N mỗi cột;
   lịch chỉ nạp khoảng đang xem; ma trận giới hạn cửa sổ ngày + phân trang hàng.

4. **Đừng tra bảng để lấy thứ đã có trong cookie.** Tên/email/quyền của người đang đăng nhập nằm
   sẵn trong claim (`name`, `email`, `perm`) — đọc `User.FindFirst(...)`, không truy vấn bảng Users.

5. **Danh mục tra cứu lặp lại thì dùng cache**, đừng gọi lại mỗi lần AJAX phân trang.
   Danh bạ nhân viên đã có `TourKit.Api.Services.UserDirectory` (cache 60 giây, tách theo tenant) —
   dùng nó thay cho `IUserAdminService.ListAsync()` ở các handler dữ liệu.

6. **Ngày nghiệp vụ neo offset 0** bằng `TkDate.Day()`. Xem mục Ngày tháng bên dưới.

## Lệch pha đã biết giữa skill và repo

Skill khuyên "đừng bọc DbContext trong Repository". Repo này **đang** bọc (`IRepository<T>`), và
609 test dựa vào `FakeRepository`. **Không** tự ý bỏ lớp repository — nếu thấy cần, hỏi trước.
Trong lúc còn lớp này: query phức tạp thì tạo interface repo riêng đẩy được xuống SQL, chứ đừng
lọc trong bộ nhớ cho tiện.

## Ngày tháng

Ngày nghiệp vụ (khởi hành, ngày sinh, hạn thanh toán, ngày hoá đơn) **không có múi giờ**.
Lưu phải qua `TkDate.Day()` (bỏ giờ, neo `TimeSpan.Zero`). Gọi `.ToUniversalTime()` trên loại
field này sẽ làm ngày **lùi một ngày** ở múi giờ VN. Mốc thời gian thật (giờ nhắc, giờ tạo) thì
ngược lại: giữ UTC.

Ô ngày trên giao diện luôn cấu hình flatpickr `dateFormat:'Y-m-d', altInput:true, altFormat:'d/m/Y'`
— gửi ISO, hiện d/m/Y. Gửi thẳng `dd/MM/yyyy` thì model binding không parse được và field âm thầm
về null (mất dữ liệu).
