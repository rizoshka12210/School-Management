(() => {
    if (!("serviceWorker" in navigator)) {
        return;
    }

    window.addEventListener("load", () => {
        navigator.serviceWorker
            .register("/service-worker.js")
            .catch(() => {
                // Installability just degrades to "regular website" - no
                // need to surface this to the user.
            });
    });
})();
