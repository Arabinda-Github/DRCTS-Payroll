$('#btnForgot').on('click', function () {
    var email = $('#userEmail').val().trim();

    if (!email) {
        showToast('Please enter your email', 'error');
        return;
    }

    // Basic client-side email validation
    var emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (!emailRegex.test(email)) {
        showToast('Please enter valid email format', 'error');
        return;
    }

    const btn = document.getElementById("btnForgot");
    btn.disabled = true;

    // Show loading spinner icon
    btn.innerHTML = `<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> Loading...`;
    const formData = new FormData();
    formData.append("Email", email);
    $.ajax({
        url: '/Home/ForgotPassword',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        success: function (response) {
            if (response.success) {
                $('#userEmail').val('');
                showToast(response.message || 'Password reset link sent!', 'success');
            } else {
                showToast(response.message || 'Something went wrong!', 'error');
            }
        },
        error: function (xhr) {
            showToast(xhr.responseText || 'Server error occurred', 'error');
        },
        complete: function () {
            // Restore button text with icon
            btn.disabled = false;
            btn.innerHTML = `Reset <i class="fas fa-sign-in-alt ms-1"></i>`;
        }
    });
});
