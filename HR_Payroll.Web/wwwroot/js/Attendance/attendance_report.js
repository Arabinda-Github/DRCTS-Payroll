let table;

$(document).ready(function () {
    initAttendanceTable();

    // Filter button click
    $('#filterBtn').on('click', function () {
        table.ajax.reload();
    });

    // Export buttons
    $('#exportCsv').on('click', function () {
        table.button('.buttons-csv').trigger();
    });

    $('#exportPdf').on('click', function () {
        table.button('.buttons-pdf').trigger();
    });
});

function initAttendanceTable() {
    table = $('#attn_datatable').DataTable({
        processing: true,
        serverSide: true,
        responsive: true,
        ajax: {
            url: '/MarkAttendance/GetAttendanceReport',
            type: 'POST',
            contentType: 'application/x-www-form-urlencoded; charset=UTF-8',
            data: function (d) {
                return $.param({
                    start: d.start,           // Starting record (0, 10, 20...)
                    length: d.length,         // Page size (10, 25, 50...)
                    fromDate: $('#fromDate').val() || '',
                    toDate: $('#toDate').val() || ''
                });
            },
            dataSrc: function (json) {

                if (!json.status || !json.data) {
                    alert(json.message || "No data available");
                    return [];
                }

                // Set total records for pagination
                json.recordsTotal = json.data.totalCount || 0;
                json.recordsFiltered = json.data.totalCount || 0;

                return json.data.records || [];
            },
            beforeSend: function () {
                $('.loader').removeClass('hide');
            },
            complete: function () {
                $('.loader').addClass('hide');
            },
            error: function (xhr, error, thrown) {
                $('.loader').addClass('hide');
                console.error('DataTable Error:', xhr.responseText);
                alert('Failed to load attendance data. Please try again.');
            }
        },
        columns: [
            {
                data: 'attendanceDate',
                title: 'Date',
                render: function (d) {
                    if (!d) return '-';
                    const date = new Date(d);
                    return date.toLocaleDateString('en-GB'); // DD/MM/YYYY
                }
            },
            {
                data: 'dayName',
                title: 'Day',
                render: function (d) {
                    return d || '-';
                }
            },
            {
                data: 'isWeekend',
                title: 'Weekend',
                render: function (d) {
                    return d === 'Y' || d === true ? 'Yes' : 'No';
                },
                className: 'text-center'
            },
            {
                data: 'shiftName',
                title: 'Shift',
                render: function (d) {
                    return d || 'N/A';
                }
            },
            {
                data: 'checkInTime',
                title: 'Check-In',
                render: function (d) {
                    return d || '-';
                },
                className: 'text-center'
            },
            {
                data: 'checkOutTime',
                title: 'Check-Out',
                render: function (d) {
                    return d || '-';
                },
                className: 'text-center'
            },
            {
                data: 'workingHoursFormatted',
                title: 'Hours Worked',
                render: function (d) {
                    return d || '-';
                },
                className: 'text-center'
            },
            {
                data: 'status',
                title: 'Status',
                render: function (d, type, row) {
                    let color = 'gray';
                    if (d === 'Present') color = 'green';
                    else if (d === 'Absent') color = 'red';
                    else if (d === 'Weekend') color = 'blue';

                    let title = `Shift: ${row.shiftName || '-'}\nRemarks: ${row.remarks || '-'}`;

                    return `<span style="color:${color}; font-weight: bold;" title="${title}">${d || 'N/A'}</span>`;
                },
                className: 'text-center'
            }
        ],
        lengthMenu: [[10, 25, 50, 100, -1], [10, 25, 50, 100, "All"]],
        pageLength: 50,
        order: [[0, 'desc']], // Sort by date descending
        dom: '<"row mb-2"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>rt<"row mt-2"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>B',
        buttons: [
            {
                extend: 'csv',
                text: 'CSV',
                className: 'buttons-csv d-none',
                titleAttr: 'Export CSV',
                exportOptions: {
                    columns: ':visible'
                }
            },
            {
                extend: 'pdf',
                text: 'PDF',
                className: 'buttons-pdf d-none',
                titleAttr: 'Export PDF',
                orientation: 'landscape',
                exportOptions: {
                    columns: ':visible'
                }
            }
        ],
        language: {
            emptyTable: "No attendance records found",
            zeroRecords: "No matching records found",
            search: "Search:",
            paginate: {
                first: "First",
                last: "Last",
                next: "Next",
                previous: "Previous"
            }
        }
    });
}