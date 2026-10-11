// Notification Client for FUNews SignalR Realtime Notifications
(function () {
    'use strict';

    if (typeof signalR === 'undefined') {
        console.warn('SignalR library not loaded. Realtime notifications disabled.');
        return;
    }

    const badgeEl = document.getElementById('notificationCountBadge');
    const listEl = document.getElementById('notificationDropdownList');
    const emptyNoticeEl = document.getElementById('notificationEmptyNotice');
    const clearBtn = document.getElementById('btnClearNotifications');

    let unreadCount = 0;
    const MAX_RECENT_NOTIFICATIONS = 10;

    // Khởi tạo kết nối SignalR Hub
    const connection = new signalR.HubConnectionBuilder()
        .withUrl('/hubs/notifications')
        .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
        .configureLogging(signalR.LogLevel.Warning)
        .build();

    function updateBadge() {
        if (!badgeEl) return;
        if (unreadCount > 0) {
            badgeEl.textContent = unreadCount > 99 ? '99+' : unreadCount;
            badgeEl.classList.remove('d-none');
        } else {
            badgeEl.textContent = '0';
            badgeEl.classList.add('d-none');
        }
    }

    function createNotificationItemHtml(data) {
        const timeStr = data.createdDate
            ? new Date(data.createdDate).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })
            : 'Vừa xong';
        const title = data.title || 'Bài viết mới';
        const category = data.categoryName ? `<span class="badge bg-light text-primary border me-1">${data.categoryName}</span>` : '';
        const author = data.author ? ` • ${data.author}` : '';

        return `
            <li class="border-bottom notification-item p-2 hover-bg-light transition-all">
                <a href="/news?id=${encodeURIComponent(data.articleId || '')}" class="text-decoration-none text-dark d-block">
                    <div class="d-flex justify-content-between align-items-center mb-1">
                        <span class="badge bg-primary-subtle text-primary small"><i class="bi bi-newspaper me-1"></i>Bài viết mới</span>
                        <span class="text-muted small">${timeStr}</span>
                    </div>
                    <div class="fw-semibold small text-truncate-2 mb-1">${title}</div>
                    <div class="text-muted smaller">${category}${author}</div>
                </a>
            </li>
        `;
    }

    function appendNotification(data, isInitialLoad) {
        if (!listEl) return;

        if (emptyNoticeEl) {
            emptyNoticeEl.classList.add('d-none');
        }

        const itemHtml = createNotificationItemHtml(data);
        listEl.insertAdjacentHTML('afterbegin', itemHtml);

        // Đảm bảo không vượt quá 10 thông báo gần nhất
        while (listEl.children.length > MAX_RECENT_NOTIFICATIONS) {
            listEl.removeChild(listEl.lastElementChild);
        }

        if (!isInitialLoad) {
            unreadCount++;
            updateBadge();

            // Hiển thị Toast thông báo pop-up
            showNotificationToast(data);
        }
    }

    function showNotificationToast(data) {
        const title = data.title || 'Bài viết mới';
        const category = data.categoryName ? `[${data.categoryName}] ` : '';
        const author = data.author ? ` bởi ${data.author}` : '';
        const message = `🎉 ${category}${title}${author} vừa được xuất bản!`;

        if (window.showFuToast) {
            window.showFuToast(message, 'info');
        } else {
            console.log('Realtime notification:', message);
        }
    }

    // Lắng nghe sự kiện ReceiveNotification từ server
    connection.on('ReceiveNotification', function (data) {
        if (!data) return;
        appendNotification(data, false);
    });

    // Kết nối và nạp danh sách 10 thông báo gần nhất
    connection.start()
        .then(function () {
            return connection.invoke('GetRecentNotifications');
        })
        .then(function (recentNotifications) {
            if (recentNotifications && recentNotifications.length > 0) {
                // Sắp xếp tăng dần rồi append để tin mới nhất ở đầu
                recentNotifications.slice().reverse().forEach(function (item) {
                    appendNotification(item, true);
                });
            } else if (emptyNoticeEl) {
                emptyNoticeEl.classList.remove('d-none');
            }
        })
        .catch(function (err) {
            console.warn('SignalR connection error:', err);
        });

    // Nút đánh dấu đã đọc
    if (clearBtn) {
        clearBtn.addEventListener('click', function (e) {
            e.preventDefault();
            unreadCount = 0;
            updateBadge();
        });
    }
})();
