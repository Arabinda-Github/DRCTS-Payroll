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

document.getElementById('togglePassword').addEventListener('click', function () {
    const passwordInput = document.getElementById('userpassword');
    const toggleIcon = document.getElementById('toggleIcon');

    if (passwordInput.type === 'password') {
        passwordInput.type = 'text';
        toggleIcon.classList.remove('fa-eye-slash');
        toggleIcon.classList.add('fa-eye');
    } else {
        passwordInput.type = 'password';
        toggleIcon.classList.remove('fa-eye');
        toggleIcon.classList.add('fa-eye-slash');
    }
});

document.getElementById("submitbtn").addEventListener("click", async function (e) {
    e.preventDefault();

    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("userpassword").value.trim();
    const remember = document.getElementById("rememberMe").checked;

    if (!username && !password) {
        showToast('Username & Password Required!', 'error');
        return;
    }
    if (!username) {
        showToast('Username Required!', 'error');
        return;
    }
    if (!password) {
        showToast('Password Required!', 'error');
        return;
    }

    const btn = document.getElementById("submitbtn");
    btn.disabled = true;
    btn.textContent = "Loading...";

    try {
        const response = await fetch("/Home/Login", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                Username: username,
                Password: password,
                RememberMe: remember
            })
        });

        const result = await response.json();

        if (result.success) {
            showToast(result.message || 'Login successful!', 'success');
            setTimeout(() => window.location.href = result.redirectUrl || "/Dashboard/AdminDashboard", 2000);
        } else {
            showToast(result.message || "Incorrect username or password.", 'error');
            document.getElementById("username").value = "";
            document.getElementById("userpassword").value = "";
        }
    } catch (err) {
        console.error("Fetch error:", err);
        showToast("An unexpected error occurred. Please try again.", 'error');
    } finally {
        btn.disabled = false;
        btn.textContent = "Submit";
    }
});
