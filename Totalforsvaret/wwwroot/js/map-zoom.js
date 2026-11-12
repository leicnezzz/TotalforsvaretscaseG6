if (window.L) {
    L.Map.addInitHook(function () {
        const map = this;
        const container = map.getContainer();
        const listeners = new AbortController();
        const options = { passive: false, signal: listeners.signal };
        let touching = false;
        let gesture = null;

        function updateTouchState(event) {
            touching = event.targetTouches.length > 0;
        }

        for (const eventName of ['touchstart', 'touchend', 'touchcancel']) {
            container.addEventListener(eventName, updateTouchState, { signal: listeners.signal });
        }

        // Safari reports trackpad pinches as gesture events instead of wheel events.
        container.addEventListener('gesturestart', (event) => {
            if (touching) return;

            event.preventDefault();
            gesture = {
                zoom: map.getZoom(),
                point: map.mouseEventToContainerPoint(event)
            };
        }, options);

        container.addEventListener('gesturechange', (event) => {
            if (!gesture) return;

            event.preventDefault();
            const targetZoom = Math.round(gesture.zoom + Math.log2(event.scale));
            const zoom = Math.max(map.getMinZoom(), Math.min(map.getMaxZoom(), targetZoom));
            if (zoom !== map.getZoom()) {
                map.setZoomAround(gesture.point, zoom, { animate: false });
            }
        }, options);

        container.addEventListener('gestureend', (event) => {
            if (!gesture) return;

            event.preventDefault();
            gesture = null;
        }, options);

        map.once('unload', () => listeners.abort());
    });
}
