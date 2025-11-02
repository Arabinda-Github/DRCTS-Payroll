document.addEventListener("DOMContentLoaded", function () {
    const dropdown = document.getElementById("employeeDropdown");
    const button = document.getElementById("empDropdownBtn");
    const list = document.getElementById("empList");
    const text = document.getElementById("empSelected");

    // Toggle dropdown visibility
    button.addEventListener("click", function (e) {
        e.stopPropagation(); // Prevent bubbling to document click
        dropdown.classList.toggle("open");
    });

    // Delegate checkbox change listener (for dynamic content)
    list.addEventListener("change", function () {
        const selected = Array.from(list.querySelectorAll("input[type='checkbox']:checked"))
            .map(cb => cb.parentNode.textContent.trim());
        text.textContent = selected.length > 0
            ? selected.join(", ")
            : "-- Select Employee --";
    });

    // Close dropdown when clicking outside
    document.addEventListener("click", function (e) {
        if (!dropdown.contains(e.target)) {
            dropdown.classList.remove("open");
        }
    });
});

$(function () {
    loadDepartment();
    loadSubDepartments(0);
    loadBranchWiseUsers(0);
    $('#dept').on('change', function () {
        const deptId = $(this).val();
        loadSubDepartments(deptId);
    });
    $('#subDept').on('change', function () {
        const subDeptId = $(this).val();
        if (subDeptId) {
            loadBranchWiseUsers(subDeptId);
        } else {
            clearUserFields();
        }
    });
});

function loadDepartment() {
    $.ajax({
        url: '/Assign/GetDepartments',
        type: 'GET',
        success: function (response) {
            if (response.status && response.data) {
                $('#dept').empty().append('<option value="">-- Select Department --</option>');
                $.each(response.data, function (index, dept) {
                    $('#dept').append(
                        $('<option></option>').val(dept.departmentId).text(dept.departmentName)
                    );
                });
            } else {
                console.warn('No departments found:', response.message);
            }
        },
        error: function (xhr) {
            console.error('Error fetching departments:', xhr.responseText);
        }
    });
}

function loadSubDepartments(departmentId) {
    $('#subDept').empty().append('<option value="">-- Select Sub Department --</option>');

    $.ajax({
        url: '/Assign/GetSubDepartments?departmentId=' + departmentId,
        type: 'GET',
        success: function (response) {
            if (response.status && response.data) {
                $.each(response.data, function (index, sub) {
                    $('#subDept').append(
                        $('<option></option>').val(sub.subDepartmentId).text(sub.subDepartmentName)
                    );
                });
            }
        },
        error: function (xhr) {
            console.error('Error loading sub-departments:', xhr.responseText);
        }
    });
}

function loadBranchWiseUsers(subDepartmentId) {
    $.ajax({
        url: '/Assign/GetEmployeeBySubDept?subDepartmentId=' + subDepartmentId,
        type: 'GET',
        success: function (response) {
            if (response.status && response.data) {
                const users = response.data;

                // Clear existing options
                $('#managerSelect').empty().append('<option value="">-- Select Manager --</option>');
                $('#teamleadSelect').empty().append('<option value="">-- Select Team Lead --</option>');
                $('#empList').empty();

                // Populate dropdowns
                $.each(users, function (index, user) {
                    const option = $('<option></option>').val(user.employeeID).text(user.employeeName);

                    if (user.role === 'Manager') {
                        $('#managerSelect').append(option);
                    } else if (user.role === 'Team Lead') {
                        $('#teamleadSelect').append(option);
                    } else if (user.role === 'Employee') {
                        const checkbox = `
                                <label class="dropdown-item">
                                    <input type="checkbox" class="emp-checkbox" value="${user.employeeID}"> ${user.employeeName}
                                </label>`;
                        $('#empList').append(checkbox);
                    }
                });
            } else {
                console.warn('No users found:', response.message);
                clearUserFields();
            }
        },
        error: function (xhr) {
            console.error('Error loading users:', xhr.responseText);
            clearUserFields();
        }
    });
}

function clearUserFields() {
    $('#managerSelect').empty().append('<option value="">-- Select Manager --</option>');
    $('#teamleadSelect').empty().append('<option value="">-- Select Team Lead --</option>');
    $('#empList').empty();
}

$('#btnAssign').on('click', function () {
    // Reset validation visuals
    $('#dept, #subDept, #managerSelect, #teamleadSelect, #empDropdownBtn').css('border-color', '');

    const departmentId = $('#dept').val();
    const subDepartmentId = $('#subDept').val();
    const managerId = $('#managerSelect').val();
    const teamLeadId = $('#teamleadSelect').val();
    const employeeIds = getSelectedEmployeeIds();
    const remarks = $('#remarks').val();

    let errors = [];

    if (!departmentId) {
        errors.push("Please select a Department.");
        $('#dept').css('border-color', '#ef4d56');
    }
    if (!subDepartmentId) {
        errors.push("Please select a Sub Department.");
        $('#subDept').css('border-color', '#ef4d56');
    }
    if (!managerId) {
        errors.push("Please select a Manager.");
        $('#managerSelect').css('border-color', '#ef4d56');
    }
    if (!teamLeadId) {
        errors.push("Please select a Team Lead.");
        $('#teamleadSelect').css('border-color', '#ef4d56');
    }
    if (!employeeIds.length) {
        errors.push("Please select at least one Employee.");
        $('#empDropdownBtn').css('border-color', '#ef4d56');
    }

    if (errors.length > 0) {
        showToast(errors.join('\n'), "error");
        return;
    }

    // ✅ Prepare form data correctly
    const formData = new FormData();
    formData.append('DepartmentId', departmentId);
    formData.append('SubDepartmentId', subDepartmentId);
    formData.append('ManagerId', managerId);
    formData.append('TeamLeadId', teamLeadId || '');
    formData.append('EmployeeId', employeeIds.join(','));
    formData.append('Remarks', remarks);

    $.ajax({
        url: '/Assign/AssignHierarchy',
        type: 'POST',
        data: formData,
        processData: false,
        contentType: false,
        beforeSend: function () {
            $('#btnAssign').prop('disabled', true).text('Assigning...');
        },
        success: function (response) {
            if (response.status) {
                showToast(response.message || 'Assigned successfully', "success");
                setTimeout(() => window.location.reload(), 1000);
            } else {
                showToast(response.message || 'Assignment failed', "error");
            }
        },
        error: function (xhr) {
            showToast(`Error: ${xhr.responseText}`, "error");
        },
        complete: function () {
            $('#btnAssign').prop('disabled', false).text('Assign');
        }
    });
});

function getSelectedEmployeeIds() {
    return $('.emp-checkbox:checked').map(function () {
        return $(this).val();
    }).get(); // ✅ returns an array of all selected IDs
}

function getClientIp() {
    // Optional fallback; real IP is set on the server side
    return '0.0.0.0';
}

$(document).on('change', '.emp-checkbox', function () {
    const selectedCount = $('.emp-checkbox:checked').length;
    if (selectedCount > 0) {
        $('#empDropdownBtn').css('border-color', ''); // remove red border
    }
});

