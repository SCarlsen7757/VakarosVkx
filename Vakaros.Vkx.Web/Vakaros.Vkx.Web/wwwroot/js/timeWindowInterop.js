window.timeWindowInterop = (() => {
    let positions = null;
    let positionTimesMs = null;
    let startSlider = null;
    let endSlider = null;
    let startLabel = null;
    let durationLabel = null;
    let endLabel = null;
    let dotNetRef = null;
    let callbackMethod = null;
    let debounceTimer = null;

    // Chart panel container IDs, set once from .NET during init
    let chartContainerIds = null;

    // Conversion factor from raw SpeedOverGround (m/s) to the user's preferred speed unit.
    // Default is m/s → knots. Updated by .NET via setSogConversion() after preferences load.
    let sogConversionFactor = 1.94384;

    const MAX_CHART_POINTS = 500;

    function formatTime(isoString) {
        const d = new Date(isoString);
        const h = String(d.getHours()).padStart(2, '0');
        const m = String(d.getMinutes()).padStart(2, '0');
        const s = String(d.getSeconds()).padStart(2, '0');
        return `${h}:${m}:${s}`;
    }

    function formatDuration(ms) {
        const totalSec = Math.floor(ms / 1000);
        const h = Math.floor(totalSec / 3600);
        const m = Math.floor((totalSec % 3600) / 60);
        const s = totalSec % 60;
        if (h > 0) return `${h}h ${String(m).padStart(2, '0')}m`;
        if (m > 0) return `${m}m ${String(s).padStart(2, '0')}s`;
        return `${s}s`;
    }

    // Quaternion → heel (roll) in degrees
    function computeHeel(qw, qx, qy, qz) {
        const sinR = 2.0 * (qw * qx + qy * qz);
        const cosR = 1.0 - 2.0 * (qx * qx + qy * qy);
        return Math.atan2(sinR, cosR) * 180.0 / Math.PI;
    }

    // Quaternion → trim (pitch) in degrees
    function computeTrim(qw, qx, qy, qz) {
        let sinP = 2.0 * (qw * qy - qz * qx);
        sinP = Math.max(-1, Math.min(1, sinP));
        return Math.asin(sinP) * 180.0 / Math.PI;
    }

    function resampleAndPush(startIdx, endIdx) {
        if (!positions || !window.echartsInterop || !chartContainerIds) return;

        const windowCount = endIdx - startIdx + 1;
        const step = Math.max(1, Math.floor(windowCount / MAX_CHART_POINTS));

        const speedData = [];
        const heelData = [];
        const trimData = [];
        const headingData = [];

        for (let i = startIdx; i <= endIdx; i += step) {
            const p = positions[i];
            const t = positionTimesMs[i];
            const cogDeg = p.courseOverGround * (180.0 / Math.PI);
            const heel = computeHeel(p.quaternionW, p.quaternionX, p.quaternionY, p.quaternionZ);
            const trim = computeTrim(p.quaternionW, p.quaternionX, p.quaternionY, p.quaternionZ);

            speedData.push({ time: t, sog: p.speedOverGround * sogConversionFactor });
            heelData.push({ time: t, heel: heel });
            trimData.push({ time: t, trim: trim });
            headingData.push({ time: t, cog: cogDeg });
        }

        // Always include the last point
        const lastIdx = endIdx;
        const lastT = positionTimesMs[lastIdx];
        if (speedData.length === 0 || speedData[speedData.length - 1].time !== lastT) {
            const p = positions[lastIdx];
            const cogDeg = p.courseOverGround * (180.0 / Math.PI);
            const heel = computeHeel(p.quaternionW, p.quaternionX, p.quaternionY, p.quaternionZ);
            const trim = computeTrim(p.quaternionW, p.quaternionX, p.quaternionY, p.quaternionZ);

            speedData.push({ time: lastT, sog: p.speedOverGround * sogConversionFactor });
            heelData.push({ time: lastT, heel: heel });
            trimData.push({ time: lastT, trim: trim });
            headingData.push({ time: lastT, cog: cogDeg });
        }

        // Push directly to amcharts — panels are ordered: speed, heel, trim, heading
        const panelData = [speedData, heelData, trimData, headingData];
        for (let i = 0; i < chartContainerIds.length && i < panelData.length; i++) {
            window.echartsInterop.setChartData(chartContainerIds[i], panelData[i]);
        }

        // Zoom x-axis to the window
        const startIso = positions[startIdx].time;
        const endIso = positions[endIdx].time;
        window.echartsInterop.setTimeWindow(startIso, endIso);

        // Update map highlight
        if (window.leafletInterop) {
            window.leafletInterop.setTrackWindow(startIso, endIso);
        }

        // Constrain the cursor (TimelineSlicer) to the selected window
        if (window.timelineInterop) {
            window.timelineInterop.setRange(startIdx, endIdx);
        }
    }

    function updateLabels() {
        if (!positions || !startSlider || !endSlider) return;
        const si = parseInt(startSlider.value, 10);
        const ei = parseInt(endSlider.value, 10);
        const sp = positions[si];
        const ep = positions[ei];
        if (!sp || !ep) return;

        if (startLabel) startLabel.textContent = formatTime(sp.time);
        if (endLabel) endLabel.textContent = formatTime(ep.time);
        if (durationLabel) {
            const ms = positionTimesMs[ei] - positionTimesMs[si];
            durationLabel.textContent = formatDuration(ms);
        }
    }

    function fireCallback() {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(() => {
            if (!positions) return;
            const si = parseInt(startSlider.value, 10);
            const ei = parseInt(endSlider.value, 10);

            // Resample and push chart data directly in JS
            resampleAndPush(si, ei);

            // Notify .NET only for window state (LeafletMap Blazor params, gauge values)
            if (dotNetRef && callbackMethod) {
                dotNetRef.invokeMethodAsync(callbackMethod, si, ei);
            }
        }, 150);
    }

    function onStartInput() {
        if (!startSlider || !endSlider) return;
        const sv = parseInt(startSlider.value, 10);
        const ev = parseInt(endSlider.value, 10);
        if (sv > ev) startSlider.value = ev;
        updateLabels();
        fireCallback();
    }

    function onEndInput() {
        if (!startSlider || !endSlider) return;
        const sv = parseInt(startSlider.value, 10);
        const ev = parseInt(endSlider.value, 10);
        if (ev < sv) endSlider.value = sv;
        updateLabels();
        fireCallback();
    }

    return {
        init(containerId, positionData, dotNetReference, callback) {
            this.dispose();

            positions = positionData;
            positionTimesMs = positions.map(p => new Date(p.time).getTime());
            dotNetRef = dotNetReference;
            callbackMethod = callback;

            const container = document.getElementById(containerId);
            if (!container || !positions || positions.length === 0) return;

            startSlider = container.querySelector('.tw-slider-start');
            endSlider = container.querySelector('.tw-slider-end');
            startLabel = container.querySelector('.tw-label-start');
            durationLabel = container.querySelector('.tw-label-duration');
            endLabel = container.querySelector('.tw-label-end');

            const maxIdx = positions.length - 1;

            if (startSlider) {
                startSlider.min = 0;
                startSlider.max = maxIdx;
                startSlider.value = 0;
                startSlider.addEventListener('input', onStartInput);
            }
            if (endSlider) {
                endSlider.min = 0;
                endSlider.max = maxIdx;
                endSlider.value = maxIdx;
                endSlider.addEventListener('input', onEndInput);
            }

            updateLabels();
        },

        /** Called from .NET after charts are initialised, so JS knows the container IDs */
        setChartIds(ids) {
            chartContainerIds = ids;
        },

        /** Called from .NET after preferences load to set the m/s → preferred-unit factor */
        setSogConversion(factor) {
            sogConversionFactor = factor;
        },

        setWindow(isoStart, isoEnd) {
            if (!positionTimesMs || positionTimesMs.length === 0 || !startSlider || !endSlider) return;
            const startMs = new Date(isoStart).getTime();
            const endMs = new Date(isoEnd).getTime();

            // Binary search for start index
            let lo = 0, hi = positionTimesMs.length - 1;
            while (lo < hi) {
                const mid = (lo + hi) >> 1;
                if (positionTimesMs[mid] < startMs) lo = mid + 1;
                else hi = mid;
            }
            startSlider.value = lo;

            // Binary search for end index
            lo = 0;
            hi = positionTimesMs.length - 1;
            while (lo < hi) {
                const mid = (lo + hi + 1) >> 1;
                if (positionTimesMs[mid] > endMs) hi = mid - 1;
                else lo = mid;
            }
            endSlider.value = lo;

            updateLabels();
        },

        dispose() {
            if (startSlider) startSlider.removeEventListener('input', onStartInput);
            if (endSlider) endSlider.removeEventListener('input', onEndInput);
            clearTimeout(debounceTimer);
            positions = null;
            positionTimesMs = null;
            startSlider = null;
            endSlider = null;
            startLabel = null;
            durationLabel = null;
            endLabel = null;
            dotNetRef = null;
            callbackMethod = null;
            chartContainerIds = null;
        }
    };
})();
