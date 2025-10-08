
$("#submitbtn").click(function (e) {
    e.preventDefault();
    var username = $("#username").val();
    var password = $("#userpassword").val();
    if (username == "" && password == "") {
        message = "Username & Password Required";
        Toast.fire({
            icon: 'error',
            title: message
        });
        return;
    }
    if (password == "") {
        message = "Password Required";
        Toast.fire({
            icon: 'error',
            title: message
        });
        return;
    }
    if (username == "") {
        message = "Username Required";
        Toast.fire({
            icon: 'error',
            title: message
        });
        return;
    }
    $("#submitbtn").prop("disabled", true).text("Loading...");
    $.ajax({
        type: "POST",
        url: "/Account/AdminLogin",
        contentType: "application/json",
        dataType: "json",
        data: JSON.stringify({
            Username: username,
            Password: password,
            LoginType: "Admin"
        }),
        success: function (response) {
            if (response.success) {
                Toast.fire({
                    icon: 'success',
                    title: response.message
                });
                setTimeout(function () {
                    window.location.href = response.redirectUrl;
                }, 2000);
            } else {
                Toast.fire({
                    icon: 'error',
                    title: response.message || "Incorrect username or password. Please try again."
                });
                $('#userpassword').val('');
                $('#username').val('');

            }
        },
        error: function (xhr) {
            console.error("AJAX Error:", xhr.responseText);
            Toast.fire({
                icon: 'error',
                title: "An unexpected error occurred. Please try again."
            });
        },
        complete: function () {
            // Re-enable the button and reset text after request completes
            $("#submitbtn").prop("disabled", false).text("Submit");
        }
    });
});