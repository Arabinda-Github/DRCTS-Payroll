// ========================================
// STATE MANAGEMENT WITH LOCAL STORAGE
// ========================================
const STORAGE_KEY = 'attendance_state';
const CACHE_DURATION = 5 * 60 * 1000; // 5 minutes for IP cache
const STATUS_SYNC_INTERVAL = 5 * 60 * 1000; // 5 minutes
const STATE_EXPIRY = 12 * 60 * 60 * 1000; // 12 hours

let dropdownInstance = null;
let isCheckedIn = false;
let attendanceSeconds = 0;
let attendanceTimerInterval = null;
let statusSyncInterval = null;
let isLoadingStatus = false;
let cachedIP = null;
let ipCacheTime = null;
let lastFocusTime = Date.now();

$(function () {
    updateDateTimeBadge();
    setInterval(updateDateTimeBadge, 60000);
});

function updateDateTimeBadge() {
    const now = new Date();

    // Format: DD-MM-YYYY
    const dateFormatted =
        String(now.getDate()).padStart(2, '0') + "-" +
        String(now.getMonth() + 1).padStart(2, '0') + "-" +
        now.getFullYear();

    // Format time: HH:MM AM/PM
    let hours = now.getHours();
    const minutes = String(now.getMinutes()).padStart(2, '0');
    const ampm = hours >= 12 ? 'PM' : 'AM';

    hours = hours % 12;
    hours = hours ? hours : 12; // 0 → 12

    const timeFormatted = `${String(hours).padStart(2, '0')}:${minutes} ${ampm}`;

    $("#currentDate").text(dateFormatted);
    $("#currentTime").text(timeFormatted);
}

// ========================================
// LOCAL STORAGE FUNCTIONS
// ========================================

function saveState() {
    try {
        const state = {
            isCheckedIn,
            attendanceSeconds,
            checkInTime: isCheckedIn ? Date.now() - (attendanceSeconds * 1000) : null,
            lastUpdated: Date.now()
        };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch (error) {
        console.error('Failed to save state:', error);
    }
}

function saveStateWithTimestamp(checkInTimestamp) {
    try {
        const state = {
            isCheckedIn: true,
            attendanceSeconds: attendanceSeconds,
            checkInTime: checkInTimestamp,
            lastUpdated: Date.now()
        };
        localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
    } catch (error) {
        console.error('Failed to save state:', error);
    }
}

function loadState() {
    try {
        const stored = localStorage.getItem(STORAGE_KEY);
        if (!stored) return null;

        const state = JSON.parse(stored);
        const timeDiff = Date.now() - state.lastUpdated;

        // Invalidate state if older than 12 hours
        if (timeDiff > STATE_EXPIRY) {
            localStorage.removeItem(STORAGE_KEY);
            return null;
        }

        return state;
    } catch (error) {
        console.error('Failed to load state:', error);
        return null;
    }
}

function clearState() {
    try {
        localStorage.removeItem(STORAGE_KEY);
    } catch (error) {
        console.error('Failed to clear state:', error);
    }
}

// ========================================
// DROPDOWN MANAGEMENT
// ========================================

function initDropdown() {
    const dropdownElement = document.getElementById('notificationBell');
    if (!dropdownElement) return;

    dropdownInstance = new bootstrap.Dropdown(dropdownElement, {
        autoClose: false
    });
}

function showDropdown() {
    if (dropdownInstance) {
        dropdownInstance.show();
    }
    // Show the alert badge
    const badge = document.getElementById("alert-clock-badge");
    if (badge) {
        badge.style.display = "inline-block";
    }
}

function hideDropdown() {
    if (dropdownInstance) {
        dropdownInstance.hide();
    }
    // Hide the alert badge
    const badge = document.getElementById("alert-clock-badge");
    if (badge) {
        badge.style.display = "none";
    }
}

// ========================================
// ATTENDANCE STATUS LOADING & SYNC
// ========================================

async function loadAttendanceStatus() {
    const user = window.globalUserData || {};

    if (!user.empid) {
        showToast('No employee ID available','error');
        return;
    }

    if (isLoadingStatus) {
        console.log('Status load already in progress');
        return;
    }

    isLoadingStatus = true;
    $('.loader').removeClass('hide');
    try {
        const response = await $.ajax({
            url: '/MarkAttendance/GetCurrentStatus',
            type: 'GET',
            cache: false,
            timeout: 10000
        });

        handleAttendanceStatusResponse(response);

    } catch (error) {
        console.error('Error loading attendance status:', error);
        handleStatusLoadFailure();
    } finally {
        isLoadingStatus = false;
    }
}

function handleAttendanceStatusResponse(response) {
    const today = new Date();
    const isWeekend = today.getDay() === 0 || today.getDay() === 6;
    $('.loader').addClass('hide');
    if (!response || !response.status || !response.data) {
        //showToast('No valid attendance data received','error');
        resetAttendanceUI();
        if (!isWeekend) {
            showDropdown();
        } else {
            hideDropdown(); 
        }
        return;
    }

    const data = response.data;
    const status = data.currentStatus || 'NOT_CHECKED_IN';

    console.log('Received status:', status, data);

    switch (status) {
        case 'CHECKED_IN':
            restoreCheckedInState(data);
            break;
        case 'CHECKED_OUT':
            restoreCompletedState(data);
            break;
        case 'NOT_CHECKED_IN':
            showDropdown();
            break;
        default:
            resetAttendanceUI();
            break;
    }
}

function restoreCheckedInState(data) {
    isCheckedIn = true;
    attendanceSeconds = data.elapsedSeconds || 0;

    // Update time displays
    if (data.checkInTime) {       
        $('#clockInTime').text(convertTo12HourFormat(data.checkInTime));
        hideDropdown();
    }
    $('#clockOutTime').text('--:--');
    $('#totalHours').text(formatWorkingHours(data.workingHours || 0));

    // Update UI elements
    updateUIForPunchedIn();

    // Start timer from elapsed seconds
    startTimer(attendanceSeconds);

    // Save state to localStorage
    const checkInTimestamp = Date.now() - (attendanceSeconds * 1000);
    saveStateWithTimestamp(checkInTimestamp);

    // Start periodic sync
    startStatusSync();

    console.log('✓ Restored check-in status:', {
        checkInTime: data.checkInTime,
        elapsedSeconds: attendanceSeconds,
        workingHours: data.workingHours
    });
}

function restoreCompletedState(data) {
    isCheckedIn = false;

    // Update time displays
    if (data.checkInTime) {
        hideDropdown();
        $('#clockInTime').text(convertTo12HourFormat(data.checkInTime));
    }
    if (data.checkOutTime) {
        $('#clockOutTime').text(convertTo12HourFormat(data.checkOutTime));
    }
    $('#totalHours').text(formatWorkingHours(data.workingHours || 0));

    // Update UI
    updateUIForPunchedOut();

    // Clear timer and state
    stopTimer();
    clearState();
    stopStatusSync();

    console.log('✓ Attendance completed for today:', {
        checkInTime: data.checkInTime,
        checkOutTime: data.checkOutTime,
        workingHours: data.workingHours
    });
}

function resetAttendanceUI() {
    isCheckedIn = false;

    $('#clockInTime').text('--:--');
    $('#clockOutTime').text('--:--');
    $('#totalHours').text('--:--');

    updateUIForPunchedOut();
    stopTimer();
    clearState();
    stopStatusSync();
}

function handleStatusLoadFailure() {
    const savedState = loadState();

    if (savedState && savedState.isCheckedIn) {
        const elapsedSeconds = Math.floor((Date.now() - savedState.checkInTime) / 1000);

        // Validate saved state (not older than 24 hours)
        if (elapsedSeconds < 24 * 60 * 60) {
            isCheckedIn = true;
            attendanceSeconds = elapsedSeconds;
            updateUIForPunchedIn();
            startTimer(elapsedSeconds);
            startStatusSync();

            console.log('⚠ Restored from localStorage (API failed):', {
                elapsedSeconds: elapsedSeconds
            });
        } else {
            resetAttendanceUI();
        }
    } else {
        resetAttendanceUI();
    }
}

function startStatusSync() {
    if (statusSyncInterval) {
        clearInterval(statusSyncInterval);
    }

    statusSyncInterval = setInterval(function () {
        const user = window.globalUserData || {};
        if (user.empid && isCheckedIn) {
            console.log('🔄 Running periodic status sync...');
            loadAttendanceStatus();
        }
    }, STATUS_SYNC_INTERVAL);

    console.log('✓ Status sync started (5 min interval)');
}

function stopStatusSync() {
    if (statusSyncInterval) {
        clearInterval(statusSyncInterval);
        statusSyncInterval = null;
        console.log('✓ Status sync stopped');
    }
}

// ========================================
// INITIALIZATION
// ========================================

function initializeState(user) {
    user = user || window.globalUserData || {};
    const role = (user.role || "").toString().toLowerCase();

    if (role === 'employee' && user.empid) {
        console.log('🚀 Initializing attendance for employee:', user.empid);
        loadAttendanceStatus();
        //showDropdown();
    } else {
        hideDropdown();
    }
}

// Primary: wait for cookie data loaded event
$(document).on("cookieDataLoaded", function () {
    const user = window.globalUserData || {};
    initDropdown();
    initializeState(user);
});

// Fallback: if cookie already loaded
$(function () {
    setTimeout(function () {
        if (window.globalUserData && Object.keys(window.globalUserData).length > 0) {
            initDropdown();
            initializeState(window.globalUserData);
        }
    }, 50);
});

// ========================================
// ATTENDANCE PUNCH IN/OUT
// ========================================

$("#tapBtn").on("click", async function () {
    const user = window.globalUserData || {};

    if (!user.userid) {
        showToast("Please log in to mark attendance.", "error");
        setTimeout(() => window.location.href = "/Home/Logout", 1000);
        return;
    }

    try {
        if (!isCheckedIn) {
            await punchIn();
        } else {
            await punchOut();
        }
    } catch (error) {
        showToast(error.message || "An error occurred", "error");
    }
});

async function punchIn() {
    const user = window.globalUserData || {};

    try {    
        $('.loader').removeClass('hide');
        // Get all required data in parallel for better performance
        const [locationData, ip] = await Promise.all([
            getCurrentLocation(),
            getIpAddress()
        ]);

        const { latitude, longitude } = locationData;
        const address = await getAddressFromCoords(latitude, longitude);

        const formData = new FormData();
        //formData.append('EmployeeID', user.empid);
        formData.append('Latitude', latitude);
        formData.append('Longitude', longitude);
        formData.append('Location', address);
        formData.append('Address', address);
        formData.append('IPAddress', ip);
        formData.append('DeviceInfo', navigator.userAgent);
        formData.append('Remarks', 'Check-In from web');
        formData.append('ModifiedBy', user.name || 'System');
       
        const response = await $.ajax({
            url: '/MarkAttendance/ProcessCheckin',
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false
        });

        if (response.status) {
            $('.loader').addClass('hide');
            // Update UI with response data
            const checkInData = response.data?.[0]?.checkInDetails;
            if (checkInData?.checkInTime) {
                $('#clockInTime').text(convertTo12HourFormat(checkInData.checkInTime));
            }
            $('#clockOutTime').text('--:--');
            $('#totalHours').text('--:--');

            updateUIForPunchedIn();
            isCheckedIn = true;
            attendanceSeconds = 0;
            startTimer(0);
            saveState();
            startStatusSync();

            showToast(response.message, "success");

            // Refresh status from server after 2 seconds
            setTimeout(() => loadAttendanceStatus(), 2000);
        } else {
            $('.loader').addClass('hide');
            throw new Error(response.message || "Check-in failed");
        }

        setTimeout(() => showDropdown(), 10);
    } catch (error) {
        $('.loader').addClass('hide');
        throw error;
    }
}

async function punchOut() {
    const user = window.globalUserData || {};

    try {
        $('.loader').removeClass('hide');
        // Get all required data in parallel
        const [locationData, ip] = await Promise.all([
            getCurrentLocation(),
            getIpAddress()
        ]);

        const { latitude, longitude } = locationData;
        const address = await getAddressFromCoords(latitude, longitude);

        const formData = new FormData();
        //formData.append('EmployeeID', user.empid);
        formData.append('Latitude', latitude);
        formData.append('Longitude', longitude);
        formData.append('Location', address);
        formData.append('Address', address);
        formData.append('IPAddress', ip);
        formData.append('DeviceInfo', navigator.userAgent);
        formData.append('Remarks', 'Check-Out from web');
        formData.append('ModifiedBy', user.name || 'System');
      
        const response = await $.ajax({
            url: '/MarkAttendance/ProcessCheckout',
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false
        });

        if (response.status) {
            $('.loader').addClass('hide');
            // Update UI with response data
            const checkOutData = response.data?.[0]?.checkOutDetails;
            if (checkOutData?.checkOutTime) {
                $('#clockOutTime').text(convertTo12HourFormat(checkOutData.checkOutTime));
            }

            stopTimer();
            const formattedDuration = formatDuration(attendanceSeconds);
            $('#totalHours').text(response.workingHours || formattedDuration);

            updateUIForPunchedOut();
            isCheckedIn = false;
            clearState();
            stopStatusSync();

            showToast(response.message, "success");

            // Refresh status from server after 2 seconds
            setTimeout(() => loadAttendanceStatus(), 2000);
        } else {
            $('.loader').addClass('hide');
            throw new Error(response.message || "Check-out failed");
        }

        setTimeout(() => showDropdown(), 10);
    } catch (error) {
        $('.loader').addClass('hide');
        throw error;
    }
}

// ========================================
// LOCATION & IP SERVICES
// ========================================

async function getIpAddress() {
    const now = Date.now();

    // Return cached IP if still valid
    if (cachedIP && ipCacheTime && (now - ipCacheTime < CACHE_DURATION)) {
        return cachedIP;
    }

    try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000);

        const response = await fetch("https://api.ipify.org?format=json", {
            signal: controller.signal
        });

        clearTimeout(timeoutId);

        const data = await response.json();
        cachedIP = data.ip;
        ipCacheTime = now;
        return data.ip;
    } catch (error) {
        console.error('IP fetch failed:', error);
        return "Unavailable";
    }
}

function getCurrentLocation() {
    return new Promise((resolve, reject) => {
        if (!navigator.geolocation) {
            reject(new Error("Geolocation not supported by your browser."));
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
                const messages = {
                    [error.PERMISSION_DENIED]: "Location permission denied. Please enable GPS access in your browser.",
                    [error.POSITION_UNAVAILABLE]: "Location unavailable. Please check your device GPS or internet connection.",
                    [error.TIMEOUT]: "Request timed out while fetching location. Please try again."
                };
                reject(new Error(messages[error.code] || "An unknown error occurred while getting location."));
            },
            {
                enableHighAccuracy: true,
                timeout: 10000,
                maximumAge: 0
            }
        );
    });
}

async function getAddressFromCoords(latitude, longitude) {
    try {
        const controller = new AbortController();
        const timeoutId = setTimeout(() => controller.abort(), 5000);

        const response = await fetch(
            `https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json`,
            {
                signal: controller.signal,
                headers: {
                    'User-Agent': 'AttendanceApp/1.0'
                }
            }
        );

        clearTimeout(timeoutId);

        if (!response.ok) throw new Error("Failed to fetch location data.");

        const data = await response.json();
        return data.display_name || "Unknown Area";
    } catch (error) {
        console.error("Error fetching address:", error);
        return "Unknown Area";
    }
}

// ========================================
// UI UPDATES
// ========================================

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

// ========================================
// TIMER MANAGEMENT
// ========================================

function startTimer(startSeconds = 0) {
    attendanceSeconds = startSeconds;

    if (attendanceTimerInterval) {
        clearInterval(attendanceTimerInterval);
    }

    attendanceTimerInterval = setInterval(function () {
        attendanceSeconds++;
        $('#timerBtn').text(formatDuration(attendanceSeconds));

        // Auto-save state every 5 minutes
        if (attendanceSeconds % 300 === 0) {
            saveState();
        }
    }, 1000);
}

function stopTimer() {
    if (attendanceTimerInterval) {
        clearInterval(attendanceTimerInterval);
        attendanceTimerInterval = null;
    }
}

// ========================================
// UTILITY FUNCTIONS
// ========================================

function convertTo12HourFormat(timeStr) {
    if (!timeStr || timeStr === '--:--') return '--:--';

    const parts = timeStr.split(':');
    if (parts.length < 2) return timeStr;

    const [hoursStr, minutes] = parts;
    let hours = parseInt(hoursStr, 10);

    if (isNaN(hours)) return timeStr;

    const ampm = hours >= 12 ? 'PM' : 'AM';
    hours = hours % 12 || 12;
    return `${String(hours).padStart(2, '0')}:${minutes} ${ampm}`;
}

function formatDuration(seconds) {
    const hours = Math.floor(seconds / 3600);
    const minutes = Math.floor((seconds % 3600) / 60);
    const secs = seconds % 60;

    return `${String(hours).padStart(2, '0')}:${String(minutes).padStart(2, '0')}:${String(secs).padStart(2, '0')}`;
}

function formatWorkingHours(hours) {
    if (!hours || hours === 0) return '00:00';

    const h = Math.floor(hours);
    const m = Math.round((hours - h) * 60);

    return `${String(h).padStart(2, '0')}:${String(m).padStart(2, '0')}`;
}

// ========================================
// EVENT HANDLERS FOR STATUS SYNC
// ========================================

// Handle page visibility changes (tab switch)
document.addEventListener('visibilitychange', function () {
    if (!document.hidden) {
        const user = window.globalUserData || {};
        if (user.empid && isCheckedIn) {
            console.log('👁 Page visible - refreshing status');
            loadAttendanceStatus();
        }
    }
});

// Handle page shown from cache (back button navigation)
$(window).on('pageshow', function (event) {
    if (event.originalEvent && event.originalEvent.persisted) {
        const user = window.globalUserData || {};
        if (user.empid) {
            console.log('🔙 Page restored from cache - refreshing status');
            loadAttendanceStatus();
        }
    }
});

// Handle online/offline events
window.addEventListener('online', function () {
    console.log('🌐 Connection restored - syncing status');
    const user = window.globalUserData || {};
    if (user.empid) {
        loadAttendanceStatus();
    }
});

window.addEventListener('offline', function () {
    console.log('📴 Connection lost - using cached state');
});


// ========================================
// CLEANUP ON PAGE UNLOAD
// ========================================

$(window).on('beforeunload', function () {
    if (isCheckedIn) {
        saveState();
    }
});