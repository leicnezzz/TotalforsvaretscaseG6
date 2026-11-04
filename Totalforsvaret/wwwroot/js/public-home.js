(() => {
    "use strict";

    const content = document.querySelector(".public-content");
    const cards = document.querySelector(".public-action-cards");
    if (content && cards) {
        // Halve kortraden overlapper videoen. På mobil overlapper halve første kort.
        const updateOverlap = () => {
            const cardHeight = cards.firstElementChild?.getBoundingClientRect().height;
            if (cardHeight) {
                content.style.setProperty("--cards-overlap", `${cardHeight / 2}px`);
            }
        };
        const observer = new ResizeObserver(updateOverlap);
        observer.observe(cards);
        for (const card of cards.children) observer.observe(card);
        updateOverlap();
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