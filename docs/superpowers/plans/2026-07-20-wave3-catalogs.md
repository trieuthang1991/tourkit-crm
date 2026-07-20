# Wave 3 — Danh mục & Cấu hình hàng loạt (plan chi tiết)

> ~27 màn S nhân bản khung Wave 0. Có thể chạy **song song** Wave 1/2 (khi rảnh) hoặc giao subagent hàng loạt — mỗi màn độc lập, rẻ. KHÔNG cần soi hệ cũ sâu (CRUD danh mục thuần), chỉ đối chiếu nhãn cột.

## Khuôn mỗi màn (lặp lại cho cả danh sách)
1. PageModel kế thừa `TkListPageModel` + inject service Application có sẵn.
2. `Index.cshtml`: `_TkTable` (cột theo DTO) + `_TkOffcanvas` (form theo Create/UpdateDto) — mục tiêu ≤150 dòng tổng.
3. `[Authorize(Policy=...)]` đúng perm; nối leaf menu/ConfigHub; verify Chrome nhanh (CRUD 1 vòng); commit theo cụm 3-5 màn.

## Danh sách màn (route → service → leaf menu)
| # | Route | Service | Ghi chú |
|---|---|---|---|
| 1 | /CustomerTypes | ICustomerTypeService | config |
| 2 | /CustomerSources | ICustomerSourceService | config |
| 3 | /CustomerTags | ICustomerTagService | config (cột màu Color — input color) |
| 4 | /MarketTypes | IMarketTypeService | config (ParentId — select cha) |
| 5 | /CarTypes | ICarTypeService | config |
| 6 | /LanguageTypes | ILanguageTypeService | config |
| 7 | /Currencies | ICurrencyService | config (RateToVnd 18,4) |
| 8 | /PaymentAccounts | IPaymentAccountService | config (IsDefault toggle) |
| 9 | /PaymentTerms | IPaymentTermService | p-terms |
| 10 | /RoomClasses | IRoomClassService | b-roomclass |
| 11 | /Surcharges | ISurchargeService | config |
| 12 | /Departments | IDepartmentService | config |
| 13 | /Positions | IPositionService | config |
| 14 | /MessageTemplates | IMessageTemplateService | config (textarea nội dung) |
| 15 | /TransferReasons | ITransferReasonService | config |
| 16 | /CustomerCommissionRules | ICustomerCommissionRuleService | hh-customer (% theo loại khách) |
| 17 | /PostCategories | IPostCategoryService | mkt-postcat |
| 18 | /TourGroups | ITourGroupService | dropdown Orders (màn quản lý riêng) |
| 19 | /Branches | IBranchService | dropdown nhiều màn |
| 20 | /Vehicles | IVehicleService | v-store (nếu chưa làm ở Wave 2) |
| 21 | /TourRatings | ITourRatingService | fb-general/fb-tour (list + trả lời) |
| 22 | /Notifications | INotificationService | w-noti (list + đánh dấu đã đọc — read-only) |
| 23 | /ActivityLogs | IActivityLogService | log-system (read-only + filter entity/ngày) |
| 24 | /TourTemplates | ITourTemplateService | list mẫu tour (detail builder để Wave 5) |
| 25 | /CompanyProfile | ICompanyProfileService | form đơn (không list) |
| 26 | /ConfigHub | — | trang hub: grid card link tới 1-25 (icon + mô tả) |
| 27 | /ApprovalProcesses | IApprovalProcessService | cấu hình bước duyệt (form bước + user — M nhẹ, làm cuối wave) |

## Nghiệm thu wave
- 100% leaf "config" trong menu hết trỏ `#`; ConfigHub thành cổng danh mục.
- Spot-check 5 màn ngẫu nhiên: quy ước form (placeholder/*/validate/flatpickr/Swal) đầy đủ.
- Test xanh; ước lượng: nếu 1 màn >150 dòng hoặc >30 phút → khung Wave 0 thiếu gì đó, dừng và vá khung trước.
