# Quy Chuẩn Git Commit (Git Commit Convention) — PRN232 A02

Quy chuẩn đặt tên Git Commit dựa trên chuẩn quốc tế **Conventional Commits (v1.0.0)**, được tùy biến tối ưu cho hệ thống phân tán 4 Solutions và 17 Task Cards của **FUNews Assignment 02**.

---

## 1. Cấu Trúc Tổng Quát (Commit Message Format)

```text
<type>(<scope>): <mô tả ngắn bằng tiếng Anh hoặc tiếng Việt> [#<task-id>]

[optional body: mô tả chi tiết những gì đã làm, nguyên nhân và tác động]

[optional footer: ghi chú breaking changes hoặc test cases liên quan]
```

### Ví dụ tiêu chuẩn:
```text
feat(core): implement refresh token rotation flow [#A02-003]

- Add RefreshToken entity and EF Core configuration
- Implement POST /api/auth/refresh with token family revocation
- Verified with Test Case T05, T06
```

---

## 2. Danh Mục Các `type` (Commit Types)

| Type | Ý nghĩa & Khi nào sử dụng | Ví dụ |
|:---|:---|:---|
| `feat` | Thêm mới một tính năng hoặc endpoint mới | `feat(ai): add keyword extraction engine [#A02-010]` |
| `fix` | Sửa lỗi (bug fix) trong logic, API hoặc giao diện | `fix(fe): handle expired token in DelegatingHandler [#A02-004]` |
| `docs` | Thêm hoặc cập nhật tài liệu (Markdown, README, API docs) | `docs(kit): update API contract for analytics export [#A02-009]` |
| `refactor`| Tái cấu trúc mã nguồn (không đổi tính năng bên ngoài) | `refactor(core): streamline category deletion guard [#A02-006]` |
| `perf` | Cải thiện hiệu năng (query optimization, caching) | `perf(fe): cache public articles in memory snapshot [#A02-012]` |
| `test` | Thêm mới hoặc sửa các bài kiểm thử (xUnit, Integration) | `test(core): add integration tests for audit log [#A02-006]` |
| `chore` | Các công việc bảo trì (cập nhật package, config, gitignore) | `chore(sln): add ClosedXML package to Analytics API [#A02-009]` |
| `style` | Thay đổi về format code, khoảng trắng, CSS styling | `style(fe): polish responsive layout for news detail [#A02-016]` |

---

## 3. Danh Mục Các `scope` (Phạm Vi Thay Đổi)

Gắn `scope` tương ứng với 4 Solution hoặc thành phần kỹ thuật bị ảnh hưởng:

| Scope | Đại diện cho thành phần | Áp dụng khi thay đổi |
|:---|:---|:---|
| `core` | `FUNewsManagement_CoreAPI` | Controller, Services, Repositories, DbContext của Core API |
| `analytics`| `FUNewsManagement_AnalyticsAPI` | Dashboard, Trending, Recommend, Export Excel |
| `ai` | `FUNewsManagement_AIAPI` | Tag Suggestion, Learning Cache |
| `fe` | `FUNewsManagement_FE` | Razor Pages, ViewModels, HttpClients, Modals |
| `signalr` | SignalR Notification System | Hub, Realtime Toast, WebSocket client |
| `db` | Database & Migration Scripts | File SQL, bản vá schema, migration EF |
| `worker` | Background Worker HostedService | Caching worker 6h |
| `docs` | Thư mục tài liệu | `docs/a02/`, `README.md`, `RUNBOOK.md` |

---

## 4. Bảng Mẫu Commit Gợi Ý Cho Từng Task (Task-by-Task Cheat Sheet)

Khi bạn thực hiện từ Task `A02-001` đến `A02-017`, hãy dùng đúng các mẫu commit sau:

| Task ID | Gợi ý Commit Message |
|:---:|:---|
| **A02-001** | `docs(baseline): complete A01 source audit and reuse gap matrix [#A02-001]` |
| **A02-002** | `chore(sln): scaffold 4 independent .NET 8 solutions [#A02-002]` |
| **A02-003** | `feat(core): implement JWT authentication & refresh token flow [#A02-003]` |
| **A02-004** | `feat(fe): configure 3 Typed HttpClients with DelegatingHandler & Polly [#A02-004]` |
| **A02-005** | `feat(db): apply A02 database migration schema patch [#A02-005]` |
| **A02-006** | `feat(core): implement audit logging with before/after JSON & integrity guards [#A02-006]` |
| **A02-007** | `feat(media): implement article image upload and validation [#A02-007]` |
| **A02-008** | `feat(analytics): implement dashboard metrics, trending and recommendations [#A02-008]` |
| **A02-009** | `feat(fe): render Chart.js graphs and enable Excel report export [#A02-009]` |
| **A02-010** | `feat(ai): build tag suggestion engine and learning cache store [#A02-010]` |
| **A02-011** | `feat(fe): integrate AI tag suggestion chips and feedback loop [#A02-011]` |
| **A02-012** | `feat(worker): build 6-hour cache refresh BackgroundService [#A02-012]` |
| **A02-013** | `feat(fe): implement offline mode with banner and cached fallback [#A02-013]` |
| **A02-014** | `feat(admin): build audit log viewer modal and account role filters [#A02-014]` |
| **A02-015** | `feat(signalr): implement real-time notification hub with toast and bell count [#A02-015]` |
| **A02-016** | `feat(ux): standardize Bootstrap Modals, loading indicators and accessibility [#A02-016]` |
| **A02-017** | `docs(release): finalize submission bundle, SQL script and README [#A02-017]` |

---

## 5. Quy Tắc Cần Nhớ (Rules of Thumb)

1. **Động từ thể hiện hành động (Imperative mood):** Dùng `implement`, `add`, `fix`, `update` (không dùng `implemented`, `fixing`, `added`).
2. **Ngắn gọn, rõ ràng:** Dòng đầu tiên (Title) tối đa 72 ký tự.
3. **Luôn gắn Task ID:** Cuối tiêu đề commit luôn đính kèm `[#A02-xxx]` để dễ dàng truy vết tiến độ trên git graph.
4. **Không commit mã rác:** Không commit các thư mục `bin/`, `obj/`, `.vs/`, `.idea/`, file `.env` hoặc file token bí mật.
