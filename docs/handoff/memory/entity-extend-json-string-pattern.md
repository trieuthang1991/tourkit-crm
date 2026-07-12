---
name: entity-extend-json-string-pattern
description: Khi mở rộng entity bám hệ cũ — dùng cột JSON cho field mềm/list + ID kiểu STRING (migrate được legacy)
metadata:
  type: feedback
  originSessionId: 4a96fa90-2f6b-4adb-9b7f-e76d2a5b864b
---

Chủ dự án chỉ định (2026-07-12) khi mở rộng model bám hệ cũ, ÁP DỤNG CHO MỌI ENTITY sau:

**1. Cột JSON cho nhóm field mềm + list.** Gói các field CRM mềm/mở rộng + danh sách vào 1 cột JSON
(`<Entity>.CrmProfileJson` kiểu string) thay vì mỗi field một cột. Thêm field/list mới về sau KHÔNG cần
migration. Chỉ để cột RIÊNG cho field cần tìm/lọc/join (vd `Code`).

**2. ID tham chiếu = STRING, KHÔNG dùng Guid.** Vì sau này migrate dữ liệu hệ cũ sang, ID legacy KHÔNG phải
GUID → để Guid sẽ không nhét được. Lưu string (chứa cả GUID mới lẫn ID legacy). Resolve tên best-effort:
nếu string khớp User.Id.ToString() → hiện tên; không khớp (ID legacy) → GIỮ NGUYÊN chuỗi (không mất dữ liệu).

**3. Nhiều trường kiểu LIST.** Hệ cũ có field multi (vd NV phụ trách, phân nhóm/segments, thẻ). Lưu mảng
trong JSON; DTO trả mảng phẳng; FE multi-select (SelectField `mode="multiple"|"tags"`).

**Mẫu tham chiếu đã làm:** Customer (`2e1bbd8`): `CrmProfileJson` gói gender/city/marketGroup/initialNeed/
collaboratorName/campaign/createdBy(string) + segments[]/tags[]/assignedTo[](string); cột `Code` riêng
(Mã KH tự sinh). Value object `CustomerCrmProfile.Parse/ToJsonOrNull`. List enrich aggregate (số lần mua/
doanh thu/CSKH gần nhất) tính ở service, không lưu.

**How to apply:** Trước khi mở rộng 1 entity, XEM KỸ giao diện + form hệ cũ live (staging) để nhận đúng
trường nào single/multi/reference. Xem [[legacy-ui-reference]], [[business-logic-follow-old-project]].
