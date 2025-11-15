$(function () {
    // Auto-calculate total leave days
    const from = document.getElementById("fromDate");
    const to = document.getElementById("toDate");
    const totalDays = document.getElementById("totalDays");

    from.addEventListener("change", calcDays);
    to.addEventListener("change", calcDays);

    function calcDays() {
        const f = new Date(from.value);
        const t = new Date(to.value);
        if (!isNaN(f) && !isNaN(t) && t >= f) {
            const diff = (t - f) / (1000 * 60 * 60 * 24) + 1;
            totalDays.value = diff;
        } else {
            totalDays.value = "";
        }
    }

    loadLeaveTypes();
    loadLeaveBalances();

    // Remove error border on change / input
    $('#leaveType, #fromDate, #toDate').on("change", function () {
        $(this).css('border-color', '');
    });

    $('#reason').on("input", function () {
        $(this).css('border-color', '');
    });

});

// Load leave types
function loadLeaveTypes() {
    $.ajax({
        url: "/Leave/GetLeaveTypes",
        type: "GET",
        success: function (res) {
            $("#leaveType").empty()
                .append(`<option value="">Select Leave Type</option>`);

            $.each(res, function (i, x) {
                $("#leaveType").append(`<option value="${x.leaveTypeId}">${x.leaveTypeName}</option>`);
            });
        }
    });
}

// Load balances
function loadLeaveBalances() {
    $.ajax({
        url: "/Leave/GetBalances",
        type: "GET",
        success: function (data) {
            $("#totalBalance").text(data.leave);
            $("#casualBalance").text(data.casual);
            $("#sickBalance").text(data.sick);
            $("#otherBalance").text(data.other);
        }
    });
}

// Apply leave click
document.getElementById("btnApplyLeave").addEventListener("click", function () {
    const fromDate = document.getElementById("fromDate").value;
    const toDate = document.getElementById("toDate").value;
    const leaveType = document.getElementById("leaveType").value;
    const reason = document.getElementById("reason").value;
    
    let errors = [];
    // Reset previous borders
    $('#leaveType, #fromDate, #toDate, #reason').css('border-color', '');

    if (!leaveType) {
        errors.push("Please select a leave type.");
        $('#leaveType').css('border-color', '#ef4d56');
    }
    if (!fromDate) {
        errors.push("Please select a from date");
        $('#fromDate').css('border-color', '#ef4d56');
    }
    if (!toDate) {
        errors.push("Please select a to date.");
        $('#toDate').css('border-color', '#ef4d56');
    }
    if (!reason) {
        errors.push("Please enter reason.");
        $('#reason').css('border-color', '#ef4d56');
    }
   
    if (errors.length > 0) {
        showToast(errors.join('\n'), "error");
        return;
    }
    const leaveData = {
        fromDate: fromDate,
        toDate: toDate,
        leaveTypeId: leaveType,
        reason: reason
    };
    $('.loader').removeClass('hide');
    $.ajax({
        url: "/Leave/ApplyLeave",
        type: "POST",
        contentType: "application/json",
        data: JSON.stringify(leaveData),
        success: function (res) {
            $('.loader').addClass('hide');
            if (res.success) {
                showToast("Leave applied successfully.", "success");
                loadLeaveBalances();

                // Close modal after short delay
                setTimeout(() => {
                    const modal = bootstrap.Modal.getInstance(
                        document.getElementById("applyLeaveModal")
                    );
                    modal.hide();
                    document.getElementById("applyLeaveForm").reset();
                    showMsg("");
                }, 1200);
            } else {
                alert("Error applying leave: " + res.message);
            }
        }
    });
});