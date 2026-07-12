---
name: project-layout-and-tooling
description: tourkit-crm nested path; main is the real advanced branch; GitNexus + memory-compiler layout
metadata: 
  node_type: memory
  type: project
  originSessionId: 4a96fa90-2f6b-4adb-9b7f-e76d2a5b864b
---

**Đường dẫn thật:** Session working dir là `D:\MiGroup\AI\tourkit-crm` (thư mục NGOÀI, gần rỗng, KHÔNG phải git repo). Project .NET thật + git repo ở thư mục lồng `D:\MiGroup\AI\tourkit-crm\tourkit-crm\`. Mọi lệnh git/dotnet/read file phải trỏ vào lớp lồng.

**Nhánh đúng là `main`** (remote `trieuthang1991/tourkit-crm`). Nhánh `feat/phase-0a-multitenant` đã LỖI THỜI/SAI (0 commit vượt main) — đã xoá local. Remote branch feat vẫn còn và đang là **default branch của repo trên GitHub**; muốn xoá phải đổi default sang `main` trước (thao tác setting dùng chung — cần user tự làm hoặc cấp quyền).

**Main là codebase gần hoàn chỉnh, KHÔNG phải mới "Phase 0a":** 509 file, kiến trúc RESTful phân tầng qua Controllers (đã bỏ CQRS/kernel). Module đã có: Booking, Finance, Catalog, Providers, Auth, Commission, Billing, Marketing, Reports, Crm, Tenancy, Persistence, Provisioning. → Đừng lập kế hoạch "xây Auth/Phase 0b" trước khi quét lại main; nhiều thứ đã tồn tại.

**Memory Compiler:** main có bản CHÍNH CHỦ commit sẵn tại `.claude/memory` (hook trong `.claude/settings.json` của repo, dùng `uv run --directory .claude/memory`). Bản tôi cài tạm phiên trước ở `D:\MiGroup\tools` + settings.json thư mục ngoài đã bị GỠ để tránh trùng.

**GitNexus:** đã re-index main (3438 nodes / 509 files). Index cũ 149 node là của nhánh feat, bỏ. Xem [[no-tables-for-tooling-data]], [[business-logic-follow-old-project]].
