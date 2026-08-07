// Small wrapper around navigator.geolocation for the ESS GPS check-in page
// (Components/Pages/Ess/EssAttendanceCheckin.razor). Blazor Server has no
// built-in way to read browser APIs, so this is invoked via IJSRuntime.
window.humanokAtt = {
    getCurrentPosition: function () {
        return new Promise((resolve) => {
            if (!navigator.geolocation) {
                resolve({ success: false, error: "เบราว์เซอร์นี้ไม่รองรับการระบุตำแหน่ง (Geolocation)" });
                return;
            }
            navigator.geolocation.getCurrentPosition(
                function (position) {
                    resolve({
                        success: true,
                        latitude: position.coords.latitude,
                        longitude: position.coords.longitude,
                        accuracy: position.coords.accuracy
                    });
                },
                function (err) {
                    let message = "ไม่สามารถระบุตำแหน่งได้";
                    if (err.code === err.PERMISSION_DENIED) message = "กรุณาอนุญาตให้เข้าถึงตำแหน่งของคุณเพื่อเช็คอิน";
                    else if (err.code === err.POSITION_UNAVAILABLE) message = "ไม่พบสัญญาณตำแหน่ง กรุณาลองใหม่ในที่โล่ง";
                    else if (err.code === err.TIMEOUT) message = "หมดเวลารอสัญญาณตำแหน่ง กรุณาลองใหม่";
                    resolve({ success: false, error: message });
                },
                { enableHighAccuracy: true, timeout: 15000, maximumAge: 0 }
            );
        });
    }
};
