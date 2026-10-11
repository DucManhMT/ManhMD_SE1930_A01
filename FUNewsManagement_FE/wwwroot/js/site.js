// FUNews Management System - Client Scripts (Bootstrap 5 & Vanilla JS)

/**
 * 1. Global Loading Overlay Functions
 */
window.showGlobalLoading = function (message = 'Đang xử lý, vui lòng chờ...') {
    const overlay = document.getElementById('globalLoading');
    if (overlay) {
        const textEl = overlay.querySelector('.fu-loading-text');
        if (textEl) {
            textEl.textContent = message;
        }
        overlay.classList.remove('d-none');
        overlay.setAttribute('aria-busy', 'true');
    }
};

window.showLoading = window.showGlobalLoading;

window.hideGlobalLoading = function () {
    const overlay = document.getElementById('globalLoading');
    if (overlay) {
        overlay.classList.add('d-none');
        overlay.setAttribute('aria-busy', 'false');
    }
};

window.hideLoading = window.hideGlobalLoading;

/**
 * 2. Global Toast Notification Helper
 * @param {string} message - Nội dung thông báo
 * @param {string} type - 'success', 'danger'/'error', 'warning', 'info'
 * @param {number} delay - Thời gian tự ẩn (ms), mặc định 4000ms
 */
window.showFuToast = function (message, type = 'info', delay = 4000) {
    if (!message) return;

    const container = document.getElementById('fuToastContainer');
    if (!container) return;

    let bgClass = 'bg-primary text-white';
    let iconClass = 'bi-info-circle-fill';
    let titleText = 'Thông báo';

    const normalizedType = (type || 'info').toLowerCase();
    if (normalizedType === 'success') {
        bgClass = 'bg-success text-white';
        iconClass = 'bi-check-circle-fill';
        titleText = 'Thành công';
    } else if (normalizedType === 'danger' || normalizedType === 'error') {
        bgClass = 'bg-danger text-white';
        iconClass = 'bi-x-circle-fill';
        titleText = 'Lỗi';
    } else if (normalizedType === 'warning') {
        bgClass = 'bg-warning text-dark';
        iconClass = 'bi-exclamation-triangle-fill';
        titleText = 'Cảnh báo';
    }

    const toastId = 'toast_' + Date.now() + '_' + Math.floor(Math.random() * 1000);
    const toastHtml = `
        <div id="${toastId}" class="toast fu-toast align-items-center ${bgClass} border-0 shadow mb-2" role="alert" aria-live="assertive" aria-atomic="true">
            <div class="d-flex">
                <div class="toast-body d-flex align-items-start gap-2 py-3 px-3">
                    <i class="bi ${iconClass} fs-5 mt-n1"></i>
                    <div>
                        <strong class="d-block mb-1 small text-uppercase opacity-75">${titleText}</strong>
                        <div class="small fw-normal">${message}</div>
                    </div>
                </div>
                <button type="button" class="btn-close ${normalizedType === 'warning' ? '' : 'btn-close-white'} me-2 m-auto" data-bs-dismiss="toast" aria-label="Đóng"></button>
            </div>
        </div>
    `;

    container.insertAdjacentHTML('beforeend', toastHtml);
    const toastEl = document.getElementById(toastId);
    if (toastEl && window.bootstrap && window.bootstrap.Toast) {
        const toast = new bootstrap.Toast(toastEl, { delay: delay, autohide: true });
        toast.show();
        toastEl.addEventListener('hidden.bs.toast', function () {
            toastEl.remove();
        });
    }
};

/**
 * 3. Document Ready Initialization
 */
document.addEventListener('DOMContentLoaded', function () {
    // 3.1. Tự động kiểm tra Flash Messages từ Server (nếu có trong container attribute)
    const toastContainer = document.getElementById('fuToastContainer');
    if (toastContainer) {
        const serverSuccess = toastContainer.getAttribute('data-server-success');
        if (serverSuccess) {
            window.showFuToast(serverSuccess, 'success');
        }
        const serverError = toastContainer.getAttribute('data-server-error');
        if (serverError) {
            window.showFuToast(serverError, 'danger');
        }
    }

    // 3.2. Khởi tạo và xử lý Modal Đăng nhập AJAX
    const loginForm = document.getElementById('fuLoginModalForm');
    if (loginForm) {
        loginForm.addEventListener('submit', async function (e) {
            e.preventDefault();

            const emailInput = document.getElementById('fuLoginModalEmail');
            const passwordInput = document.getElementById('fuLoginModalPassword');
            const alertBox = document.getElementById('fuLoginModalAlert');
            const submitBtn = document.getElementById('fuLoginModalSubmitBtn');
            const spinner = document.getElementById('fuLoginModalSpinner');
            const btnText = document.getElementById('fuLoginModalBtnText');

            const email = emailInput ? emailInput.value.trim() : '';
            const password = passwordInput ? passwordInput.value : '';

            if (!email || !password) {
                if (alertBox) {
                    alertBox.textContent = 'Vui lòng nhập đầy đủ email và mật khẩu.';
                    alertBox.classList.remove('d-none');
                }
                return;
            }

            // Lấy token antiforgery từ form
            const tokenInput = loginForm.querySelector('input[name="__RequestVerificationToken"]');
            const csrfToken = tokenInput ? tokenInput.value : '';

            // Bật trạng thái loading
            if (submitBtn) submitBtn.disabled = true;
            if (spinner) spinner.classList.remove('d-none');
            if (btnText) btnText.textContent = 'Đang xác thực...';
            if (alertBox) alertBox.classList.add('d-none');

            try {
                const response = await fetch('/Login?handler=AjaxLogin', {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json',
                        'X-CSRF-TOKEN': csrfToken
                    },
                    body: JSON.stringify({ email: email, password: password })
                });

                const data = await response.json();

                if (data && data.success) {
                    // Đăng nhập thành công: chuyển hướng đến trang đích theo vai trò
                    window.location.href = data.redirectUrl || '/';
                } else {
                    if (alertBox) {
                        alertBox.textContent = (data && data.message) ? data.message : 'Email hoặc mật khẩu không chính xác.';
                        alertBox.classList.remove('d-none');
                    }
                    if (passwordInput) {
                        passwordInput.value = '';
                        passwordInput.focus();
                    }
                }
            } catch (err) {
                if (alertBox) {
                    alertBox.textContent = 'Không thể kết nối đến máy chủ xác thực. Vui lòng thử lại sau.';
                    alertBox.classList.remove('d-none');
                }
            } finally {
                if (submitBtn) submitBtn.disabled = false;
                if (spinner) spinner.classList.add('d-none');
                if (btnText) btnText.textContent = 'Đăng nhập';
            }
        });
    }

    // 3.3. Tự động bật Spinner Overlay cho các Form POST tiêu chuẩn (trừ ajax hoặc modal forms)
    document.querySelectorAll('form').forEach(function (form) {
        if (form.getAttribute('method')?.toLowerCase() === 'post') {
            // Không can thiệp nếu là form ajax login hoặc đã có novalidate ajax handler
            if (form.id === 'fuLoginModalForm' || form.id === 'fuLogoutForm' || form.hasAttribute('data-ajax')) {
                return;
            }

            form.addEventListener('submit', function (e) {
                // Kiểm tra nếu form hợp lệ thì mới hiện spinner
                if (!form.checkValidity || form.checkValidity()) {
                    // Tránh hiện nếu người dùng bị preventDefault bởi validation
                    setTimeout(function () {
                        if (!e.defaultPrevented) {
                            window.showGlobalLoading('Đang xử lý yêu cầu...');
                        }
                    }, 50);
                }
            });
        }
    });

    // 3.4. Trợ năng bàn phím: Nhấn phím Escape để đóng Modal đang active nếu có
    document.addEventListener('keydown', function (e) {
        if (e.key === 'Escape') {
            const openModal = document.querySelector('.modal.show');
            if (openModal && window.bootstrap && window.bootstrap.Modal) {
                const modalInstance = bootstrap.Modal.getInstance(openModal);
                if (modalInstance) {
                    modalInstance.hide();
                }
            }
        }
    });
});
