using FUNews.BusinessLogic.DTOs;
using FUNews.DataAccess.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FUNews.BusinessLogic.Services;

public class ReportService : IReportService
{
    private readonly INewsArticleRepository _articleRepository;

    public ReportService(INewsArticleRepository articleRepository)
    {
        _articleRepository = articleRepository ?? throw new ArgumentNullException(nameof(articleRepository));
    }

    public async Task<NewsReportDto> GenerateReportAsync(
        DateTime startDate,
        DateTime endDate,
        string? groupBy = null,
        CancellationToken cancellationToken = default)
    {
        // FUN-019: AC 1 - Start <= End
        if (startDate.Date > endDate.Date)
        {
            throw new ArgumentException("Khoảng ngày không hợp lệ: Ngày bắt đầu không được lớn hơn ngày kết thúc.");
        }

        // AC 2: Tính hết EndDate local (inclusive 00:00:00 của StartDate đến hết 23:59:59 của EndDate)
        var startLocal = startDate.Date;
        var endExclusive = endDate.Date.AddDays(1);

        // AC 7: Truy vấn trực tiếp NewsArticle repository, không tạo log table
        var query = _articleRepository.GetQueryable().AsNoTracking()
            .Where(a => a.CreatedDate >= startLocal && a.CreatedDate < endExclusive);

        // AC 4: Detail CreatedDate desc, ThenBy NewsArticleID desc
        // AC 5: Left join nullable với Category và UpdatedBy
        var details = await query
            .OrderByDescending(a => a.CreatedDate)
            .ThenByDescending(a => a.NewsArticleID)
            .Select(a => new ReportDetailDto
            {
                NewsArticleId = a.NewsArticleID,
                NewsTitle = a.NewsTitle,
                Headline = a.Headline,
                CategoryId = a.CategoryID,
                CategoryName = a.Category != null && a.Category.CategoryName != null ? a.Category.CategoryName : "Không phân loại",
                CreatedById = a.CreatedByID,
                AuthorName = a.CreatedBy != null && a.CreatedBy.AccountName != null ? a.CreatedBy.AccountName : "Không xác định",
                CreatedDate = a.CreatedDate,
                NewsStatus = a.NewsStatus,
                UpdatedById = a.UpdatedByID,
                LastEditorName = a.UpdatedBy != null && a.UpdatedBy.AccountName != null ? a.UpdatedBy.AccountName : "Chưa chỉnh sửa",
                ModifiedDate = a.ModifiedDate
            })
            .ToListAsync(cancellationToken);

        // AC 3: Aggregate toàn tập
        var totalArticles = details.Count;
        var totalActive = details.Count(d => d.NewsStatus == true);
        var totalInactive = details.Count(d => d.NewsStatus != true);

        var normGroupBy = (groupBy?.Trim().ToLowerInvariant()) switch
        {
            "author" => "author",
            "status" => "status",
            _ => "category"
        };

        var groupSummaries = new List<ReportGroupSummaryDto>();

        if (normGroupBy == "author")
        {
            groupSummaries = details
                .GroupBy(d => new { Key = d.CreatedById?.ToString() ?? "0", Name = d.AuthorName })
                .Select(g => new ReportGroupSummaryDto
                {
                    GroupKey = g.Key.Key,
                    GroupName = g.Key.Name,
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(x => x.NewsStatus == true),
                    InactiveCount = g.Count(x => x.NewsStatus != true)
                })
                .OrderByDescending(g => g.TotalCount)
                .ThenBy(g => g.GroupName)
                .ToList();
        }
        else if (normGroupBy == "status")
        {
            groupSummaries = details
                .GroupBy(d => d.NewsStatus == true)
                .Select(g => new ReportGroupSummaryDto
                {
                    GroupKey = g.Key ? "active" : "inactive",
                    GroupName = g.Key ? "Đang hiển thị (Active)" : "Tạm ẩn (Inactive)",
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(x => x.NewsStatus == true),
                    InactiveCount = g.Count(x => x.NewsStatus != true)
                })
                .OrderByDescending(g => g.TotalCount)
                .ToList();
        }
        else
        {
            groupSummaries = details
                .GroupBy(d => new { Key = d.CategoryId?.ToString() ?? "0", Name = d.CategoryName })
                .Select(g => new ReportGroupSummaryDto
                {
                    GroupKey = g.Key.Key,
                    GroupName = g.Key.Name,
                    TotalCount = g.Count(),
                    ActiveCount = g.Count(x => x.NewsStatus == true),
                    InactiveCount = g.Count(x => x.NewsStatus != true)
                })
                .OrderByDescending(g => g.TotalCount)
                .ThenBy(g => g.GroupName)
                .ToList();
        }

        return new NewsReportDto
        {
            StartDate = startLocal,
            EndDate = endDate.Date,
            GroupBy = normGroupBy,
            TotalArticles = totalArticles,
            TotalActive = totalActive,
            TotalInactive = totalInactive,
            GroupSummaries = groupSummaries,
            Details = details
        };
    }
}
