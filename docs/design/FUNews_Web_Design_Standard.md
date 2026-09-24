# FUNews web design standard

## Định hướng

Website tin tức trường đại học rõ ràng, gọn và dễ đọc; khu quản trị ưu tiên thao tác bảng và form. UI tiếng Việt, Bootstrap 5, system font, không thêm frontend framework. Không tạo ảnh giả, số thống kê giả hoặc chức năng không có API.

## Tokens

- Primary #1D4ED8; text #0F172A; muted #475569; background #F8FAFC; surface #FFFFFF; border #CBD5E1.
- Success #166534, danger #B91C1C; Inactive dùng text xám và nhãn chữ. Không truyền trạng thái chỉ bằng màu.
- Spacing 4/8/12/16/24/32px; radius 8px; body 16px; page heading 28–32px; form labels 14–16px.
- Container public tối đa 1200px; nội dung bài tối đa 800px. Form modal thường tối đa 720px; editor bài 960px desktop, fullscreen mobile nếu cần.

## Shell và component

Public: logo chữ FUNews, Tin tức, Tìm kiếm, Đăng nhập hoặc menu người dùng. Không thêm notification bell khi chưa có chức năng.
Admin/Staff: sidebar theo role, header tên màn hình và account menu. Mobile sidebar thành offcanvas. Lecturer dùng public shell.
Dùng shared partial cho page header, toolbar, pagination, status badge, field error, toast, confirm modal, empty state. Không nhân bản palette/CSS từng màn hình.

## Blueprint

| Trang | Nội dung và CTA |
|---|---|
| Home | Danh sách title/headline/category/date; CTA Đọc bài; tìm kiếm và phân trang |
| Detail | Title, headline, tác giả, ngày, nội dung plain text, tags, tối đa 3 related |
| Search | Keyword title/headline/content, category, tag, author, date range, sort; clear filters |
| Accounts | Table tên/email/role, search và role filter, Thêm tài khoản, Sửa, Xóa |
| Categories | Tên/mô tả/cha/status/số bài; modal thêm/sửa; bật/tắt qua thao tác được xác thực |
| Tags | Tên/note; modal thêm/sửa; xem bài dùng tag; confirm xóa |
| News management | Title/category/author/date/status; filter, tạo/sửa/xóa/nhân bản |
| Article modal | Title/headline/content/source/category/status/multi-select tag; Lưu và Hủy |
| Profile | Thông tin bản thân; modal sửa thông tin và modal đổi mật khẩu tách biệt |
| My history | Tin do mình tạo, status/date, link chi tiết quản lý hợp lệ |
| Report | Date range, groupBy, tổng Active/Inactive, bảng nhóm, chi tiết editor/date |

## Form và trạng thái

Mỗi flow cần initial, loading, empty, invalid, saving, success, error, unauthorized/forbidden phù hợp. Required label rõ; field error gần input; summary lỗi phía đầu modal. Lỗi giữ input và focus vào lỗi đầu tiên. Save disable trong request; thành công đóng modal, cập nhật dữ liệu và toast; thất bại không đóng modal.
Xóa hiển thị tên đối tượng và nút Hủy/Xóa, không chỉ hỏi ID. 409 nói rõ liên kết khiến không thể xóa. Nhân bản là mutation có loading, khi xong mở bản sao Inactive để sửa; không publish ngay.
Không chèn dữ liệu người dùng bằng innerHTML chưa encode. Nội dung plain text giữ xuống dòng an toàn. Password không prefill giá trị đang lưu và không hiện trong table.

## Responsive và accessibility

Kiểm tra 390, 768, 1024, 1440px và reflow 320px khi phù hợp. Không toàn trang cuộn ngang; table có wrapper cuộn riêng hoặc ưu tiên cột quan trọng trên mobile. Long title/email wrap hoặc truncate có cách đọc đầy đủ. Modal có label, focus trap, Escape khi không đang commit, trả focus về nút mở. Mọi control dùng bàn phím, focus ring rõ, label gắn input, loading/success có aria-live phù hợp.

## Không tự thêm

Upload ảnh, banner carousel, rich-text editor, approval, AI, dark mode, charts không có dữ liệu thật, nút export trước FUN-022. Report dùng bảng và tổng số là đủ.
