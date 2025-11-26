// cookie.js
window.globalUserData = {}; // global object accessible anywhere

// Return the jQuery ajax promise so caller can .done/.fail
function getCookieValue(callback) {
    return $.ajax({
        url: '/Home/GetCookieValue',
        type: 'GET',
        dataType: 'json'
    }).done(function (response) {
        if (response && response.success) {
            // store globally for reuse
            window.globalUserData = response.data || {};

            // update DOM fields if present
            if (response.data) {
                $("#cookiem").val(response.data.mob || "");
                $("#cookieuid").val(response.data.userid || "");
            }

            // callback to update UI
            if (typeof callback === "function") callback(response.data);
        } else {
            // ensure callback still called even when no data
            if (typeof callback === "function") callback(null);
        }
    }).fail(function () {
        showToast('An error occurred while retrieving session data. Please try again.', "error");
        if (typeof callback === "function") callback(null);
    });
}

$(function () {
    // start loading cookie data on document ready
    getCookieValue(updateUIdata).always(function () {
        // ALWAYS trigger the event whether success or fail so listeners can continue
        $(document).trigger("cookieDataLoaded");
    });
});

function updateUIdata(userData) {
    userData = userData || {};
    let cleanName = (userData.name || "").trim();
    let shortText = cleanName !== "" ? cleanName : (userData.role || "").trim();
    let shortName = shortText
        .split(' ')
        .map(n => n[0])
        .join('')
        .toUpperCase();

    if (userData.profilePic) {
        $("#profileImageContainer").html(
            `<img src="${userData.profilePic}" alt="user image" class="rounded-circle me-2 thumb-sm">`
        );
    } else {
        $("#profileImageContainer").html(
            `<div class="rounded-circle me-2 short-name" title="user image">${shortName}</div>`
        );
    }

    $("#urole").text(userData.role || "");
    $("#uname").html(`${shortText} <i class="mdi mdi-chevron-down"></i>`);
}
