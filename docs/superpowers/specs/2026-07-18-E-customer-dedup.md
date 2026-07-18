# Item E — Customer dedup (rà khách trùng)

**Ngày:** 2026-07-18 · **Effort:** M · **Trạng thái:** ✅ (dedup) · phần còn lại tách slice

## Phạm vi (PM tự quyết)
Item E gốc = dedup + auto-chia lead + import + customer-360 (4 mảng lớn). Làm mảng **đúng-chắc, giá trị cao nhất trước: phát hiện khách trùng**. Auto-chia lead (đã có màn "Chia số Sale"/lead-campaigns), import, và 360 → slice riêng.

## Thiết kế
- `CustomerService.FindDuplicatesAsync()`: gom theo **SĐT chuẩn hoá** (bỏ ký tự không số; tiền tố `84`→`0` để bắt `0901…` vs `+84901…`) và **email** (thường + trim). Chỉ trả nhóm ≥2 khách. Sắp theo số lượng giảm dần.
- DTO `DuplicateGroupDto(MatchType, MatchKey, Customers[])` + `DuplicateCustomerDto`.
- Endpoint `GET /customers/duplicates` [CustomerView].
- FE: `CustomerDuplicatesPage` (nhóm thẻ theo khoá + bảng khách) + route + menu "Rà khách trùng".
- Test: gom SĐT đa định dạng + email hoa/thường; bỏ khách không SĐT/email. 462 PASS.

## Còn lại E (tách slice, PM quyết)
Auto-chia lead round-robin (kiểm chứng lead-campaigns hiện có tới đâu) · Import CSV khách · Customer-360 (gộp lịch sử đơn/chăm sóc/công nợ) · Gộp bản ghi trùng (merge — cần quyết chính sách gộp field).
