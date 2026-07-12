---
name: external-providers-gateway
description: Tích hợp NCC ngoài (SMS/Zalo/Bank/OCR/CRM) phải qua 1 API Gateway chung, KHÔNG viết client rời rạc
metadata:
  type: project
  originSessionId: 4a96fa90-2f6b-4adb-9b7f-e76d2a5b864b
---

Quyết định của chủ dự án (2026-07-12): các tích hợp nhà cung cấp NGOÀI —
SMS (eSMS.vn), Zalo (ZNS official), đối soát ngân hàng (Casso/VietQR), OCR hộ chiếu (FPT.AI),
CRM/eTour — **KHÔNG build client .NET rời rạc từng cái trong app**. Thay vào đó gom về **MỘT
API Gateway xử lý chung 1 chỗ** ("làm API Gateway để xử lý chung 1 chỗ").

**Why:** tránh mỗi provider một client rải rác trong Infrastructure; một cổng trung tâm dễ quản
credential, retry, logging, đổi NCC, versioning API.

**How to apply:**
- KHI làm phần này: thiết kế/hiện thực API Gateway (service ngoài hoặc layer riêng) là điểm vào
  duy nhất cho mọi call NCC; app TourKit gọi Gateway qua các seam sẵn có (`ISmsSender`/`IZaloSender`/
  `IEmailSender` + seam mới cho OCR/Bank) — mỗi seam trỏ tới 1 client "GatewayClient" gọi Gateway,
  KHÔNG gọi thẳng NCC.
- Seam abstraction đã sẵn trong app (dev = Log stub). Chỉ thêm implementation "gọi Gateway" khi
  Gateway sẵn sàng + có endpoint/credential.
- NCC đã chốt (chờ key, chủ dự án gửi sau): SMS=eSMS.vn, Zalo=ZNS official, Bank=Casso/VietQR,
  OCR hộ chiếu=FPT.AI. (Đã hỏi & xác nhận qua AskUserQuestion.)
- ĐÃ HUỶ hướng cũ (build EsmsSender/ZaloZnsSender/FptAiPassportOcr/CassoGateway rời) — nhánh
  feat/external-providers đã xoá, chưa commit gì.

Liên quan: [[roadmap-status]] (nhóm external còn lại), [[no-tables-for-tooling-data]].
