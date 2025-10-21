document.addEventListener("DOMContentLoaded", function () {
    var calendarEl = document.getElementById("calendar");

    // Fetch attendance data from API
    fetch('/MarkAttendance/GetAttendanceCalendar')
        .then(response => response.json())
        .then(json => {
            if (!json.status || !json.data) {
                alert(json.message || "No calendar data");
                return;
            }

            // Map API data to FullCalendar events
            var events = json.data.map(x => {
                let backgroundColor = "";
                let textColor = "#fff"; // white text for contrast

                switch (x.status) {
                    case "Present":
                        backgroundColor = "#28a745"; // Bootstrap green
                        break;
                    case "Absent":
                        backgroundColor = "#dc3545"; // Bootstrap red
                        break;
                    default:
                        backgroundColor = "#007bff"; // Bootstrap blue
                }

                return {
                    title: x.status,
                    start: x.attendanceDate,
                    allDay: true,
                    backgroundColor: backgroundColor,
                    textColor: textColor,
                    extendedProps: { remarks: x.remarks }
                };
            });

            var calendar = new FullCalendar.Calendar(calendarEl, {
                initialView: 'dayGridMonth',
                editable: false,
                selectable: false,
                events: events, // bind mapped events here
                eventDidMount: function (info) {
                    // Tooltip to show remarks
                    if (info.event.extendedProps.remarks) {
                        info.el.setAttribute('title', info.event.extendedProps.remarks);
                    }
                }
            });

            calendar.render();
        })
        .catch(err => console.error('Error loading calendar data:', err));
});
