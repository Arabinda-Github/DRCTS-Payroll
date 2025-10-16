
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
