# Thiết kế đăng nhập không cần mã doanh nghiệp và Google OAuth

**Ngày:** 2026-08-08

**Trạng thái:** Đã được chủ dự án duyệt qua trao đổi

**Phạm vi giao diện:** Razor Pages production (`/dang-nhap`, `/dang-ky`, `/quen-mat-khau`) và hợp đồng auth backend. Frontend React cũ trong `web/` không thuộc phạm vi.

## 1. Mục tiêu

Chuẩn hóa xác thực để người dùng không phải nhớ hoặc nhập mã doanh nghiệp khi đăng nhập và khôi phục mật khẩu. Mỗi email chỉ được thuộc một doanh nghiệp trên toàn hệ thống. Hệ thống tiếp tục hỗ trợ email/mật khẩu và bổ sung đăng nhập Google.

Người dùng Google chưa có tài khoản được chuyển sang onboarding, bắt buộc hoàn thành thông tin doanh nghiệp trước khi hệ thống tạo tenant. Tài khoản Google không phải đặt mật khẩu trong onboarding.

## 2. Quyết định chính

- Email người dùng là duy nhất toàn hệ thống, so sánh không phân biệt hoa/thường và bỏ khoảng trắng đầu/cuối.
- Form đăng nhập chỉ còn email, mật khẩu, ghi nhớ đăng nhập và nút “Tiếp tục với Google”.
- Tenant được suy ra từ user sau khi xác thực; client không truyền `TenantSlug` khi đăng nhập hoặc quên mật khẩu.
- Dùng Google OAuth native của ASP.NET Core. Hệ thống TourKit/M-Travel tiếp tục sở hữu user, tenant, RBAC, cookie và JWT.
- Danh tính Google được liên kết bằng subject ID ổn định của Google, không chỉ dựa vào email ở mọi lần đăng nhập.
- Google chỉ được liên kết tự động với user hiện có khi Google trả về email đã xác minh và email đó khớp duy nhất.
- Google email mới phải hoàn thành onboarding doanh nghiệp; không tạo user hoặc tenant trước khi form hợp lệ được gửi.
- `PasswordHash` tiếp tục là trường bắt buộc. Tài khoản tạo qua Google nhận một mật khẩu ngẫu nhiên riêng, sinh bằng CSPRNG, được hash rồi giá trị gốc bị hủy ngay.
- Không dùng mật khẩu mặc định cố định hoặc dùng chung giữa các tài khoản.
- Người dùng Google có thể dùng “Quên mật khẩu” để tự đặt mật khẩu thật sau này.
- Chỉ triển khai Google trong đợt này. Mô hình liên kết danh tính cho phép thêm Microsoft hoặc nhà cung cấp khác sau này mà không đổi bảng user.

## 3. Phương án đã chọn

### 3.1 Phương án chọn: auth nội bộ + Google OAuth native

Giữ nguyên kiến trúc xác thực hiện tại, bổ sung Google handler, external cookie tạm thời và một dịch vụ liên kết danh tính. Sau khi Google xác thực, hệ thống luôn phát principal nội bộ có `sub`, `tenant_id`, `email`, `name` và `perm`; principal Google không được dùng trực tiếp để truy cập ứng dụng.

Phương án này có ít phụ thuộc vận hành, giữ nguyên tenancy/RBAC hiện có và không phải di chuyển toàn bộ auth sang dịch vụ ngoài.

### 3.2 Phương án không chọn

- **Auth0/Clerk/Firebase Auth:** hỗ trợ nhiều provider nhanh nhưng tạo một cuộc di chuyển auth lớn, thêm phụ thuộc và chi phí ngoài phạm vi.
- **Tra email Google ở mọi lần đăng nhập mà không lưu liên kết:** ít mã hơn nhưng yếu hơn về quản trị danh tính, audit và xử lý khi email phía provider thay đổi.

## 4. Mô hình dữ liệu

### 4.1 Email chuẩn hóa toàn cục

`User` có thêm giá trị email chuẩn hóa (ví dụ `NormalizedEmail`) được tạo từ `Trim()` và phép đổi chữ nhất quán. Database đặt unique index toàn cục lên giá trị này, thay cho unique index `(TenantId, Email)` hiện tại.

Mọi đường tạo user đều phải dùng chung một hàm chuẩn hóa:

- đăng ký doanh nghiệp bằng email/mật khẩu;
- onboarding Google;
- quản trị viên tạo nhân viên;
- seed dữ liệu và test fixture.

Các truy vấn xác thực ẩn danh được phép bỏ tenant query filter chỉ trong lớp identity chuyên trách, tìm user bằng email chuẩn hóa rồi mới thiết lập `AmbientTenantContext` từ `User.TenantId`.

### 4.2 Liên kết tài khoản ngoài

Thêm entity liên kết danh tính ngoài với các trường tối thiểu:

- `TenantId`;
- `UserId`;
- `Provider` (đợt này là `Google`);
- `ProviderSubject`;
- email provider gần nhất dùng cho audit;
- các trường audit kế thừa theo convention dự án.

Ràng buộc database:

- unique toàn cục `(Provider, ProviderSubject)`;
- unique `(UserId, Provider)` để một user chỉ có một liên kết cho mỗi provider;
- foreign key tới `User`;
- dữ liệu liên kết tuân theo tenant isolation như các entity tenant khác.

## 5. Thành phần và trách nhiệm

### 5.1 Identity lookup dùng chung

Một component identity tập trung chịu trách nhiệm:

- chuẩn hóa email;
- tìm user toàn cục bằng email chuẩn hóa;
- kiểm tra email đã tồn tại trước khi đăng ký/tạo nhân viên;
- kiểm tra user và tenant còn hoạt động;
- thiết lập tenant context chỉ sau khi đã xác định user.

Component này tránh lặp lại truy vấn `IgnoreQueryFilters()` trong password auth, cookie auth, password reset và Google auth.

### 5.2 Password auth

`LoginRequest` và các service password auth chỉ nhận `Email` và `Password`. Sau khi tìm user theo email chuẩn hóa, service xác minh mật khẩu, trạng thái user/tenant, thiết lập tenant context, tải quyền và phát cookie hoặc JWT như hiện tại.

### 5.3 Google external auth

ASP.NET Core cấu hình ba scheme liên quan:

- cookie nội bộ hiện tại cho phiên ứng dụng;
- external cookie ngắn hạn chỉ dùng trong handshake/onboarding;
- Google OAuth challenge/callback.

Google principal chỉ tồn tại trong external cookie. Callback gọi dịch vụ external auth để liên kết hoặc xác định onboarding, sau đó external cookie bị xóa. Chỉ principal nội bộ do ứng dụng dựng mới được đăng nhập vào cookie chính.

### 5.4 Provisioning

Provisioning hỗ trợ hai đầu vào:

- đăng ký thường: công ty, slug, họ tên, email, mật khẩu;
- đăng ký Google: công ty, slug, họ tên, email đã khóa từ Google, Google subject và mật khẩu ngẫu nhiên nội bộ.

Thao tác tạo tenant, Admin user, role, permissions, subscription mặc định và external login phải atomic trên database quan hệ. Nếu bất kỳ bước nào lỗi, không để lại tenant hoặc user một phần.

## 6. Luồng người dùng

### 6.1 Đăng nhập bằng email/mật khẩu

1. Người dùng nhập email và mật khẩu.
2. Server chuẩn hóa email và tìm đúng một user toàn hệ thống.
3. Server kiểm tra user, tenant và mật khẩu.
4. Server thiết lập tenant context, tải permissions và phát cookie/JWT.
5. Người dùng được chuyển tới `returnUrl` nội bộ hợp lệ hoặc `/tong-quan`.

Không còn trường mã doanh nghiệp và không còn giá trị demo điền sẵn.

### 6.2 Google với tài khoản đã liên kết

1. Người dùng chọn “Tiếp tục với Google”.
2. Google callback trả subject và email đã xác minh.
3. Hệ thống tìm external login theo `(Google, subject)`.
4. Liên kết theo subject là định danh chính; email mới từ Google chỉ được cập nhật làm dữ liệu audit, không dùng để chuyển liên kết sang user khác.
5. Hệ thống kiểm tra user/tenant, dựng principal nội bộ và đăng nhập.

### 6.3 Google lần đầu với email đã có

1. Không tìm thấy external login theo subject.
2. Hệ thống tìm user duy nhất bằng email Google đã chuẩn hóa.
3. Nếu user/tenant hợp lệ, hệ thống tạo external login cho user đó.
4. Hệ thống đăng nhập bằng principal nội bộ.

Unique constraints bảo vệ khỏi hai callback đồng thời tạo liên kết trùng.

### 6.4 Google lần đầu với email mới

1. Không tìm thấy external login hoặc user theo email.
2. Server giữ Google identity trong external cookie ngắn hạn và chuyển tới trang đăng ký ở chế độ Google.
3. Trang đăng ký hiển thị email ở trạng thái chỉ đọc, điền sẵn họ tên nếu Google cung cấp.
4. Người dùng bắt buộc nhập tên doanh nghiệp, mã doanh nghiệp và đồng ý điều khoản; họ tên vẫn phải hợp lệ.
5. Server không tin email/subject gửi lại từ form mà đọc từ external cookie đã xác thực.
6. Provisioning tạo toàn bộ tenant, Admin user, quyền, subscription và Google link trong một giao dịch.
7. Mật khẩu ngẫu nhiên riêng được sinh, hash và bỏ bản rõ ngay.
8. External cookie bị xóa; user được đăng nhập bằng cookie nội bộ và chuyển tới `/tong-quan`.

Nếu người dùng đóng trang hoặc external cookie hết hạn, không có dữ liệu nào được tạo. Họ phải bắt đầu lại từ nút Google.

### 6.5 Quên mật khẩu

Form chỉ nhận email. Service tìm user toàn cục bằng email chuẩn hóa nhưng luôn trả cùng một thông báo bên ngoài, dù email có tồn tại hay không. Link reset hiện tại tiếp tục mang user/tenant trong payload được Data Protection bảo vệ.

Tài khoản Google có mật khẩu ngẫu nhiên cũng dùng được luồng này; sau khi reset, mật khẩu mới thay thế hash ngẫu nhiên và các refresh token còn sống bị thu hồi theo hành vi hiện tại.

## 7. Xử lý lỗi và bảo mật

- Password login sai luôn trả thông báo chung, không tiết lộ email, user, tenant hoặc mật khẩu sai ở bước nào.
- Google callback thiếu subject, thiếu email hoặc email chưa xác minh bị từ chối.
- External login trỏ tới user bị khóa, đã xóa hoặc tenant ngừng hoạt động bị từ chối.
- Subject chưa liên kết nhưng email khớp một user đã có Google subject khác bị từ chối; hệ thống không tự động chuyển, thay thế hoặc gộp liên kết.
- `returnUrl` chỉ được chấp nhận khi là URL nội bộ để ngăn open redirect.
- External cookie có thời hạn ngắn, HttpOnly, Secure ở production, SameSite phù hợp với OAuth callback và bị xóa sau khi hoàn tất/hủy.
- Client secret chỉ đọc từ cấu hình môi trường/user secrets, không commit vào repository hoặc ghi log.
- Khi Google chưa được cấu hình đầy đủ, ứng dụng vẫn khởi động, ẩn nút Google và giữ password login hoạt động.
- Mật khẩu ngẫu nhiên dùng ít nhất 32 byte entropy từ CSPRNG, không log, không gửi cho client và không lưu bản rõ.
- Các unique constraint là lớp bảo vệ cuối cho email và external identity; lỗi cạnh tranh được chuyển thành thông báo conflict có kiểm soát.

## 8. Cấu hình

Thêm section cấu hình không chứa bí mật thật, gồm cờ bật và hai khóa Google OAuth. `appsettings.example.json` chỉ mô tả/để trống credential. Production dùng biến môi trường tương ứng, ví dụ:

- `Authentication__Google__Enabled`;
- `Authentication__Google__ClientId`;
- `Authentication__Google__ClientSecret`.

Nút Google chỉ render khi `Enabled=true` và cả Client ID/Secret hợp lệ. Redirect URI được Google Console cấu hình theo callback HTTPS của từng môi trường.

## 9. Migration và dữ liệu cũ

Trước khi tạo unique index toàn cục, migration/deployment phải kiểm tra trùng email sau chuẩn hóa, kể cả khác hoa/thường hoặc khoảng trắng. Nếu có trùng, deployment dừng với báo cáo để chủ dữ liệu quyết định tài khoản nào được giữ/chuyển; không tự gộp tenant, role, dữ liệu hoặc xóa user.

Sau khi dữ liệu sạch:

1. thêm và backfill email chuẩn hóa;
2. tạo unique index toàn cục;
3. bỏ unique index `(TenantId, Email)` cũ;
4. tạo bảng external login và các constraint;
5. giữ `PasswordHash` bắt buộc, không cần migration nullable.

Quy trình phải hoạt động với provider database được dự án hỗ trợ; SQL backfill/preflight phải được kiểm tra cho PostgreSQL production và SQLite development/test.

## 10. Kiểm thử

### 10.1 Email và password auth

- đăng nhập thành công chỉ bằng email/mật khẩu;
- email không phân biệt hoa/thường và bỏ khoảng trắng;
- sai mật khẩu, user khóa, tenant bị xóa/ngừng hoạt động đều thất bại;
- principal/JWT vẫn chứa đúng tenant và permissions;
- đăng ký thường từ chối email đã thuộc bất kỳ tenant nào;
- quản trị viên tạo nhân viên từ chối email đã tồn tại toàn hệ thống;
- quên mật khẩu chỉ cần email và không làm lộ tài khoản tồn tại.

### 10.2 Google auth

- external login đã liên kết đăng nhập đúng user/tenant;
- subject mới + email cũ đã xác minh tạo liên kết rồi đăng nhập;
- subject mới + email mới chuyển onboarding, chưa tạo DB record;
- email chưa xác minh, callback thiếu claim hoặc external cookie hết hạn bị từ chối;
- callback lặp/đồng thời không tạo hai liên kết;
- user/tenant không hoạt động bị từ chối;
- external cookie được giữ khi chuyển sang onboarding; nó bị xóa sau đăng nhập thành công hoặc lỗi kết thúc luồng.

### 10.3 Google onboarding

- email hiển thị read-only và server bỏ qua mọi email/subject giả mạo từ form;
- thiếu company name, slug, full name hoặc điều khoản không tạo dữ liệu;
- slug hoặc email bị chiếm trong lúc onboarding trả conflict có kiểm soát;
- success tạo đủ tenant, Admin user, role/permissions, subscription và external login;
- password hash được tạo nhưng mật khẩu ngẫu nhiên không xuất hiện trong response/log;
- lỗi giữa provisioning rollback toàn bộ;
- hoàn tất onboarding tự đăng nhập vào đúng tenant.

### 10.4 Giao diện và cấu hình

- `/dang-nhap` không render mã doanh nghiệp và có nút Google khi cấu hình đủ;
- `/quen-mat-khau` không render mã doanh nghiệp;
- thiếu cấu hình Google thì nút bị ẩn và password login vẫn hoạt động;
- return URL ngoài hệ thống không được redirect;
- Razor smoke tests, auth integration tests, tenancy tests và test suite đầy đủ đều xanh.

## 11. Phạm vi không làm

- Không thêm Microsoft, Facebook, Apple hoặc provider khác trong đợt này.
- Không tự động nhập user vào nhiều doanh nghiệp; một email chỉ thuộc một doanh nghiệp.
- Không cho chọn workspace/tenant sau đăng nhập.
- Không tự gộp hoặc xóa tài khoản trùng trong dữ liệu cũ.
- Không thay auth nội bộ bằng Identity SaaS bên ngoài.
- Không phát triển thêm frontend React cũ trong `web/`.

## 12. Tiêu chí hoàn thành

- Người dùng đăng nhập và quên mật khẩu mà không nhập mã doanh nghiệp.
- Database thực thi quy tắc một email cho một doanh nghiệp.
- Người dùng hiện có đăng nhập Google được liên kết an toàn theo email đã xác minh.
- Người dùng Google mới hoàn thành onboarding doanh nghiệp mà không đặt mật khẩu.
- Không có tenant/user rác khi onboarding bị hủy hoặc thất bại.
- Cookie/JWT, tenant isolation, RBAC, refresh/reset token và password login hiện tại không bị hồi quy.
- Google có thể tắt bằng cấu hình mà không ảnh hưởng khả năng khởi động hoặc đăng nhập mật khẩu.
