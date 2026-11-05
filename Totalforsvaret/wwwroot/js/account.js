(() => {
    "use strict";

    document.querySelectorAll("[data-password-toggle]")
        .forEach(button => {
            const inputId = button.getAttribute("aria-controls");
            const input = document.getElementById(inputId);

            if (!(input instanceof HTMLInputElement)) {
                return;
            }

            button.addEventListener("click", () => {
                const showPassword = input.type === "password";

                input.type = showPassword ? "text" : "password";

                button.setAttribute(
                    "aria-pressed",
                    String(showPassword)
                );

                button.setAttribute(
                    "aria-label",
                    showPassword ? "Skjul passord" : "Vis passord"
                );
            });
        });
})();
// Veksler mellom å vise og skjule passordfeltet når brukere klikker på knappen. 