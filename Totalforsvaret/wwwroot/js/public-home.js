(() => {
    "use strict";

    const firstScreen = document.querySelector(".public-first-screen");
    const purpose = document.querySelector(".public-purpose");

    if (firstScreen && purpose) {
        const updateLayout = () => {
            firstScreen.style.setProperty("--public-purpose-overlap", `${purpose.getBoundingClientRect().height / 2}px`);
        };
        const observer = new ResizeObserver(updateLayout);
        observer.observe(purpose);
        updateLayout();
    }

    const video = document.getElementById("public-background-video");
    if (!video) return;

    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)");

    function updatePlayback() {
        if (reducedMotion.matches) {
            video.removeAttribute("src");
            video.hidden = true;
        } else {
            video.src = video.dataset.videoSrc;
            video.hidden = false;
        }
    }

    reducedMotion.addEventListener("change", updatePlayback);
    updatePlayback();
})();
// Viser video på forsiden.