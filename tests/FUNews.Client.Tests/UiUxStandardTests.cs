using System;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace FUNews.Client.Tests;

public class UiUxStandardTests
{
    private readonly string _solutionDir;

    public UiUxStandardTests()
    {
        // Navigate up from bin/Debug/net8.0 to solution root
        var currentDir = Directory.GetCurrentDirectory();
        while (!string.IsNullOrEmpty(currentDir) && !File.Exists(Path.Combine(currentDir, "FUNewsManagement_FE.sln")))
        {
            var parent = Directory.GetParent(currentDir);
            if (parent == null) break;
            currentDir = parent.FullName;
        }
        _solutionDir = currentDir;
    }

    [Fact]
    public void Layout_ContainsGlobalLoadingOverlayAndA11ySkipLink()
    {
        var layoutPath = Path.Combine(_solutionDir, "FUNewsManagement_FE", "Pages", "Shared", "_Layout.cshtml");
        Assert.True(File.Exists(layoutPath), $"Không tìm thấy _Layout.cshtml tại {layoutPath}");

        var content = File.ReadAllText(layoutPath);

        // 1. Kiểm tra Global Loading Overlay
        Assert.Contains("id=\"globalLoading\"", content);
        Assert.Contains("fu-loading-overlay", content);
        Assert.Contains("spinner-border", content);

        // 2. Kiểm tra Toast Container
        Assert.Contains("id=\"fuToastContainer\"", content);
        Assert.Contains("aria-live=\"polite\"", content);

        // 3. Kiểm tra Skip to content link cho A11y
        Assert.Contains("class=\"skip-to-content\"", content);
        Assert.Contains("id=\"mainContent\"", content);
    }

    [Fact]
    public void SiteJs_ImplementsGlobalLoadingAndToastHelpers()
    {
        var jsPath = Path.Combine(_solutionDir, "FUNewsManagement_FE", "wwwroot", "js", "site.js");
        Assert.True(File.Exists(jsPath), $"Không tìm thấy site.js tại {jsPath}");

        var content = File.ReadAllText(jsPath);

        // Kiểm tra hàm loading
        Assert.Contains("window.showGlobalLoading", content);
        Assert.Contains("window.hideGlobalLoading", content);
        Assert.Contains("window.showLoading", content);

        // Kiểm tra hàm toast
        Assert.Contains("window.showFuToast", content);
        Assert.Contains("fu-toast", content);

        // Kiểm tra xử lý phím Esc đóng modal (Accessibility)
        Assert.Contains("Escape", content);
    }

    [Fact]
    public void SiteCss_ContainsLoadingOverlayAndFocusVisibleRules()
    {
        var cssPath = Path.Combine(_solutionDir, "FUNewsManagement_FE", "wwwroot", "css", "site.css");
        Assert.True(File.Exists(cssPath), $"Không tìm thấy site.css tại {cssPath}");

        var content = File.ReadAllText(cssPath);

        Assert.Contains(".fu-loading-overlay", content);
        Assert.Contains(".skip-to-content", content);
        Assert.Contains(":focus-visible", content);
        Assert.Contains(".fu-toast", content);
    }

    [Theory]
    [InlineData("Pages/Staff/News.cshtml", "modalDeleteNews")]
    [InlineData("Pages/Staff/Categories.cshtml", "modalDeleteCategory")]
    [InlineData("Pages/Staff/Tags.cshtml", "modalDeleteTag")]
    [InlineData("Pages/Admin/Accounts.cshtml", "modalDeleteAccount")]
    public void CrudPages_UseBootstrapConfirmationModals_WithoutRawWindowConfirm(string relativePath, string expectedDeleteModalId)
    {
        var fullPath = Path.Combine(_solutionDir, "FUNewsManagement_FE", relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(fullPath), $"Không tìm thấy view tại {fullPath}");

        var content = File.ReadAllText(fullPath);

        // Đảm bảo có modal xác nhận xóa
        Assert.Contains($"id=\"{expectedDeleteModalId}\"", content);
        Assert.Contains("modal fade", content);
        Assert.Contains("data-bs-dismiss=\"modal\"", content);

        // Đảm bảo không sử dụng confirm() của trình duyệt
        Assert.DoesNotMatch(new Regex(@"\bwindow\.confirm\s*\(", RegexOptions.IgnoreCase), content);
        Assert.DoesNotMatch(new Regex(@"\bconfirm\s*\([^)]*\)\s*;", RegexOptions.IgnoreCase), content);
    }
}
