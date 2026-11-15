(() => {
    "use strict";

    document.querySelectorAll(".site-header .nav-toggle").forEach(button => {
        const nav = button.closest(".primary-nav");
        const menuId = button.getAttribute("aria-controls");
        const menu = menuId ? document.getElementById(menuId) : null;

        if (!nav || !menu) return;

        const setOpen = open => {
            nav.classList.toggle("nav-open", open);
            button.setAttribute("aria-expanded", String(open));
            button.setAttribute("aria-label", open ? "Lukk meny" : "Åpne meny");
            button.querySelector(".nav-toggle-icon").textContent = open ? "×" : "☰";
        };

        button.addEventListener("click", () => {
            setOpen(button.getAttribute("aria-expanded") !== "true");
        });

        menu.querySelectorAll("a").forEach(link => {
            link.addEventListener("click", () => setOpen(false));
        });

        nav.addEventListener("keydown", event => {
            if (event.key === "Escape") {
                setOpen(false);
                button.focus();
            }
        });

        document.addEventListener("click", event => {
            if (!nav.contains(event.target)) {
                setOpen(false);
            }
        });

        window.matchMedia("(min-width: 761px)").addEventListener("change", event => {
            if (event.matches) setOpen(false);
        });
    });
})();