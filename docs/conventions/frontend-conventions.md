# TourKit — Frontend Conventions (React + TypeScript)

> Mục tiêu: UI nhất quán, gõ code giống nhau, không lộn xộn. Đây là luật, ép bằng ESLint/Prettier/TypeScript strict (build/CI fail nếu vi phạm).

Gắn với spec `docs/superpowers/specs/2026-07-07-tourkit-saas-platform-design.md`.

---

## 0. Stack chuẩn (đã chốt — không tự ý thay)

| Vai trò | Lựa chọn | Ghi chú |
|---------|----------|---------|
| Build | **Vite** + React 18 + **TypeScript (strict)** | |
| UI library | **Ant Design (antd)** | Table/Form/DatePicker giàu sẵn, hợp admin/ERP |
| Server state | **TanStack Query** (React Query) | Không nhét data server vào Redux |
| Client state | React state / Context; **Zustand** nếu thực sự cần global | Không thêm Redux nếu chưa cần |
| Routing | **React Router** | |
| Form | **react-hook-form** + **zod** | zod schema = nguồn chân lý validate |
| HTTP | **axios** instance tập trung (interceptor gắn JWT) | Không gọi `fetch` rải rác |
| i18n | **react-i18next** | Sản phẩm tiếng Việt; không hardcode chuỗi hiển thị |
| Format/lint | **Prettier** + **ESLint** (typescript-eslint) | CI fail nếu vi phạm |

---

## 1. Cấu trúc thư mục — theo TÍNH NĂNG, không theo loại file

Cùng một tính năng thì để cùng chỗ (component + hook + API + type). Không chia `components/`, `hooks/`, `services/` toàn cục rồi rải một tính năng ra 4 nơi.

```
web/src/
  app/
    router.tsx           # khai báo route
    queryClient.ts       # cấu hình TanStack Query
    providers.tsx        # gộp providers (Antd ConfigProvider, Query, i18n, Auth)
  shared/                # dùng chung nhiều feature
    api/
      httpClient.ts      # axios instance + interceptor JWT + xử lý 401
      types.ts           # kiểu chung (Paged<T>, ProblemDetails)
    ui/                  # component tái sử dụng bọc antd (PageHeader, DataTable...)
    hooks/
    lib/                 # tiện ích thuần (formatMoney, formatDate)
  features/
    bookings/
      api/
        bookingApi.ts      # hàm gọi API + query keys
        bookingHooks.ts    # useBookings(), useCreateBooking()
      components/
        BookingTable.tsx
        BookingForm.tsx
      types.ts             # Booking, CreateBookingRequest (khớp DTO backend)
      BookingListPage.tsx
    customers/
    auth/
  main.tsx
```

**Luật:** một feature không import trực tiếp file nội bộ của feature khác. Cần chia sẻ → nâng lên `shared/`.

---

## 2. Naming

| Đối tượng | Quy tắc | Ví dụ |
|-----------|---------|-------|
| Component & file component | `PascalCase.tsx` | `BookingTable.tsx` |
| Hook | `useCamelCase.ts` | `useBookings.ts` |
| Biến, hàm thường | `camelCase` | `totalAmount`, `formatMoney` |
| Type / interface | `PascalCase`, không tiền tố `I` | `Booking`, `CreateBookingRequest` |
| Hằng số toàn cục | `UPPER_SNAKE_CASE` | `PAGE_SIZE` |
| File không phải component | `camelCase.ts` | `bookingApi.ts` |
| Query keys | mảng có tiền tố feature | `['bookings', 'list', params]` |

- Tên tiếng Anh cho code; **chuỗi hiển thị luôn qua i18n** (`t('booking.create')`), không hardcode "Tạo đơn".

---

## 3. TypeScript (strict — ép bởi tsconfig)

- `"strict": true`, `"noUncheckedIndexedAccess": true`, `"noImplicitAny": true`.
- **Cấm `any`.** Không rõ thì `unknown` + thu hẹp kiểu. `@typescript-eslint/no-explicit-any` = error.
- **Không `as` ép kiểu bừa.** Ưu tiên type guard / zod parse.
- Kiểu request/response **khớp DTO backend**; validate biên bằng zod trước khi tin dữ liệu.
- Ưu tiên `type` cho model dữ liệu; `interface` cho props component (thống nhất, đừng trộn tùy hứng — chọn `type` mặc định).

---

## 4. Component

- **Function component + hooks.** Không class component.
- **Nhỏ và một trách nhiệm.** File > ~200 dòng hoặc component làm quá nhiều việc → tách.
- Tách **container (data) vs presentational (UI)**: page/hook lấy dữ liệu, component con nhận props thuần → dễ test, dễ tái dùng.
- Props có type tường minh. Không `props: any`.
- Không logic nghiệp vụ nặng trong JSX; đưa vào hook/hàm thuần ở `lib/`.
- Key trong list là id ổn định, **không dùng index**.
- Side effect chỉ trong `useEffect`/event handler, dependency array đầy đủ (`react-hooks/exhaustive-deps` = error).

---

## 5. Gọi API & server state

- **Mọi request qua `httpClient` (axios) tập trung.** Interceptor tự gắn `Authorization: Bearer` và xử lý 401 (refresh/logout). Không rải `axios.get` khắp nơi.
- **Đọc dữ liệu server = TanStack Query** (`useQuery`), ghi = `useMutation` + invalidate query liên quan. Không tự quản loading/error bằng `useState` cho data server.
- Query keys tập trung theo feature để invalidate nhất quán.
- Loading/error/empty là **trạng thái bắt buộc** cho mọi màn có data (skeleton của antd + thông báo lỗi). Không để màn trắng.
- **Không tự lọc tenant ở frontend.** Backend đã cô lập theo JWT; frontend chỉ hiển thị.

Ví dụ hook chuẩn:

```ts
// features/bookings/api/bookingHooks.ts
export const bookingKeys = {
  all: ['bookings'] as const,
  list: (params: BookingQuery) => [...bookingKeys.all, 'list', params] as const,
};

export function useBookings(params: BookingQuery) {
  return useQuery({
    queryKey: bookingKeys.list(params),
    queryFn: () => bookingApi.list(params),
  });
}

export function useCreateBooking() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: bookingApi.create,
    onSuccess: () => qc.invalidateQueries({ queryKey: bookingKeys.all }),
  });
}
```

---

## 6. Form

- **react-hook-form + zod** cho mọi form. Một zod schema/form, dùng chung cho validate và suy ra kiểu (`z.infer`).
- Dùng antd `Form`/`Input` nối qua `Controller` khi cần, nhưng validate là zod (một nguồn chân lý).
- Hiện lỗi field rõ ràng; disable nút submit khi đang gửi.

---

## 7. Styling

- **Ưu tiên component & token của antd.** Theme (màu, spacing) đặt tập trung qua `ConfigProvider` theme token — không sửa CSS lẻ tẻ mỗi nơi.
- CSS thêm: CSS Modules cho style cục bộ. **Không** style inline rải rác, **không** file CSS global khổng lồ.
- Không "magic number" cho spacing/màu; dùng token theme.

---

## 8. i18n

- Mọi chuỗi hiển thị qua `t('key')`; namespace theo feature (`bookings.json`, `common.json`).
- Không nối chuỗi thủ công cho câu có biến — dùng interpolation của i18next.
- Ngôn ngữ mặc định `vi`; cấu trúc sẵn để thêm `en`.

---

## 9. Enforcement (tự động)

- `tsconfig.json`: `strict` + `noUncheckedIndexedAccess`.
- **ESLint** (typescript-eslint, eslint-plugin-react-hooks, import) — các rule then chốt là `error`:
  - `@typescript-eslint/no-explicit-any`
  - `react-hooks/rules-of-hooks`, `react-hooks/exhaustive-deps`
  - `no-restricted-imports` cấm import chéo nội bộ feature
- **Prettier** cấu hình chung; `eslint-config-prettier` để không xung đột.
- **Husky + lint-staged**: pre-commit chạy `eslint --fix` + `prettier` + `tsc --noEmit` trên file staged. Commit fail nếu còn lỗi.
- CI chạy `tsc --noEmit`, `eslint`, `prettier --check`, `vitest` — đỏ thì chặn merge.
- Test UI: **Vitest + React Testing Library** cho component có logic; test hành vi, không test chi tiết render.
