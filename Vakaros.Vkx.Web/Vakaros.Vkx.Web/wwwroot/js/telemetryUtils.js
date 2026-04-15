/**
 * Shared utility functions used by timelineInterop.js and timeWindowInterop.js.
 * Load this script before the interop modules that depend on it.
 */
window.telemetryUtils = (() => {
    /**
     * Formats an ISO-8601 date string as a local HH:MM:SS label.
     */
    function formatTime(isoString) {
        const d = new Date(isoString);
        const h = String(d.getHours()).padStart(2, '0');
        const m = String(d.getMinutes()).padStart(2, '0');
        const s = String(d.getSeconds()).padStart(2, '0');
        return `${h}:${m}:${s}`;
    }

    /**
     * Quaternion → heel (roll) in degrees.
     * MUST stay in sync with TelemetryMath.ComputeHeel (C#).
     */
    function computeHeel(qw, qx, qy, qz) {
        const sinR = 2.0 * (qw * qx + qy * qz);
        const cosR = 1.0 - 2.0 * (qx * qx + qy * qy);
        return Math.atan2(sinR, cosR) * 180.0 / Math.PI;
    }

    /**
     * Quaternion → trim (pitch) in degrees.
     * MUST stay in sync with TelemetryMath.ComputeTrim (C#).
     */
    function computeTrim(qw, qx, qy, qz) {
        let sinP = 2.0 * (qw * qy - qz * qx);
        sinP = Math.max(-1, Math.min(1, sinP));
        return Math.asin(sinP) * 180.0 / Math.PI;
    }

    /**
     * Formats a duration in milliseconds as a human-readable string.
     */
    function formatDuration(ms) {
        const totalSec = Math.floor(ms / 1000);
        const h = Math.floor(totalSec / 3600);
        const m = Math.floor((totalSec % 3600) / 60);
        const s = totalSec % 60;
        if (h > 0) return `${h}h ${String(m).padStart(2, '0')}m`;
        if (m > 0) return `${m}m ${String(s).padStart(2, '0')}s`;
        return `${s}s`;
    }

    return { formatTime, computeHeel, computeTrim, formatDuration };
})();

/**
 * Shared position data store.
 * Positions are serialized from WASM once and consumed by leafletInterop,
 * timeWindowInterop, and timelineInterop — avoiding 3× serialization of the
 * same large array across the JS interop boundary.
 */
window.positionStore = (() => {
    let positions = null;
    let timesMs = null;

    return {
        /** Called once from .NET after positions are fetched. */
        set(positionData) {
            positions = positionData;
            timesMs = positions ? positions.map(p => new Date(p.time).getTime()) : null;
        },

        /** Returns the raw position array (or null). */
        get() {
            return positions;
        },

        /** Returns the pre-computed epoch-ms array (or null). */
        getTimesMs() {
            return timesMs;
        },

        /** Returns position count, or 0 if not loaded. */
        count() {
            return positions ? positions.length : 0;
        },

        /** Releases the stored data. */
        clear() {
            positions = null;
            timesMs = null;
        }
    };
})();
