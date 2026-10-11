using ClosedXML.Excel;
using FUNewsManagement_AnalyticsAPI.Models;

namespace FUNewsManagement_AnalyticsAPI.Services;

public class ExcelExportService : IExcelExportService
{
    public byte[] ExportDashboardReport(
        AnalyticsDashboardDto dashboard,
        List<CoreNewsArticleDto> articles,
        Dictionary<string, int> viewStats,
        DashboardFilterQuery filter)
    {
        using var workbook = new XLWorkbook();

        // ---------------- Sheet 1: Thống Kê Tổng Quan ----------------
        var summarySheet = workbook.Worksheets.Add("Thống Kê Tổng Quan");
        
        // Header Báo Cáo
        summarySheet.Cell(1, 1).Value = "BÁO CÁO THỐNG KÊ HỆ THỐNG TIN TỨC FUNEWS";
        summarySheet.Range(1, 1, 1, 4).Merge().Style
            .Font.SetBold(true)
            .Font.SetFontSize(16)
            .Font.SetFontColor(XLColor.FromHtml("#1F4E79"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        summarySheet.Cell(2, 1).Value = $"Thời gian xuất báo cáo: {DateTime.Now:dd/MM/yyyy HH:mm:ss}";
        summarySheet.Range(2, 1, 2, 4).Merge().Style
            .Font.SetItalic(true)
            .Font.SetFontSize(10)
            .Font.SetFontColor(XLColor.Gray)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Filter info
        int currentRow = 4;
        summarySheet.Cell(currentRow, 1).Value = "Bộ lọc áp dụng:";
        summarySheet.Cell(currentRow, 1).Style.Font.SetBold(true);
        currentRow++;

        var filterDesc = new List<string>();
        if (filter.StartDate.HasValue) filterDesc.Add($"Từ ngày: {filter.StartDate:dd/MM/yyyy}");
        if (filter.EndDate.HasValue) filterDesc.Add($"Đến ngày: {filter.EndDate:dd/MM/yyyy}");
        if (filter.CategoryId.HasValue) filterDesc.Add($"Mã danh mục: {filter.CategoryId.Value}");
        if (filter.Status.HasValue) filterDesc.Add($"Trạng thái: {(filter.Status.Value ? "Active" : "Inactive")}");
        
        summarySheet.Cell(currentRow, 1).Value = filterDesc.Count > 0 ? string.Join(" | ", filterDesc) : "Tất cả dữ liệu (Không lọc)";
        summarySheet.Range(currentRow, 1, currentRow, 4).Merge().Style.Font.SetItalic(true);
        currentRow += 2;

        // KPI Box
        summarySheet.Cell(currentRow, 1).Value = "Chỉ số tổng quan";
        summarySheet.Range(currentRow, 1, currentRow, 2).Merge().Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#D9E1F2"));
        currentRow++;

        summarySheet.Cell(currentRow, 1).Value = "Tổng số bài viết:";
        summarySheet.Cell(currentRow, 2).Value = dashboard.TotalArticles;
        currentRow++;

        summarySheet.Cell(currentRow, 1).Value = "Bài viết kích hoạt (Active):";
        summarySheet.Cell(currentRow, 2).Value = dashboard.ActiveArticles;
        currentRow++;

        summarySheet.Cell(currentRow, 1).Value = "Bài viết vô hiệu hóa (Inactive):";
        summarySheet.Cell(currentRow, 2).Value = dashboard.InactiveArticles;
        currentRow++;

        int totalViews = articles.Sum(a => viewStats.TryGetValue(a.NewsArticleId, out var v) ? v : 0);
        summarySheet.Cell(currentRow, 1).Value = "Tổng lượt đọc (Views):";
        summarySheet.Cell(currentRow, 2).Value = totalViews;
        currentRow += 2;

        // Bảng Thống kê theo Chuyên mục
        summarySheet.Cell(currentRow, 1).Value = "Thống kê theo Chuyên mục";
        summarySheet.Range(currentRow, 1, currentRow, 3).Merge().Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#D9E1F2"));
        currentRow++;

        summarySheet.Cell(currentRow, 1).Value = "Mã danh mục";
        summarySheet.Cell(currentRow, 2).Value = "Tên chuyên mục";
        summarySheet.Cell(currentRow, 3).Value = "Số lượng bài viết";
        summarySheet.Range(currentRow, 1, currentRow, 3).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#2F5597"))
            .Font.SetFontColor(XLColor.White);
        currentRow++;

        int catStartRow = currentRow;
        foreach (var cat in dashboard.ArticlesByCategory)
        {
            summarySheet.Cell(currentRow, 1).Value = cat.CategoryId;
            summarySheet.Cell(currentRow, 2).Value = cat.CategoryName;
            summarySheet.Cell(currentRow, 3).Value = cat.Count;
            currentRow++;
        }
        if (catStartRow < currentRow)
        {
            summarySheet.Range(catStartRow, 1, currentRow - 1, 3).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            summarySheet.Range(catStartRow, 1, currentRow - 1, 3).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
        }

        currentRow += 2;

        // Bảng Thống kê theo Trạng thái
        summarySheet.Cell(currentRow, 1).Value = "Thống kê theo Trạng thái";
        summarySheet.Range(currentRow, 1, currentRow, 2).Merge().Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#D9E1F2"));
        currentRow++;

        summarySheet.Cell(currentRow, 1).Value = "Trạng thái";
        summarySheet.Cell(currentRow, 2).Value = "Số lượng bài";
        summarySheet.Range(currentRow, 1, currentRow, 2).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#2F5597"))
            .Font.SetFontColor(XLColor.White);
        currentRow++;

        int statusStartRow = currentRow;
        foreach (var st in dashboard.ByStatus)
        {
            summarySheet.Cell(currentRow, 1).Value = st.Status;
            summarySheet.Cell(currentRow, 2).Value = st.Count;
            currentRow++;
        }
        if (statusStartRow < currentRow)
        {
            summarySheet.Range(statusStartRow, 1, currentRow - 1, 2).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            summarySheet.Range(statusStartRow, 1, currentRow - 1, 2).Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
        }

        summarySheet.Columns().AdjustToContents();

        // ---------------- Sheet 2: Chi Tiết Bài Viết ----------------
        var detailSheet = workbook.Worksheets.Add("Chi Tiết Bài Viết");

        detailSheet.Cell(1, 1).Value = "DANH SÁCH CHI TIẾT BÀI VIẾT";
        detailSheet.Range(1, 1, 1, 7).Merge().Style
            .Font.SetBold(true)
            .Font.SetFontSize(14)
            .Font.SetFontColor(XLColor.FromHtml("#1F4E79"))
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        // Header Table
        string[] headers = { "Mã bài viết", "Tiêu đề", "Chuyên mục", "Tác giả", "Ngày tạo", "Trạng thái", "Lượt xem" };
        for (int i = 0; i < headers.Length; i++)
        {
            detailSheet.Cell(3, i + 1).Value = headers[i];
        }
        detailSheet.Range(3, 1, 3, headers.Length).Style
            .Font.SetBold(true)
            .Fill.SetBackgroundColor(XLColor.FromHtml("#1F4E79"))
            .Font.SetFontColor(XLColor.White)
            .Alignment.SetHorizontal(XLAlignmentHorizontalValues.Center);

        int detailRow = 4;
        foreach (var art in articles)
        {
            detailSheet.Cell(detailRow, 1).Value = art.NewsArticleId;
            detailSheet.Cell(detailRow, 2).Value = art.NewsTitle ?? string.Empty;
            detailSheet.Cell(detailRow, 3).Value = art.CategoryName ?? (art.CategoryId.HasValue ? $"#{art.CategoryId}" : string.Empty);
            detailSheet.Cell(detailRow, 4).Value = art.AuthorName ?? (art.CreatedById.HasValue ? $"User #{art.CreatedById}" : string.Empty);
            detailSheet.Cell(detailRow, 5).Value = art.CreatedDate.HasValue ? art.CreatedDate.Value.ToString("dd/MM/yyyy HH:mm") : string.Empty;
            detailSheet.Cell(detailRow, 6).Value = art.NewsStatus == true ? "Active" : "Inactive";
            detailSheet.Cell(detailRow, 7).Value = viewStats.TryGetValue(art.NewsArticleId, out var v) ? v : 0;
            detailRow++;
        }

        if (detailRow > 4)
        {
            var dataRange = detailSheet.Range(4, 1, detailRow - 1, headers.Length);
            dataRange.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            dataRange.Style.Border.SetInsideBorder(XLBorderStyleValues.Thin);
        }

        detailSheet.Columns().AdjustToContents();

        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        return memoryStream.ToArray();
    }
}
