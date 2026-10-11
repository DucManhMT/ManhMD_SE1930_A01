/**
 * Dashboard Charts & Dynamic Filtering Script — FUNews Assignment 02
 * Hỗ trợ vẽ Chart.js và cập nhật số liệu thời gian thực qua Ajax handler
 */
(function () {
    'use strict';

    let categoryChart = null;
    let periodChart = null;

    const chartColors = [
        '#0d6efd', '#20c997', '#ffc107', '#fd7e14',
        '#6f42c1', '#d63384', '#0dcaf0', '#6c757d',
        '#198754', '#dc3545', '#3d5a80', '#98c1d9'
    ];

    document.addEventListener('DOMContentLoaded', function () {
        const initial = window.initialDashboardData || {};
        const dashboard = initial.dashboard || {};
        const trending = initial.trending || [];

        renderCharts(dashboard);
        bindFilterEvents();
    });

    /**
     * Khởi tạo hoặc cập nhật 2 biểu đồ Chart.js
     */
    function renderCharts(dashboard) {
        renderCategoryPieChart(dashboard.articlesByCategory || dashboard.byCategory || []);
        renderPeriodBarChart(dashboard.articlesByPeriod || []);
    }

    /**
     * 1. Pie Chart: Phân bố bài viết theo Chuyên mục
     */
    function renderCategoryPieChart(categoriesData) {
        const canvas = document.getElementById('categoryPieChart');
        const emptyMsg = document.getElementById('noCategoryData');
        if (!canvas) return;

        if (categoryChart) {
            categoryChart.destroy();
            categoryChart = null;
        }

        if (!categoriesData || categoriesData.length === 0) {
            canvas.style.display = 'none';
            if (emptyMsg) emptyMsg.classList.remove('d-none');
            return;
        }

        canvas.style.display = 'block';
        if (emptyMsg) emptyMsg.classList.add('d-none');

        const labels = categoriesData.map(c => c.categoryName || `Category #${c.categoryId}`);
        const data = categoriesData.map(c => c.count);
        const bgColors = labels.map((_, i) => chartColors[i % chartColors.length]);

        const ctx = canvas.getContext('2d');
        categoryChart = new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: bgColors,
                    hoverOffset: 6,
                    borderWidth: 2,
                    borderColor: '#ffffff'
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            boxWidth: 14,
                            padding: 10,
                            font: { size: 12 }
                        }
                    },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const val = context.raw || 0;
                                const pct = total > 0 ? ((val / total) * 100).toFixed(1) : 0;
                                return ` ${context.label}: ${val} bài (${pct}%)`;
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * 2. Bar Chart: Phân bố bài viết theo Chu kỳ thời gian
     */
    function renderPeriodBarChart(periodData) {
        const canvas = document.getElementById('periodBarChart');
        const emptyMsg = document.getElementById('noPeriodData');
        if (!canvas) return;

        if (periodChart) {
            periodChart.destroy();
            periodChart = null;
        }

        if (!periodData || periodData.length === 0) {
            canvas.style.display = 'none';
            if (emptyMsg) emptyMsg.classList.remove('d-none');
            return;
        }

        canvas.style.display = 'block';
        if (emptyMsg) emptyMsg.classList.add('d-none');

        const labels = periodData.map(p => p.period);
        const data = periodData.map(p => p.count);

        const ctx = canvas.getContext('2d');
        periodChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: 'Số lượng bài viết',
                    data: data,
                    backgroundColor: 'rgba(13, 110, 253, 0.75)',
                    borderColor: '#0d6efd',
                    borderWidth: 1.5,
                    borderRadius: 4
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: { stepSize: 1, precision: 0 },
                        grid: { color: 'rgba(0, 0, 0, 0.05)' }
                    },
                    x: {
                        grid: { display: false }
                    }
                },
                plugins: {
                    legend: { display: false },
                    tooltip: {
                        callbacks: {
                            label: function (context) {
                                return ` ${context.raw} bài viết`;
                            }
                        }
                    }
                }
            }
        });
    }

    /**
     * Bắt sự kiện Ajax lọc dữ liệu không cần reload toàn bộ trang
     */
    function bindFilterEvents() {
        const form = document.getElementById('formDashboardFilter');
        if (!form) return;

        const indicator = document.getElementById('filterStatusIndicator');
        const btnExportExcel = document.getElementById('btnExportExcel');

        form.addEventListener('submit', async function (e) {
            e.preventDefault();

            const formData = new FormData(form);
            const params = new URLSearchParams();

            for (const [key, value] of formData.entries()) {
                if (value && value.trim() !== '') {
                    params.append(key, value.trim());
                }
            }

            // Đồng bộ link xuất Excel theo bộ lọc hiện tại
            if (btnExportExcel) {
                const excelParams = new URLSearchParams(params);
                excelParams.set('handler', 'ExportExcel');
                btnExportExcel.href = `/admin/dashboard?${excelParams.toString()}`;
            }

            // Hiển thị trạng thái đang tải
            if (indicator) {
                indicator.innerHTML = '<span class="spinner-border spinner-border-sm text-primary me-1"></span>Đang tải dữ liệu...';
            }

            try {
                const url = `/admin/dashboard?handler=ChartData&${params.toString()}`;
                const response = await fetch(url, {
                    headers: { 'X-Requested-With': 'XMLHttpRequest' }
                });

                if (!response.ok) {
                    throw new Error(`Lỗi phản hồi API (${response.status})`);
                }

                const result = await response.json();
                if (!result.success) {
                    throw new Error(result.message || 'Lỗi không xác định khi tải dữ liệu.');
                }

                const dash = result.dashboard || {};
                const trending = result.trending || [];

                // Cập nhật 4 thẻ KPI
                updateKpis(dash, trending);

                // Cập nhật lại 2 biểu đồ
                renderCharts(dash);

                // Cập nhật bảng Trending
                updateTrendingTable(trending);

                if (indicator) {
                    indicator.innerHTML = '<i class="bi bi-check-circle-fill text-success me-1"></i>Đã cập nhật số liệu';
                }
            } catch (err) {
                console.error('Filter error:', err);
                if (indicator) {
                    indicator.innerHTML = `<i class="bi bi-exclamation-triangle-fill text-danger me-1"></i>${err.message}`;
                }
            }
        });
    }

    /**
     * Cập nhật 4 thẻ số liệu KPI
     */
    function updateKpis(dash, trending) {
        const total = document.getElementById('kpiTotalArticles');
        const active = document.getElementById('kpiActiveArticles');
        const inactive = document.getElementById('kpiInactiveArticles');
        const views = document.getElementById('kpiTotalViews');

        if (total) total.textContent = dash.totalArticles ?? 0;
        if (active) active.textContent = dash.activeArticles ?? 0;
        if (inactive) inactive.textContent = dash.inactiveArticles ?? 0;

        const sumViews = (trending || []).reduce((acc, cur) => acc + (cur.viewCount || 0), 0);
        if (views) views.textContent = sumViews;
    }

    /**
     * Cập nhật bảng Trending Articles
     */
    function updateTrendingTable(trending) {
        const tbody = document.getElementById('trendingTableBody');
        if (!tbody) return;

        if (!trending || trending.length === 0) {
            tbody.innerHTML = `
                <tr>
                    <td colspan="6" class="text-center py-4 text-muted small">
                        <i class="bi bi-inbox fs-4 d-block mb-1"></i>Chưa có bài viết nào được ghi nhận lượt đọc.
                    </td>
                </tr>`;
            return;
        }

        let html = '';
        trending.forEach((item, index) => {
            const idx = index + 1;
            const rankClass = idx <= 3 ? 'text-danger' : 'text-muted';
            const trophy = idx === 1 ? '<i class="bi bi-trophy-fill text-warning me-1"></i>' : '';
            const created = item.createdDate ? new Date(item.createdDate).toLocaleDateString('vi-VN', {
                day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit'
            }) : '-';
            const cat = item.categoryName ? item.categoryName : 'Chưa phân loại';

            html += `
                <tr>
                    <td class="ps-3 fw-bold ${rankClass}">${trophy}${idx}</td>
                    <td><code class="text-primary">${escapeHtml(item.newsArticleId || '')}</code></td>
                    <td>
                        <a href="/news/${encodeURIComponent(item.newsArticleId || '')}" class="text-decoration-none text-dark fw-semibold" target="_blank">
                            ${escapeHtml(item.newsTitle || '')}
                        </a>
                    </td>
                    <td><span class="badge bg-light text-dark border">${escapeHtml(cat)}</span></td>
                    <td class="small text-muted">${created}</td>
                    <td class="text-end pe-3">
                        <span class="badge bg-danger px-2 py-1 fw-bold">
                            <i class="bi bi-eye me-1"></i>${item.viewCount || 0}
                        </span>
                    </td>
                </tr>`;
        });

        tbody.innerHTML = html;
    }

    function escapeHtml(text) {
        if (!text) return '';
        return String(text)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#039;');
    }
})();
