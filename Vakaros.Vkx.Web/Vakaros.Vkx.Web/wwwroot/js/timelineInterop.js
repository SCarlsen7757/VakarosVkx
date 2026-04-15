window.timelineInterop = (() => {
    let positions = null;
    let positionTimesMs = null; // pre-built numeric array for binary search
    let container = null;
    let sliderEl = null;
    let currentLabel = null;
    let dotNetRef = null;
    let callbackMethod = null;
    let debounceTimer = null;

    const { formatTime } = window.telemetryUtils;

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
            // Sync chart cursors directly in JS — avoids .NET → JS round-trip
            if (window.echartsInterop) {
                window.echartsInterop.syncCursor(pos.time);
            }

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
            positionTimesMs = positions.map(p => new Date(p.time).getTime());
            dotNetRef = dotNetReference;
            callbackMethod = callback;

            container = document.getElementById(containerId);
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
            if (!positionTimesMs || positionTimesMs.length === 0 || !sliderEl) return;
            const target = new Date(isoTimestamp).getTime();

            // Binary search — positionTimesMs is sorted ascending
            let lo = 0, hi = positionTimesMs.length - 1;
            while (lo < hi) {
                const mid = (lo + hi) >> 1;
                if (positionTimesMs[mid] < target) lo = mid + 1;
                else hi = mid;
            }
            if (lo > 0 && Math.abs(positionTimesMs[lo - 1] - target) <= Math.abs(positionTimesMs[lo] - target)) {
                lo--;
            }

            // Clamp to current window range so slider stays within bounds
            lo = Math.min(Math.max(lo, parseInt(sliderEl.min, 10)), parseInt(sliderEl.max, 10));

            sliderEl.value = lo;
            if (currentLabel) {
                currentLabel.textContent = formatTime(positions[lo].time);
            }

            // Keep map cursor in sync without a .NET round-trip
            if (window.leafletInterop) {
                const pos = positions[lo];
                window.leafletInterop.updateCursorPosition(pos.latitude, pos.longitude);
            }
        },

        /**
         * Constrains the cursor slider to [startIdx, endIdx].
         * Called by timeWindowInterop whenever the window changes.
         */
        setRange(startIdx, endIdx) {
            if (!sliderEl || !positions) return;

            sliderEl.min = startIdx;
            sliderEl.max = endIdx;

            // Clamp current cursor position inside the new range
            const current = parseInt(sliderEl.value, 10);
            const clamped = Math.min(Math.max(current, startIdx), endIdx);
            if (clamped !== current) {
                sliderEl.value = clamped;
                if (currentLabel) {
                    currentLabel.textContent = formatTime(positions[clamped].time);
                }
            }

            // Update boundary labels to show window extents
            const timeLabels = container?.querySelectorAll('.timeline-time');
            if (timeLabels && timeLabels.length >= 2) {
                timeLabels[0].textContent = formatTime(positions[startIdx].time);
                timeLabels[timeLabels.length - 1].textContent = formatTime(positions[endIdx].time);
            }
        },

        dispose() {
            if (sliderEl) {
                sliderEl.removeEventListener('input', onSliderInput);
            }
            clearTimeout(debounceTimer);
            positions = null;
            positionTimesMs = null;
            container = null;
            sliderEl = null;
            currentLabel = null;
            dotNetRef = null;
            callbackMethod = null;
        }
    };
})();
