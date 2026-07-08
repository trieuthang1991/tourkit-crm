# TourKit Web

Giao diện quản trị (SPA) cho TourKit — React 18 + Vite + TypeScript + Ant Design 5.

## Yêu cầu

- Node.js 18+ và npm.
- Backend TourKit.Api đang chạy (mặc định `http://localhost:5075`). API đã bật CORS cho origin Vite (`http://localhost:5173`, `http://localhost:4173`); prod cấu hình thêm qua `Cors:Origins` trong `appsettings`.

## Chạy

```bash
npm install
npm run dev        # dev server tại http://localhost:5173
npm run build      # type-check + build production vào dist/
npm run preview    # xem thử bản build tại http://localhost:4173
npm run test       # chạy unit test (Vitest) một lần
npm run test:watch # Vitest watch
npm run lint       # ESLint
```

### Cấu hình API base

Trỏ tới backend khác qua biến môi trường (file `.env` hoặc `.env.local`):

```
VITE_API_BASE=http://localhost:5075
```

Mặc định là `http://localhost:5075` nếu không đặt.

### Đăng nhập / đăng ký

Chưa có seed người dùng dev. Vào `/register` để tạo công ty (tenant) + tài khoản admin đầu tiên, sau đó đăng nhập tại `/login` với `tenantSlug` + email + mật khẩu vừa tạo.

## Kiến trúc (quy ước cho module mới)

Feature-sliced: mỗi module backend = một thư mục `src/features/<module>`.

- **`shared/api/httpClient.ts`** — axios instance, tự gắn Bearer token, tự đẩy về `/login` khi 401.
- **`shared/api/paged.ts`** — `pagedSchema(itemSchema)` cho envelope `{items,total,page,size}`. Các endpoint list trả `Paged<T>`; một số list con trả mảng trần (parse bằng `z.array(...)`).
- **`shared/auth/jwt.ts`** + **`features/auth/AuthContext.tsx`** — giải mã claim `perm` trong JWT; dùng `useAuth().has('perm.code')` để gate UI.
- **`shared/ui/useCrudResource.ts`** — `makeCrud({key,basePath,itemSchema,getId})` sinh sẵn hook `useList/useCreate/useUpdate/useRemove` (React Query). `useUpdate` KHÔNG parse body vì PUT trả 204.
- **`shared/ui/ResourcePage.tsx`** — trang list + thêm/sửa/xoá cấu hình bằng props; `useCreate/useUpdate/useRemove` đều optional (ẩn nút tương ứng khi thiếu). Form dùng `CrudFormModal` (RHF + Zod) + các field trong `shared/ui/Field.tsx`.
- **`shared/format.ts`** — `money()` (vi-VN), `dateText()`, `statusText(map,code)`.
- **`shared/api/problem.ts`** — `errorMessage(e)` rút thông điệp từ ProblemDetails (RFC 7807) cho toast lỗi.

### Thêm một module CRUD mới

1. `types.ts` — Zod schema cho response + form (nullable text: `.nullable().transform(v => v || null)`).
2. `<x>Crud.ts` — `makeCrud<Item, Form, Form>({...})`.
3. `<X>Page.tsx` — dùng `ResourcePage` với `columns`, `perms`, `toForm`, `formModal`.
4. `types.test.ts` — test parse schema.
5. Thêm route trong `app/router.tsx` và (nếu là mục menu) một `NavItem` trong `app/AppShell.tsx` kèm `perm` tương ứng.

Các flow không phải CRUD thuần (đặt chỗ, phiếu thu, duyệt phân cấp, chia lợi nhuận) là component bespoke (`useState` + hook React Query riêng), mount vào trang chi tiết đơn.
