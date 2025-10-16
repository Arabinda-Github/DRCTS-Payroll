
// initialize dropdown instance variable
let dropdownInstance = null;

function initDropdown() {
    const dropdownElement = document.getElementById('notificationBell');
    if (!dropdownElement) return;
    dropdownInstance = new bootstrap.Dropdown(dropdownElement, {
        autoClose: false // Prevent auto-close
    });
}

function showDropdown() {
    if (dropdownInstance) {
        dropdownInstance.show();
    }
}

function hideDropdown() {
    if (dropdownInstance) {
        dropdownInstance.hide();
    }
}

// initialize state using user object passed or globalUserData
function initializeState(user) {
    user = user || window.globalUserData || {};
    const role = (user.role || "").toString().toLowerCase();

    if (role === 'employee') {
        showDropdown();
    } else {
        hideDropdown();
    }
}

// 1) Primary: wait for cookie data loaded event
$(document).on("cookieDataLoaded", function () {
    const user = window.globalUserData || {};
    initDropdown();
    initializeState(user);
});

// 2) Fallback: if cookie already loaded before this file runs, run immediately
$(function () {
    // small timeout to ensure bootstrap/js ready
    setTimeout(function () {
        if (window.globalUserData && Object.keys(window.globalUserData).length > 0) {
            initDropdown();
            initializeState(window.globalUserData);
        }
    }, 50);
});


let isCheckedIn = false;
let attendanceSeconds = 0;
let attendanceTimerInterval = null;

// When Tap button is clicked
$("#tapBtn").on("click", async function () {
    const user = window.globalUserData || {};

    if (user.userid) {
        if (!isCheckedIn) {
            await punchIn();
        } else {
            await punchOut();
        }
    } else {
        showToast("Please log in to mark attendance.", "error");

        const interval = setInterval(() => {
            if (window.globalUserData && window.globalUserData.role) {
                initializeState(window.globalUserData);
                initDropdown();
                clearInterval(interval);
            }
        }, 200);

        setTimeout(() => window.location.href = "/Home/Logout", 1000);
    }
});

async function punchIn() {
    const user = window.globalUserData || {};
    const { latitude, longitude } = await getCurrentLocation();
    const ip = await getIpAddress();
    const address = await getAddressFromCoords(latitude, longitude); // ✅ Get area name

    const data = {
        EmployeeID: user.empid,
        Latitude: latitude,
        Longitude: longitude,
        Location: address,
        Address: address,
        IPAddress: ip,
        DeviceInfo: navigator.userAgent,
        Remarks: "Check-In from web",
        ModifiedBy: user.name || "System"
    };

    // Convert JSON to FormData
    const formData = new FormData();
    for (const key in data) {
        if (data.hasOwnProperty(key)) {
            formData.append(key, data[key]);
        }
    }

    $.ajax({
        url: '/MarkAttendance/ProcessCheckin',
        type: 'POST',
        data: formData,
        contentType: false,   
        processData: false, 
        success: function (response) {
            if (response.status) {

                $('#clockInTime').text(convertTo12HourFormat(response.data[0].checkInDetails.checkInTime));
                $('#totalHours').text("--.--");
                updateUIForPunchedIn();
                isCheckedIn = true;
                attendanceSeconds = 0;
                startTimer(0);
                showToast(response.message, "success");
            } else {
                showToast(response.message, "error");
            }
            // Keep dropdown open
            setTimeout(() => showDropdown(), 10);
        },
        error: function (xhr) {
            showToast("Error: " + xhr.responseText, "error");
        }
    });
}

async function punchOut() {
    const user = window.globalUserData || {};

    const { latitude, longitude } = await getCurrentLocation();
    const ip = await getIpAddress();
    const address = await getAddressFromCoords(latitude, longitude);

    const data = {
        EmployeeID: user.empid,
        Latitude: latitude,
        Longitude: longitude,
        Location: address,
        Address: address,
        IPAddress: ip,
        DeviceInfo: navigator.userAgent,
        Remarks: "Check-Out from web",
        ModifiedBy: user.name || "System"
    };

    // Convert JSON to FormData
    const formData = new FormData();
    for (const key in data) {
        if (data.hasOwnProperty(key)) {
            formData.append(key, data[key]);
        }
    }

    $.ajax({
        url: '/MarkAttendance/ProcessCheckout',
        type: 'POST',
        data: formData,
        contentType: false,
        processData: false, 
        success: function (response) {
            if (response.status) {
                $('#clockOutTime').text(convertTo12HourFormat(response.data[0].checkOutDetails.checkOutTime));
                stopTimer(); 
                const duration = attendanceSeconds;
                const formattedDuration = formatDuration(duration);
                $('#totalHours').text(response.workingHours || formattedDuration);
                updateUIForPunchedOut();
                isCheckedIn = false;
                showToast(response.message, "success");
            } else {
                showToast(response.message, "error");
            }
            // Keep dropdown open
            setTimeout(() => showDropdown(), 10);
        },
        error: function (xhr) {
            showToast("Error: " + xhr.responseText, "error");
        }
    });
}

// Get public IP address
async function getIpAddress() {
    try {
        const response = await fetch("https://api.ipify.org?format=json");
        const data = await response.json();
        return data.ip;
    } catch {
        return "Unavailable";
    }
}

// Get current location (latitude, longitude)
function getCurrentLocation() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject("Geolocation not supported by your browser.");
            return;
        }

        navigator.geolocation.getCurrentPosition(
            (position) => {
                resolve({
                    latitude: position.coords.latitude,
                    longitude: position.coords.longitude
                });
            },
            (error) => {
                let message = "";
                switch (error.code) {
                    case error.PERMISSION_DENIED:
                        message = "Location permission denied. Please enable GPS access in your browser.";
                        break;
                    case error.POSITION_UNAVAILABLE:
                        message = "Location unavailable. Please check your device GPS or internet connection.";
                        break;
                    case error.TIMEOUT:
                        message = "Request timed out while fetching location. Please try again.";
                        break;
                    default:
                        message = "An unknown error occurred while getting location.";
                        break;
                }
                reject(message);
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            }
        );
    });
}

// Get readable address from coordinates
async function getAddressFromCoords(latitude, longitude) {
    try {
        const response = await fetch(
            `https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json`
        );

        if (!response.ok) throw new Error("Failed to fetch location data.");

        const data = await response.json();

        // You can choose what to extract:
        const address = data.display_name || "Unknown Area";
        return address;
    } catch (error) {
        console.error("Error fetching address:", error);
        return "Unknown Area";
    }
}

function updateUIForPunchedIn() {
    $('#statusBtn').removeClass('offline-btn').addClass('online-btn')
        .html('<span class="status-indicator status-green"></span>ONLINE');

    $('#tapBtn').addClass('punch-out');
    $('#tapBtnText').text('Punch Out');
    $('.tap-icon').text('✋');
}

function updateUIForPunchedOut() {
    $('#statusBtn').removeClass('online-btn').addClass('offline-btn')
        .html('<span class="status-indicator status-red"></span>OFFLINE');

    $('#tapBtn').removeClass('punch-out');
    $('#tapBtnText').text('Punch In');
    $('.tap-icon').text('👆');
    $('#timerBtn').text('00:00:00');
}

function startTimer(startSeconds = 0) {
    attendanceSeconds = startSeconds;

    if (attendanceTimerInterval) {
        clearInterval(attendanceState.timerInterval);
    }

    attendanceTimerInterval = setInterval(function () {
        attendanceSeconds++;
        const timeString = formatDuration(attendanceSeconds);
        $('#timerBtn').text(timeString);

        //if (attendanceSeconds % 60 === 0) {
        //    saveState();
        //}
    }, 1000);
}

function stopTimer() {
    if (attendanceTimerInterval) {
        clearInterval(attendanceTimerInterval);
        attendanceTimerInterval = null;
    }
}

function formatTime(date) {
    let hours = date.getHours();
    const minutes = date.getMinutes().toString().padStart(2, '0');
    const ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12 || 12;
    return hours + ':' + minutes + ' ' + ampm;
}
function convertTo12HourFormat(timeStr) {
    const [hoursStr, minutes, secondsFraction] = timeStr.split(':');
    let hours = parseInt(hoursStr, 10);
    const ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12 || 12;
    const hours12 = hours.toString().padStart(2, '0');

    return `${hours12}:${minutes} ${ampm}`;
}

function formatDuration(seconds) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    return String(hours).padStart(2, '0') + ':' +
        String(minutes).padStart(2, '0') + ':' +
        String(secs).padStart(2, '0');
}
