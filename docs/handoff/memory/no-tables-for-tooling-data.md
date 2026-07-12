---
name: no-tables-for-tooling-data
description: "Không tạo DB table/schema để lưu dữ liệu công cụ (GitNexus graph, Memory Compiler, v.v.)"
metadata: 
  node_type: memory
  type: feedback
  originSessionId: 4a96fa90-2f6b-4adb-9b7f-e76d2a5b864b
---

Không được tạo bảng database, schema, hay migration để lưu dữ liệu của công cụ phụ trợ (GitNexus knowledge graph, Claude Memory Compiler, hoặc bất kỳ tooling nào). Các phần này phải giữ **file-based, nằm ngoài database của CRM**.

**Why:** User muốn database `tourkit-crm` chỉ chứa dữ liệu nghiệp vụ thật, không bị lẫn dữ liệu công cụ/metadata.

**How to apply:** GitNexus lưu ở `.gitnexus/` (embedded, gitignored); Memory Compiler lưu markdown ở `D:\MiGroup\tools\claude-memory-compiler`. Khi đề xuất giải pháp lưu trữ cho tooling, mặc định dùng file, KHÔNG thêm table/EF entity/migration vào CRM. Xem [[project-layout-and-tooling]].
