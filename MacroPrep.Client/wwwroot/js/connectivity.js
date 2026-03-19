window.ConnectionManager = {
    register: function (dotNetReference) {
        // Checks if the browser currently has an active internet connection
        window.addEventListener('offline', () => {
            dotNetReference.invokeMethodAsync('SetOfflineStatus', true);
        });

        window.addEventListener('online', () => {
            dotNetReference.invokeMethodAsync('SetOfflineStatus', false);
        });

        // If the browser tab becomes visible again, trigger connectivity checks
        document.addEventListener('visibilitychange', async () => {
            if (document.visibilityState === 'visible') {
                console.log("App returned to foreground, checking connectivity...");

                if (navigator.onLine) {
                    await dotNetReference.invokeMethodAsync('OnAppWakeUp');
                }
            }
        });

        return navigator.onLine; // true = online, false = offline
    }
};