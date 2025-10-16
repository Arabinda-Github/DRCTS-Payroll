function showToast(message = 'Message will appear here', type = 'info') {

    const toastEl = document.getElementById('myToast');
    const toastBody = toastEl.querySelector('.toast-body');
    const toastIcon = document.getElementById('toastIcon');
    const toastMessage = document.getElementById('toastMessage');
    const toastHeader = toastEl.querySelector('.toast-header');

    // Reset all styles & icons
    toastBody.className = 'toast-body d-flex align-items-center';
    toastHeader.className = 'toast-header d-flex align-items-center';
    toastIcon.className = 'me-2';

    // Set style & icon based on type
    switch (type) {
        case 'success':
            toastBody.classList.add('bg-success', 'text-white');
            toastIcon.classList.add('fas', 'fa-check-circle');
            break;
        case 'error':
            toastBody.classList.add('bg-danger', 'text-white');
            toastIcon.classList.add('fas', 'fa-times-circle');
            break;
        case 'warning':
            toastBody.classList.add('bg-warning', 'text-dark');
            toastIcon.classList.add('fas', 'fa-exclamation-triangle');
            break;
        default:
            toastBody.classList.add('bg-info', 'text-white');
            toastIcon.classList.add('fas', 'fa-info-circle');
            break;
    }

    // Set message
    toastMessage.textContent = message;

    // ✅ Dispose previous instance if exists
    let previousToast = bootstrap.Toast.getInstance(toastEl);
    if (previousToast) previousToast.dispose();

    // ✅ Create new instance with autohide & delay
    const toast = new bootstrap.Toast(toastEl, {
        delay: 3000,
        autohide: true
    });

    toast.show();
}
document.addEventListener("DOMContentLoaded", function () {
    const toastEl = document.getElementById('myToast');
    const toast = bootstrap.Toast.getOrCreateInstance(toastEl);
    toast.hide();
});