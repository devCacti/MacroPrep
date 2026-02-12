window.ConnectionManager = {
    register: function (dotNetReference) {
        // 1. Listen for "I lost connection"
        window.addEventListener('offline', () => {
            dotNetReference.invokeMethodAsync('SetOfflineStatus', true);
        });

        // 2. Listen for "I got connection back"
        window.addEventListener('online', () => {
            dotNetReference.invokeMethodAsync('SetOfflineStatus', false);
        });

        // 3. Return current status immediately
        return navigator.onLine; // true = online, false = offline
    }
};