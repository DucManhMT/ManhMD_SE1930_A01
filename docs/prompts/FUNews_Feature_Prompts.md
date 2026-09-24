# FUNews prompts cho từng giai đoạn

## Bootstrap

```text
Đọc AGENTS.md và README_AI.md. Triển khai FUN-001 trong repository hiện tại.
StudentName: [ĐIỀN]
ClassCode: [ĐIỀN]
Kiểm tra cấu trúc hiện có trước; chỉ tạo project thiếu. Tạo hai solution .sln
BE/FE, net8.0, đúng 3 tầng và project references không vòng.
Chưa triển khai hàng loạt nghiệp vụ. Build hai solution và ghi evidence.
```

## Review một task

```text
Review diff cho [FUN-xxx] theo card, AGENTS.md, Requirements, Decisions,
Database_API_Contract và Design Standard. Kiểm tra code thật và test hiện có.
Ưu tiên: sai quyền/Inactive leak/OData/password, mất dữ liệu/cascade/transaction,
sai nghiệp vụ hoặc schema, lỗi tích hợp FE/BE, modal/validation và accessibility.
Không sửa code ở lượt review này. Báo từng finding có mức độ, file/vị trí,
đường tái hiện và tác động. Phân biệt lỗi đã xác minh và rủi ro chưa kiểm tra.
Đối chiếu từng acceptance criterion: PASS/FAIL/UNVERIFIED cùng evidence.
Nếu không có lỗi, nói rõ phạm vi đã kiểm tra và khoảng trống còn lại.
```

## Sửa lỗi sau review

```text
Sửa các finding đã xác nhận cho [FUN-xxx]: [DÁN FINDINGS].
Đọc code/schema liên quan, tìm nguyên nhân trước khi sửa. Giữ phạm vi nhỏ,
không rewrite kiến trúc hoặc xóa validation để test pass. Bổ sung regression
check cho lỗi nghiệp vụ/quyền/dữ liệu và chạy lại flow bị ảnh hưởng.
Báo nguyên nhân, file đã sửa, test thực tế, finding còn mở.
```

## Tiếp tục phiên mới

```text
Đọc AGENTS.md, README_AI.md và docs/tasks/FUNews_Tasks.md.
Kiểm tra git diff và evidence hiện có. Tóm tắt task đang dở, blocker và
bước tiếp theo; không coi TODO là đã làm hoặc làm lại code đã hoàn thành.
Tiếp tục [FUN-xxx] theo master prompt, giữ thay đổi đang có.
```

## Nghiệm thu cuối

```text
Thực hiện FUN-020 rồi FUN-021 theo thứ tự, mỗi task có báo cáo riêng.
Đối chiếu R01–R19 và docs/testing/FUNews_Test_Plan.md với implementation.
Chạy BE, FE và SQL dev/test theo README. Không reset database có dữ liệu thật.
Kiểm tra đường truy cập API trực tiếp, OData bypass, FK delete, transaction,
report date boundary, modal và responsive. Không cài thêm feature ngoài đề.
Tạo README chạy dự án, accounts demo, API overview, evidence/screenshots thật.
Báo rõ từng phần FAIL hoặc UNVERIFIED; không kết luận sẵn sàng nộp nếu gate còn thiếu.
```
