// FUNews Management System - Client Scripts (Bootstrap 5 & Vanilla JS)

document.addEventListener('DOMContentLoaded', function () {
    // 1. Khởi tạo và xử lý Modal Đăng nhập AJAX
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
});
