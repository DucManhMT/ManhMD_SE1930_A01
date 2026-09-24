# FUNews prompt full-stack cho Gemini Antigravity

Mỗi lần dùng một task từ `docs/tasks/FUNews_Tasks.md`. Áp dụng cho Gemini Antigravity, Codex hoặc AI có quyền đọc/sửa repository. Không cần prompt UI và backend riêng.

## Master prompt

```text
Hãy trực tiếp triển khai hoàn chỉnh task FUNewsManagement trong repository hiện tại.
Không dừng ở kế hoạch, pseudocode hoặc mockup.

TASK ID: [FUN-xxx]
StudentName / ClassCode: ManhMD / SE1930
Yêu cầu bổ sung của tôi: Không có

ĐỌC TRƯỚC
- AGENTS.md và hướng dẫn gần folder cần sửa.
- README_AI.md; docs/requirements/FUNews_Requirements.md.
- docs/architecture/FUNews_Decisions.md, FUNews_Architecture.md,
  FUNews_Database_API_Contract.md.
- docs/design/FUNews_Web_Design_Standard.md.
- Card TASK ID trong docs/tasks/FUNews_Tasks.md và test liên quan.
- Code, solution/project, route, schema/SQL, entity mappings và component thực tế.
Không giả định thứ chưa đọc đã tồn tại. Không mang stack/feature Petlify sang.

PHẠM VI
Lấy actor, route, input, dependency và acceptance criteria từ card và contract.
Một task nghiệp vụ phải có đủ UI MVC/modal, API, service, repository/DAO,
validation, quyền, dữ liệu và kiểm thử. Task nền tảng theo đầu ra riêng của card.
Nếu dependency nhỏ bắt buộc bị thiếu, hoàn thiện và báo rõ; dependency lớn chưa có
thì ghi blocker, không dùng mock để báo DONE. Không mở rộng sang task khác.

RÀNG BUỘC
- .NET 8; hai solution BE/FE; MVC + Bootstrap 5; SQL Server; OData.
- FE Fetch → MVC → client service → HttpClient → BE API; FE không truy cập DB.
- BE Controller → Service → Repository → DAO → Scoped DbContext.
- DTO allowlist; role/audit lấy server. Admin từ cấu hình không có DB AccountID.
- Public/Lecturer chỉ thấy Active cả list/detail/search/related/count.
- Cookie mutation có antiforgery; API enforce JWT/policy; không tin UI ẩn nút.
- News + NewsTag atomic; chặn delete account/category/tag khi bị tham chiếu.
- Thêm/sửa bằng modal AJAX, delete confirm, giữ input lỗi, loading/toast thật.
- UI tiếng Việt theo Design Standard; không CTA giả hoặc dữ liệu demo hardcode.
- Giữ SQL gốc; schema change thành patch có preflight và cách verify, không drop DB.
- Giữ code/thay đổi đang có của người khác. Không reset git/push/deploy tự động.

THỰC HIỆN
1. Inspect: báo ngắn hiện trạng, file tái sử dụng, dependency và assumption.
2. Implement: sửa code hoàn chỉnh theo acceptance criteria của một task.
3. Verify: review diff, build solution liên quan; test quyền, validation,
   database/transaction và UI theo rủi ro. Sửa lỗi do task trước khi kết thúc.
4. Update: cập nhật trạng thái/evidence task và tài liệu contract khi cần.
5. Report: hành vi đã làm, file chính, lệnh/test đã chạy, kết quả thật,
   UI states/viewport đã kiểm tra, schema thay đổi và phần chưa xác minh.

Không báo test/build/database/UI pass nếu chưa có bằng chứng chạy thực tế.
Không đánh DONE khi chỉ code xong nhưng chưa chạy được acceptance test bắt buộc.
```

## Ví dụ sử dụng

```text
Dùng master prompt trong docs/prompts/FUNews_Antigravity_Fullstack_Prompt.md.
Triển khai FUN-012 — Tạo bài viết và gắn tags.
Đọc card, kiểm tra FUN-011 và FUN-010 đã đủ trước khi sửa.
Làm cả modal MVC và API thật; CreatedByID lấy từ JWT, không từ form.
Kiểm thử rollback nếu một TagID không tồn tại; xác minh tác giả và ngày tạo.
Không làm duplicate hoặc upload ảnh trong task này.
```

## Tiếp tục khi AI chỉ lập kế hoạch

```text
Tiếp tục thực thi task đang làm theo master prompt: sửa code, kiểm tra và
bàn giao evidence. Giữ phạm vi task; không chỉ mô tả các bước cần làm.
```
