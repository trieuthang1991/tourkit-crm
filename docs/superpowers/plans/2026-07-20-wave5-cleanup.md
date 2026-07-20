# Wave 5 — Phụ trợ, B2B & Dọn dẹp kết thúc migration (plan chi tiết)

> Điều kiện: Wave 1-4 xong. Kết thúc wave này = **xoá hẳn React**, dự án chỉ còn 1 UI.

## 5.1 Marketing `/Marketing` — M
Chiến dịch email (list + form + modal gửi SendCampaignModal → MarketingController; SMTP/Log sender đã có) + kho mẫu (MessageTemplates đã làm Wave 3, link sang).
**Nghiệm thu:** tạo chiến dịch gửi 2 khách (LogEmailSender ghi log đúng).

## 5.2 Bài viết `/Posts` — M
List + editor bài (textarea/markdown đơn giản — KHÔNG rich editor nặng trừ khi hệ cũ bắt buộc) + bình luận (PostComments panel) + chuyên mục (Wave 3).
**Nghiệm thu:** đăng bài có chuyên mục, bình luận hiện ở Workspace widget.

## 5.3 B2B Đại lý: `/Agents` (M) + `/AgentBookings` (M) + `/AgentQuotes` (M)
Agents CRUD + mã TA; đặt chỗ đại lý (list + duyệt); yêu cầu báo giá B2B (list + trả giá → convert Quote Wave 1.5).
**Nghiệm thu:** luồng: agent gửi yêu cầu → trả báo giá → thành đặt chỗ.

## 5.4 Tour Template builder `/TourTemplates/Details/{id}` — **L**
Soi React `tourTemplates/TourTemplateDetailPage` (đa tab: thông tin + lịch trình itinerary theo ngày + dịch vụ + phân công TourAssignees). Pills detail + bảng dòng itinerary.
**Nghiệm thu:** dựng mẫu tour 3N2Đ đủ lịch trình; tạo chuyến từ mẫu (Wave 1.8) kế thừa đúng.

## 5.5 Public pages: Landing `/GioiThieu` + Registration `/Register`
Landing giới thiệu (đã có bản Vuexy từ pivot theme — rà lại nội dung); form đăng ký tenant (RegistrationController — luồng provisioning đã có, form theo UI-CONVENTIONS, AllowAnonymous).
**Nghiệm thu:** đăng ký công ty mới → login được tenant mới (luồng memory `db-switch`: demo qua /registration).

## 5.6 Quyết định gap chưa từng có màn (hỏi user trước khi làm)
- Mạng Nội Bộ (w-social), Zalo OA/ZNS/UID (3 leaf), Feedback ZNS: **mặc định GIỮ leaf menu trỏ trang "đang xây dựng"** — chỉ làm khi user yêu cầu (cần tích hợp ngoài).

## 5.7 Dọn dẹp kết thúc migration (theo thứ tự, cẩn trọng)
1. Rà chéo: mọi leaf `_MenuData.cs` (trừ 5.6) trỏ route thật; click-through toàn menu bằng Chrome.
2. Gỡ `web/` React: xoá thư mục + CORS `Cors:Origins`/`UseCors("web")` trong Program.cs + script build FE trong CI nếu có. Chạy `gitnexus_impact` trước; giữ 1 tag git trước khi xoá để quay lại được.
3. Xoá trang `Ping` smoke (hoặc giữ làm healthcheck — đổi thành `/healthz` chuẩn).
4. Nợ DB còn lại (Master Plan A): H4/M1/M2 — chuẩn hoá FK (CreatedByUserId, CustomerAssignee, Source/Tag theo Id) = **đợt migration data riêng** sau khi UI ổn định; L2 FilterOptions distinct SQL; funnel tối ưu khi màn nào dùng.
5. Quét chất lượng cuối: `/security-review` + spot-check UI-CONVENTIONS 10 màn ngẫu nhiên + full test + cập nhật README/memory.

**Định nghĩa HOÀN THÀNH migration:** repo không còn `web/`; 100% nghiệp vụ vận hành trên Razor Pages; 1 deployable duy nhất; test xanh; tài liệu (`UI-CONVENTIONS`, `MASTER-PLAN`, memory) phản ánh đúng trạng thái cuối.
