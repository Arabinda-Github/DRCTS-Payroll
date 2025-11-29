
$(function () {
    const today = new Date().toISOString().split("T")[0];
    $("#fromDate").attr("min", today);
    $("#toDate").attr("min", today);

    // Auto-calculate total leave days
    const from = document.getElementById("fromDate");
    const to = document.getElementById("toDate");
    const totalDays = document.getElementById("totalDays");

    from.addEventListener("change", function () {
        const fromValue = from.value;

        if (fromValue) {
            // Reset To Date
            to.value = "";

            // Make sure To Date cannot be earlier than From Date
            to.setAttribute("min", fromValue);
        }

        calcDays();
    });
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

    // Remove error border on change / input
    $('#leaveType, #fromDate, #toDate').on("change", function () {
        $(this).css('border-color', '');
    });

    $('#reason').on("input", function () {
        $(this).css('border-color', '');
    });

    loadLeaveRequests();

    // Primary: wait for cookie data loaded event
    $(document).on("cookieDataLoaded", function () {
        const user = window.globalUserData || {};
        const userRole = user.role;

        if (userRole === "Manager" || userRole === "Team Lead") {
            $("#approvalTabContainer").show();
            loadPendingApprovals();
        } else {
            $("#approvalTabContainer").hide();
        }

    });

});

// Load leave types
function loadLeaveTypes() {
    $.ajax({
        url: "/Payroll/GetLeaveTypes",
        type: "GET",
        success: function (res) {
            $("#leaveType").empty()
                .append(`<option value="">Select Leave Type</option>`);

            $.each(res.data, function (i, x) {
                $("#leaveType").append(`<option value="${x.leaveTypeId}">${x.leaveTypeName}</option>`);
            });
        }
    });
}

(async function () {

    // -----------------------
    // API FUNCTIONS (Promise)
    // -----------------------
    function GetLeaveBalances() {
        return $.ajax({
            url: "/Payroll/GetLeaveBalances",
            type: "GET"
        });
    }

    function CheckPendingLeave() {
        return $.ajax({
            url: "/Payroll/CheckPendingLeave",
            type: "GET"
        });
    }


    // -----------------------
    // LOAD BALANCES (async)
    // -----------------------
    async function loadLeaveBalances() {
        try {
            const res = await GetLeaveBalances();   // ✔ works now

            if (res.status && res.data) {

                const details = res.data.leaveDetails || [];

                const getBalance = id => {
                    const item = details.find(x => x.leaveTypeId === id);
                    return item ? `${item.closingBalance} Days` : "0 Days";
                };

                $("#totalBalance").text(`${res.data.totalClosingBalance} Days`);
                $("#casualBalance").text(getBalance(1));
                $("#sickBalance").text(getBalance(2));
                $("#otherBalance").text(getBalance(3));
            }
            else {
                $("#totalBalance, #casualBalance, #sickBalance, #otherBalance")
                    .text("0 Days");
            }
        }
        catch (e) {
            console.error("Error loading leave balances:", e);
            $("#totalBalance, #casualBalance, #sickBalance, #otherBalance")
                .text("0 Days");
        }
    }

    // Apply leave click - NOW ASYNC
    document.getElementById("btnApplyLeave").addEventListener("click", async function () {
        const fromDate = document.getElementById("fromDate").value;
        const toDate = document.getElementById("toDate").value;
        const totalDays = document.getElementById("totalDays").value;
        const leaveType = document.getElementById("leaveType").value;
        const attachment = document.getElementById("attfile").files[0];
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

        // Load both at the same time (FAST)
        const [balance, pending] = await Promise.all([
            GetLeaveBalances(),
            CheckPendingLeave()
        ]);

        // Pending leave validation
        if (pending.data) {
            showToast("You already have a pending leave.", "error");
            return;
        }

        // Balance validation
        const details = balance.data?.leaveDetails || [];
        const item = details.find(x => x.leaveTypeId == leaveType);

        if (!item || item.closingBalance <= 0) {
            showToast("No leave balance available.", "error");
            return;
        }

        if (parseFloat(totalDays) > item.closingBalance) {
            showToast("Not enough leave balance. Available: " + item.closingBalance + " days.", "error");
            return;
        }

        // ========= APPLY LEAVE =========
        const formData = new FormData();
        formData.append("LeaveTypeId", leaveType);
        formData.append("FromDate", fromDate);
        formData.append("ToDate", toDate);
        formData.append("TotalDays", totalDays);
        formData.append("Reason", reason);
        if (attachment) {
            formData.append("Attachment", attachment);
        }

        $('.loader').removeClass('hide');
        $.ajax({
            url: "/Payroll/ApplyLeave",
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
            success: function (res) {
                $('.loader').addClass('hide');
                if (res.status) {
                    showToast("Leave applied successfully.", "success");
                    loadLeaveBalances();

                    // Close modal after short delay
                    setTimeout(() => {
                        const modal = bootstrap.Modal.getInstance(
                            document.getElementById("applyLeaveModal")
                        );
                        modal.hide();
                        document.getElementById("applyLeaveForm").reset();
                        loadLeaveRequests();
                        showMsg("");
                    }, 1200);
                } else {
                    showToast("Error applying leave: " + res.message, "error");
                }
            }
        });
    });

    loadLeaveBalances();

})(); // END ASYNC WRAPPER

function loadLeaveRequests() {
    $.ajax({
        url: "/Payroll/GetLeaveRequests",
        type: "GET",
        success: function (res) {

            const tbody = $("#leavesTable tbody");
            tbody.empty(); // Clear old rows

            if (!res.status || !res.data || res.data.length === 0) {
                tbody.append(`
                    <tr>
                        <td colspan="7" class="text-muted text-center">No data available</td>
                    </tr>
                `);
                return;
            }

            let rows = "";

            res.data.forEach((item, index) => {

                const fromDate = formatDate(item.fromDate);
                const toDate = formatDate(item.toDate);
                const appliedOn = formatDate(item.createdOn);

                const statusBadge = getStatusBadge(item.status);

                rows += `
                    <tr>
                        <td>${index + 1}</td>
                        <td>${item.leaveType || "NA"}</td>
                        <td>
                            ${(fromDate || "NA")} — ${(toDate || "NA")}
                        </td>
                        <td>${item.totalDays || "NA"}</td>
                        <td>${item.reason || "NA"}</td>
                        <td>
                            ${item.attachment
                        ? `<a href="${item.attachment}" target="_blank" class="text-primary">View</a>`
                        : "NA"
                    }
                        </td>
                        <td>${appliedOn || "NA"}</td>
                        <td>
                            ${statusBadge}

                            ${item.status === "Rejected" && item.remarks
                                                ? `<br/><small class="text-danger"><b>Reason:</b> ${item.remarks}</small>`
                                                : ""
                            }
                        </td>
                    </tr>
                `;

            });

            tbody.append(rows);
        }
    });
}

function getStatusBadge(status) {
    if (!status) return "-";

    status = status.toLowerCase();

    if (status === "approved")
        return `<span class="badge bg-success">Approved</span>`;

    if (status === "pending")
        return `<span class="badge bg-warning text-dark">Pending</span>`;

    if (status === "rejected")
        return `<span class="badge bg-danger">Rejected</span>`;

    return `<span class="badge bg-secondary">${status}</span>`;
}

function formatDate(dateString) {
    if (!dateString) return "-";
    const d = new Date(dateString);
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}-${month}-${year}`;
}

// -----------------------
// Leave Approvals
// -----------------------

function loadPendingApprovals() {
    $.ajax({
        url: "/Payroll/GetPendingApprovals",
        type: "GET",
        success: function (res) {

            const tbody = $("#approvalBody");
            tbody.empty();

            if (!res.status || res.data.length === 0) {
                tbody.append(`<tr><td colspan="9" class="text-muted text-center">No pending leaves</td></tr>`);
                return;
            }

            res.data.forEach((item, index) => {
                const fromDate = formatDate(item.fromDate);
                const toDate = formatDate(item.toDate);
                const appliedOn = formatDate(item.createdOn);

                tbody.append(`
                    <tr>
                        <td>${index + 1}</td>
                        <td>${item.employeeName}</td>
                        <td>${item.leaveType}</td>
                        <td>${fromDate} — ${toDate}</td>
                        <td>${item.totalDays}</td>
                        <td>${item.reason || "NA"}</td>
                        <td>${item.attachment ? `<a href="${item.attachment}" target="_blank">View</a>` : "NA"}</td>
                        <td>${appliedOn}</td>
                       <td>
                            <button class="btn btn-success btn-sm approveBtn" data-id="${item.leaveId}">
                                Approve
                            </button>

                            <button class="btn btn-danger btn-sm rejectBtn" data-id="${item.leaveId}">
                                Reject
                            </button>
                       </td>
                    </tr>
                `);
            });
        }
    });
}

//Approve / Reject Button Functions

$(document).on("click", ".approveBtn", function () {
    const leaveId = $(this).data("id");

    processLeave(leaveId, "Approve", "");
});

$("#btnSubmitReject").on("click", function () {
    const leaveId = $("#rejectLeaveId").val();
    const remark = $("#rejectRemark").val().trim();

    if (!remark) {
        showToast("Please enter a remark for rejection.", "error");
        return;
    }

    processLeave(leaveId, "Reject", remark);
    bootstrap.Modal.getInstance(document.getElementById("rejectModal")).hide();
});

// Open Reject Modal
$(document).on("click", ".rejectBtn", function () {
    const leaveId = $(this).data("id");

    $("#rejectLeaveId").val(leaveId);
    $("#rejectRemark").val("");

    new bootstrap.Modal(document.getElementById("rejectModal")).show();
});

function processLeave(leaveId, action, remark) {
    
    const formData = new FormData();
    formData.append("LeaveId", leaveId);
    formData.append("Action", action);
    formData.append("Remark", remark);

    $('.loader').removeClass('hide');
    $.ajax({
        url: "/Payroll/LeaveApproveProcess",
        type: "POST",
        data: formData,
        contentType: false,
        processData: false,
        success: function (res) {
            $('.loader').addClass('hide');
            if (res.status) {
                showToast("Leave " + action + "d successfully!", "success");
                loadPendingApprovals();    // reload manager/TL table
                loadLeaveRequests();        // reload employee leave list
            } else {
                showToast("Error: " + res.message, "error");
            }
        }
    });
}

