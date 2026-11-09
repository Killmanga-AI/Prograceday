// Progressive enhancement only: every action still works without JavaScript.
(function () {
    "use strict";

    document.querySelectorAll("form[data-confirm]").forEach(function (form) {
        form.addEventListener("submit", function (event) {
            var message = form.getAttribute("data-confirm");
            if (message && !window.confirm(message)) {
                event.preventDefault();
            }
        });
    });

    document.querySelectorAll("form[data-busy]").forEach(function (form) {
        form.addEventListener("submit", function () {
            var button = form.querySelector('button[type="submit"], button:not([type])');
            if (!button || button.dataset.busy === "true") {
                return;
            }

            button.dataset.busy = "true";
            button.dataset.originalText = button.textContent;

            // Defer disabling so the button's value (if any) is still submitted.
            window.setTimeout(function () {
                button.disabled = true;
                button.textContent = "Working…";
            }, 0);
        });
    });
})();
