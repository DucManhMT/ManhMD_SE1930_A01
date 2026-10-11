/**
 * AI Tag Suggestion & Badges/Chips Controller — FUNews Assignment 02
 * Kết nối AI API qua Page Handler để tự động gợi ý thẻ tin và kích hoạt Feedback Loop
 */
(function () {
    'use strict';

    document.addEventListener('DOMContentLoaded', function () {
        initAiTagSuggestButtons();
    });

    function getCsrfToken() {
        const tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenInput ? tokenInput.value : '';
    }

    function initAiTagSuggestButtons() {
        const buttons = document.querySelectorAll('.btn-ai-suggest');
        buttons.forEach(btn => {
            btn.addEventListener('click', function () {
                const scope = this.getAttribute('data-scope'); // 'create' hoặc 'edit'
                handleSuggestTags(scope, this);
            });
        });
    }

    async function handleSuggestTags(scope, buttonEl) {
        const isCreate = scope === 'create';
        const contentEl = document.getElementById(isCreate ? 'createNewsContent' : 'editNewsContent');
        const headlineEl = document.getElementById(isCreate ? 'createNewsHeadline' : 'editNewsHeadline');
        const spinnerEl = document.getElementById(isCreate ? 'spinnerAiSuggestCreate' : 'spinnerAiSuggestEdit');
        const containerEl = document.getElementById(isCreate ? 'aiCreateTagContainer' : 'aiEditTagContainer');
        const chipsEl = document.getElementById(isCreate ? 'aiCreateTagChips' : 'aiEditTagChips');
        const countBadge = document.getElementById(isCreate ? 'aiCreateTagCount' : 'aiEditTagCount');

        const content = (contentEl?.value || '').trim() || (headlineEl?.value || '').trim();
        if (!content) {
            showToastMessage(false, 'Vui lòng nhập tóm tắt hoặc nội dung bài viết trước khi yêu cầu gợi ý thẻ tin.');
            contentEl?.focus();
            return;
        }

        // Bật trạng thái loading
        if (buttonEl) buttonEl.disabled = true;
        if (spinnerEl) spinnerEl.classList.remove('d-none');

        try {
            const response = await fetch('/staff/news?handler=SuggestTags', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'X-CSRF-TOKEN': getCsrfToken()
                },
                body: JSON.stringify({ content: content })
            });

            if (!response.ok) {
                throw new Error(`Lỗi phản hồi API (${response.status})`);
            }

            const result = await response.json();
            if (!result.success) {
                throw new Error(result.message || 'Không thể lấy gợi ý thẻ tin từ AI.');
            }

            const tags = result.tags || [];
            renderTagChips(tags, scope, containerEl, chipsEl, countBadge);

            if (tags.length > 0) {
                showToastMessage(true, `AI đã tìm thấy ${tags.length} thẻ tin phù hợp. Bấm vào thẻ để gán nhanh!`);
            } else {
                showToastMessage(false, 'Không tìm thấy thẻ tin nào khớp với nội dung bài viết.');
            }
        } catch (err) {
            console.error('AI Tag Suggest Error:', err);
            showToastMessage(false, err.message || 'Lỗi kết nối khi gọi dịch vụ gợi ý thẻ tin.');
        } finally {
            if (buttonEl) buttonEl.disabled = false;
            if (spinnerEl) spinnerEl.classList.add('d-none');
        }
    }

    function renderTagChips(tags, scope, containerEl, chipsEl, countBadge) {
        if (!chipsEl || !containerEl) return;

        chipsEl.innerHTML = '';

        if (!tags || tags.length === 0) {
            containerEl.classList.add('d-none');
            return;
        }

        containerEl.classList.remove('d-none');
        if (countBadge) countBadge.textContent = `${tags.length} thẻ đề xuất`;

        const isCreate = scope === 'create';
        const cbPrefix = isCreate ? 'cbTag_' : 'editCbTag_';

        tags.forEach(t => {
            const cb = document.getElementById(`${cbPrefix}${t.tagId}`);
            const isAlreadyChecked = cb ? cb.checked : false;

            const chip = document.createElement('span');
            chip.className = `badge p-2 ai-tag-chip ${isAlreadyChecked ? 'bg-success text-white' : 'bg-primary-subtle text-primary border border-primary-subtle'}`;
            chip.style.cursor = 'pointer';
            chip.style.userSelect = 'none';
            chip.style.transition = 'all 0.2s ease';
            chip.setAttribute('data-tag-id', t.tagId);
            chip.title = `Độ tin cậy: ${Math.round((t.confidence || 0) * 100)}% - Bấm để chọn/bỏ chọn`;

            const iconHtml = isAlreadyChecked ? '<i class="bi bi-check2 me-1"></i>' : '<i class="bi bi-plus me-1"></i>';
            const pct = Math.round((t.confidence || 0) * 100);
            chip.innerHTML = `${iconHtml}#${escapeHtml(t.tagName)} <span class="opacity-75 small">(${pct}%)</span>`;

            chip.addEventListener('click', function () {
                toggleTagSelection(t.tagId, scope, chip);
            });

            chipsEl.appendChild(chip);
        });
    }

    function toggleTagSelection(tagId, scope, chipEl) {
        const isCreate = scope === 'create';
        const cb = document.getElementById(`${isCreate ? 'cbTag_' : 'editCbTag_'}${tagId}`);
        if (!cb) return;

        cb.checked = !cb.checked;
        cb.dispatchEvent(new Event('change'));

        updateChipStyle(chipEl, cb.checked);
    }

    function updateChipStyle(chipEl, isChecked) {
        if (!chipEl) return;

        if (isChecked) {
            chipEl.className = 'badge p-2 ai-tag-chip bg-success text-white shadow-sm';
            const plusIcon = chipEl.querySelector('.bi-plus');
            if (plusIcon) plusIcon.className = 'bi bi-check2 me-1';
        } else {
            chipEl.className = 'badge p-2 ai-tag-chip bg-primary-subtle text-primary border border-primary-subtle';
            const checkIcon = chipEl.querySelector('.bi-check2');
            if (checkIcon) checkIcon.className = 'bi bi-plus me-1';
        }
    }

    function showToastMessage(isSuccess, message) {
        const toastSuccessEl = document.getElementById('toastSuccess');
        const toastErrorEl = document.getElementById('toastError');

        if (isSuccess && toastSuccessEl) {
            const msgEl = document.getElementById('toastSuccessMessage');
            if (msgEl) msgEl.textContent = message;
            const toast = bootstrap.Toast.getOrCreateInstance(toastSuccessEl);
            toast.show();
        } else if (!isSuccess && toastErrorEl) {
            const msgEl = document.getElementById('toastErrorMessage');
            if (msgEl) msgEl.textContent = message;
            const toast = bootstrap.Toast.getOrCreateInstance(toastErrorEl);
            toast.show();
        }
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
