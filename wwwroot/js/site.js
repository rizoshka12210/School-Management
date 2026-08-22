(() => {
    const body = document.body;
    const sidebarToggle = document.getElementById("sidebarToggle");
    const sidebarBackdrop = document.getElementById("sidebarBackdrop");
    const sidebar = document.querySelector(".sidebar");

    const setSidebarOpen = (isOpen) => {
        if (!body) return;

        body.classList.toggle("sidebar-open", isOpen);
        sidebarToggle?.setAttribute("aria-expanded", isOpen ? "true" : "false");
        sidebarBackdrop?.setAttribute("aria-hidden", isOpen ? "false" : "true");
    };

    sidebarToggle?.addEventListener("click", () => {
        setSidebarOpen(!body.classList.contains("sidebar-open"));
    });

    sidebarBackdrop?.addEventListener("click", () => setSidebarOpen(false));

    sidebar?.querySelectorAll("a.sidebar-link").forEach(link => {
        link.addEventListener("click", () => {
            if (window.matchMedia("(max-width: 768px)").matches) {
                setSidebarOpen(false);
            }
        });
    });

    document.addEventListener("keydown", event => {
        if (event.key === "Escape" && body.classList.contains("sidebar-open")) {
            setSidebarOpen(false);
            sidebarToggle?.focus();
        }
    });

    window.addEventListener("resize", () => {
        if (window.innerWidth > 768 && body.classList.contains("sidebar-open")) {
            setSidebarOpen(false);
        }
    });

    document.querySelectorAll(".alert.alert-dismissible").forEach(alertElement => {
        alertElement.setAttribute("role", "status");
    });

    document.querySelectorAll(".table-responsive").forEach(wrapper => {
        if (!wrapper.hasAttribute("tabindex")) {
            wrapper.setAttribute("tabindex", "0");
        }
    });
})();
