(() => {
    "use strict";
    
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