window.timelineInterop = (() => {
    let positions = null;
    let sliderEl = null;
    let currentLabel = null;
    let dotNetRef = null;
    let callbackMethod = null;
    let debounceTimer = null;

    function formatTime(isoString) {
        const d = new Date(isoString);
        const h = String(d.getHours()).padStart(2, '0');
        const m = String(d.getMinutes()).padStart(2, '0');
        const s = String(d.getSeconds()).padStart(2, '0');
        return `${h}:${m}:${s}`;
    }

    function onSliderInput() {
        if (!positions || !sliderEl) return;
        const idx = parseInt(sliderEl.value, 10);
        const pos = positions[idx];
        if (!pos) return;

        // Update time label instantly — no server round-trip
        if (currentLabel) {
            currentLabel.textContent = formatTime(pos.time);
        }

        // Move map boat cursor instantly — no server round-trip
        if (window.leafletInterop) {
            window.leafletInterop.updateCursorPosition(pos.latitude, pos.longitude);
        }

        // Debounce the .NET callback so charts/gauges sync after the user
        // settles, rather than on every pixel of slider movement.
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            if (dotNetRef && callbackMethod) {
                dotNetRef.invokeMethodAsync(callbackMethod, pos.time);
            }
        }, 80);
    }

    return {
        init(containerId, positionData, dotNetReference, callback) {
            // Clean up previous instance if any
            this.dispose();

            positions = positionData;
            dotNetRef = dotNetReference;
            callbackMethod = callback;

            const container = document.getElementById(containerId);
            if (!container || !positions || positions.length === 0) return;

            sliderEl = container.querySelector('.timeline-slider');
            currentLabel = container.querySelector('.timeline-current');

            if (sliderEl) {
                sliderEl.max = positions.length - 1;
                sliderEl.value = 0;
                sliderEl.addEventListener('input', onSliderInput);
            }

            // Set time boundary labels
            const timeLabels = container.querySelectorAll('.timeline-time');
            if (timeLabels.length >= 2) {
                timeLabels[0].textContent = formatTime(positions[0].time);
                timeLabels[timeLabels.length - 1].textContent =
                    formatTime(positions[positions.length - 1].time);
            }
            if (currentLabel) {
                currentLabel.textContent = formatTime(positions[0].time);
            }
        },

        setTimestamp(isoTimestamp) {
            if (!positions || positions.length === 0 || !sliderEl) return;
            const target = new Date(isoTimestamp).getTime();
            let bestIdx = 0;
            let bestDiff = Infinity;
            for (let i = 0; i < positions.length; i++) {
                const diff = Math.abs(new Date(positions[i].time).getTime() - target);
                if (diff < bestDiff) {
                    bestDiff = diff;
                    bestIdx = i;
                }
            }
            sliderEl.value = bestIdx;
            if (currentLabel) {
                currentLabel.textContent = formatTime(positions[bestIdx].time);
            }
        },

        dispose() {
            if (sliderEl) {
                sliderEl.removeEventListener('input', onSliderInput);
            }
            clearTimeout(debounceTimer);
            positions = null;
            sliderEl = null;
            currentLabel = null;
            dotNetRef = null;
            callbackMethod = null;
        }
    };
})();
