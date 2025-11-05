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

    let table;

    initAssignEmployeeTable();

    // Export buttons
    $('#exportCsv').on('click', function () {
        table.button('.buttons-csv').trigger();
    });

    $('#exportPdf').on('click', function () {
        table.button('.buttons-pdf').trigger();
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
    $('.loader').removeClass('hide');
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
            $('.loader').addClass('hide');
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

$(document).on('change', '.emp-checkbox', function () {
    const selectedCount = $('.emp-checkbox:checked').length;
    if (selectedCount > 0) {
        $('#empDropdownBtn').css('border-color', ''); // remove red border
    }
});

function initAssignEmployeeTable() {
    $('.loader').removeClass('hide');
    var table = $('#assignEmpTable').DataTable({      
        serverSide: true,
        responsive: true,
        ajax: {
            url: '/Assign/GetAssignHierarchyList',
            type: 'GET',
            data: function (d) {
                const sortColumnIndex = d.order && d.order.length ? d.order[0].column : 3;
                const sortColumn = d.columns[sortColumnIndex] ? d.columns[sortColumnIndex].data : 'EmployeeName';
                const sortDir = d.order && d.order.length ? d.order[0].dir : 'asc';

                return JSON.stringify({
                    Start: d.start,
                    Length: d.length,
                    Search: d.search.value,
                    SortColumn: sortColumn,
                    SortDirection: sortDir
                });
            },
            dataSrc: function (json) {
                $('.loader').addClass('hide');
                // adapt this depending on your API shape
                return (json && json.data) ? json.data : json;

            }
        },
        columns: [
            { data: 'slNo', title: 'Sl. No.' },
            { data: 'department', title: 'Department' },
            { data: 'subDepartment', title: 'SubDepartment' },
            { data: 'employeeName', title: 'Employee Name' },
            { data: 'teamLead', title: 'Team Lead' },
            { data: 'manager', title: 'Manager' }
        ]
    });

    // run merging after each draw
    $('#assignEmpTable').on('draw.dt', function () {
        // columns to group: Department (1), SubDepartment (2), TeamLead (4), Manager (5)
        mergeHierarchicalColumns('#assignEmpTable', {
            dept: 1,
            subdept: 2,
            lead: 4,
            manager: 5
        });
    });
}

function mergeHierarchicalColumns(tableSelector, cols) {
    var table = $(tableSelector).DataTable();
    var rows = table.rows({ page: 'current' }).nodes();

    // trackers per named column
    var trackers = {};
    for (var key in cols) {
        trackers[key] = {
            lastValue: null,
            lastCell: null,
            rowspan: 1
        };
    }

    // Helper to reset child trackers when a parent changes
    function resetChildren(startingKey) {
        var started = false;
        for (var k in cols) {
            if (k === startingKey) {
                started = true;
                continue;
            }
            if (started) {
                trackers[k].lastValue = null;
                trackers[k].lastCell = null;
                trackers[k].rowspan = 1;
            }
        }
    }

    // Iterate each row once, and process each column in hierarchical order
    $(rows).each(function () {
        // First, read all current cell values for this row for our columns
        var currentValues = {};
        for (var k in cols) {
            var ci = cols[k];
            var $cell = $('td', this).eq(ci);
            currentValues[k] = {
                val: $cell.text().trim(),
                $cell: $cell
            };
        }

        // Now process in the same order as keys of cols (which should be parent->child)
        // Note: JS object key order is stable for insertion order; define cols accordingly.
        var started = false;
        for (var k in cols) {
            var item = currentValues[k];
            var val = item.val;
            var $cell = item.$cell;

            // If a parent earlier changed, reset children before comparing (hierarchical reset)
            // We detect parent change by comparing value of the immediate parent key (we handled by resetting in code below)
            // Compare with tracked lastValue for this key
            if (trackers[k].lastValue === val && val !== '') {
                // same as previous => increment rowspan and remove current cell
                trackers[k].rowspan++;
                $cell.remove();
                if (trackers[k].lastCell) {
                    trackers[k].lastCell.attr('rowspan', trackers[k].rowspan);
                    trackers[k].lastCell.css({
                        'vertical-align': 'middle',
                        'border': '1px solid #dee2e6',
                        'background-color': '#fff'
                    });
                }
            } else {
                // value changed for this key -> we must reset all descendants of this key
                resetChildren(k);

                // set trackers for this key
                trackers[k].lastValue = val;
                trackers[k].lastCell = $cell;
                trackers[k].rowspan = 1;

                // style the cell (keeps borders visible)
                $cell.css({
                    'vertical-align': 'middle',
                    'border': '1px solid #dee2e6'
                });
            }
        }
    });
}




