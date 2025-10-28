
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
    //btn.textContent = "Loading...";
    // Show loading spinner icon
    btn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...`;

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
            // Keep button disabled and show success state
            btn.textContent = "Redirecting...";
            btn.classList.add("btn-success"); // Optional: Change button color

            showToast(result.message || 'Login successful!', 'success');

            // Redirect after toast
            setTimeout(() => {
                window.location.href = result.redirectUrl || "/Dashboard/AdminDashboard";
            }, 1000);

        } else {
            // Only reset on failure
            showToast(result.message || "Incorrect username or password.", 'error');
            document.getElementById("username").value = "";
            document.getElementById("userpassword").value = "";

            // Reset button state on error
            btn.disabled = false;
            btn.innerHTML = `Log In <i class="fas fa-sign-in-alt ms-1"></i>`;
            btn.classList.remove("btn-success");
        }

    } catch (err) {
        console.error("Fetch error:", err);
        showToast("An unexpected error occurred. Please try again.", 'error');

        // Reset button state on error
        btn.disabled = false;
        btn.innerHTML = `Log In <i class="fas fa-sign-in-alt ms-1"></i>`;
    }
});
