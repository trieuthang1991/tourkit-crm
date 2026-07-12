---
name: business-logic-follow-old-project
description: Nghiệp vụ tour phải bám hệ cũ để không sai logic
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 4a96fa90-2f6b-4adb-9b7f-e76d2a5b864b
---

Khi làm nghiệp vụ (tour, booking, pricing, phụ thu, trẻ em/em bé, ghế, công nợ, hoa hồng...) PHẢI tham chiếu hệ cũ, không tự chế logic.

**Why:** Nghiệp vụ tour VN phức tạp; tự suy diễn dễ sai. Hệ cũ là chuẩn nghiệp vụ đã chạy thực tế.

**How to apply:** Nguồn tham chiếu: `script.sql` (144 bảng, trong repo CRM) và repo code hệ cũ trong GitNexus (`tourkit` @ migroup-vn, hoặc `toutkit-app` — xác nhận với user repo/branch nào là "đủ" trước khi index; user từng nói lấy bản mới nhất, nhưng branch `main` không tồn tại ở repo cũ, chúng dùng `master`/`develop`). Trước khi thiết kế/sửa logic nghiệp vụ, đối chiếu quy tắc từ hệ cũ. Xem [[project-layout-and-tooling]].
