
const urlParams = new URLSearchParams(window.location.search);
const token = urlParams.get('token');

if (!token) {
    showToast('Invalid reset link', 'error');
    // hide form
} else {
    // optionally validate token via API
    $.ajax({
        url: '/Home/ValidateToken',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ Token: token }),
        success: function (res) {
            // token valid
        },
        error: function () {
            showToast('Token invalid or expired', 'error');         
        }
    });
}

$('#btnReset').on('click', function () {
    var newPassword = $('#newPassword').val();
    if (!newPassword) {
        showToast('Please enter password', 'error');
        return;
    }
    if (!newPassword || newPassword.length < 6) {
        showToast('Enter at least 6 digit password', 'error');
        return;
    }

    const btn = $('#btnReset');
    btn.prop('disabled', true);
    btn.html(`<span class="spinner-border spinner-border-sm"></span> Updating...`);

    $.ajax({
        url: '/Home/ResetPassword',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify({ Token: token, NewPassword: newPassword }),
        success: function (res) {
            showToast(res.message || 'Password changed', 'success');
            window.location.href = '/Account/Login';
        },
        error: function (xhr) {
            showToast(xhr.responseJSON?.message || xhr.responseText || 'Error', 'error');
            alert();
        },
        complete: function () {
            btn.prop('disabled', false);
            btn.html('Submit');
        }
    });
});
