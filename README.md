# TourKit CRM

CRM/điều hành tour đa tenant (SaaS) — .NET 9 + EF Core 9 (backend, kiến trúc phân tầng) và React + TypeScript + Vite + Ant Design (web). Đây là **runbook vận hành**: chạy, cấu hình, triển khai. Muốn hiểu kiến trúc/nghiệp vụ, đọc [`docs/README.md`](docs/README.md) trước.

## Yêu cầu môi trường

| Thành phần | Phiên bản |
|---|---|
| .NET SDK | 9.0+ |
| Node.js | 20+ (web) |
| DB | SQLite (dev, sẵn) · SQL Server (prod) |

## Chạy nhanh (dev)

```bash
# Backend API (http://localhost:5xxx — xem launchSettings)
dotnet run --project src/TourKit.Api

# Web (http://localhost:5173)
cd web && npm install && npm run dev
```

Khi khởi động, API tự **migrate DB** (`MigrateAsync`) và **seed** permission + gói dịch vụ — dev dùng được ngay, không cần bước thủ công. DB dev mặc định là file `TourKit_Dev.db` (SQLite).

## Kiểm thử

```bash
dotnet test                       # 4 arch + 201 unit + 81 integration
cd web && npm run lint && npx tsc --noEmit && npx vitest run   # 73 test
```

## Cấu hình (`src/TourKit.Api/appsettings.json` hoặc biến môi trường)

Biến môi trường ghi đè theo quy ước .NET: `Section__Key` (2 gạch dưới), ví dụ `Email__Provider=Smtp`.

### Database
| Khoá | Ý nghĩa |
|---|---|
| `Database:Provider` | `Sqlite` (dev) hoặc `SqlServer` (prod). Code provider-agnostic. |
| `ConnectionStrings:Default` | Chuỗi kết nối. SQLite: `Data Source=...`. SqlServer: xem `SqlServerExample`. |

> Prod: đặt `Database:Provider=SqlServer` + connection string thật rồi chạy `dotnet ef database update -p src/TourKit.Infrastructure -s src/TourKit.Api`.

### Xác thực (JWT)
| Khoá | Ý nghĩa |
|---|---|
| `Jwt:Secret` | **Bắt buộc đổi ở prod** (≥32 ký tự). Khoá ký token. |
| `Jwt:Issuer` / `Jwt:Audience` | Định danh phát hành/đối tượng. |
| `Jwt:AccessTokenMinutes` / `Jwt:RefreshTokenDays` | Thời hạn token. |

### CORS
| Khoá | Ý nghĩa |
|---|---|
| `Cors:Origins` | Mảng origin của SPA. Không đặt → mặc định `http://localhost:5173`, `:4173`. Prod đặt domain web thật. |

### Email (§8) — abstraction sẵn, bật gửi thật bằng cấu hình
| Khoá | Ý nghĩa |
|---|---|
| `Email:Provider` | `Log` (dev, ghi log — **không cần credential**) hoặc `Smtp` (gửi thật). |
| `Email:Host` / `Port` / `EnableSsl` | Máy chủ SMTP. |
| `Email:User` / `Password` | Đăng nhập SMTP. |
| `Email:From` | Địa chỉ người gửi. |

> Bật gửi email thật: đặt `Email:Provider=Smtp` + điền Host/User/Password. Code gọi (`IEmailSender`) không đổi. Cùng mô hình với file storage.

### Lưu trữ tệp
| Khoá | Ý nghĩa |
|---|---|
| `FileStorage:LocalRoot` | Thư mục gốc lưu tệp đính kèm. Không đặt → `App_Data/uploads` cạnh binary. Tệp tách theo tenant. |

### Background jobs (Hangfire)
| Khoá | Ý nghĩa |
|---|---|
| `BackgroundJobs:Enabled` | `true` (mặc định) bật server + dashboard `/hangfire` (chỉ user đã đăng nhập) + job heartbeat. Tự tắt trong test. |

> Hiện dùng in-memory storage (dev). Prod nên chuyển `SqlServerStorage` để job bền qua restart.

## Triển khai prod — checklist

- [ ] `Database:Provider=SqlServer` + connection string; chạy migration.
- [ ] Đổi `Jwt:Secret` sang chuỗi bí mật thật.
- [ ] `Cors:Origins` = domain web prod.
- [ ] (Nếu cần email) `Email:Provider=Smtp` + credential.
- [ ] (Nếu cần job bền) chuyển Hangfire sang `SqlServerStorage`.
- [ ] Build web: `cd web && npm run build` → phục vụ `dist/` qua web server/CDN.

## Phần cần input để mở khoá (chưa build vì phụ thuộc ngoài)

Toàn bộ backend + frontend + hạ tầng (file/jobs/email abstraction) đã hoàn tất và test xanh. Các mục sau **cố ý chưa build** vì cần dữ liệu/quyết định từ chủ dự án (không tự bịa để tránh sai nghiệp vụ — xem [`docs/business/legacy-gap-roadmap.md`](docs/business/legacy-gap-roadmap.md)):

| Mục | Cần cung cấp |
|---|---|
| Gửi email thật | Credential SMTP (điền `Email:*`) |
| Đồng bộ CRM/eTour | Endpoint + API key + shape dữ liệu hệ CRM |
| Đối soát ngân hàng (BankHub) | Kết nối/API ngân hàng |
| Job Hangfire nghiệp vụ | Quyết định job (nhắc hạn? CSKH?) + thời điểm + người nhận |
| Cụm Báo giá/DuTru/hotel-vé-visa/hoá đơn VAT · B2B Phase 2/3 | Thiết kế nghiệp vụ (brainstorm với chủ dự án) |
