window.leafletInterop = (() => {
    let map = null;
    let trackLayer = null;
    let highlightLayer = null;
    let allPositions = null;
    let marksLayer = null;
    let startLineLayer = null;
    let cursorMarker = null;

    const boatColor = "#005AFF";
    const courseMarksColor = "#FF2600";
    const startMarksColor = "#FF2600";
    const startLineColor = "#D9FF00";
    const routeLineColor = "#FF2600";

    function makeBoatCursorIcon(headingDeg) {
        return L.divIcon({
            className: 'boat-cursor-icon',
            html: `<svg width="16" height="16" viewBox="0 0 16 16" style="transform:rotate(${headingDeg}deg);display:block;"><polygon points="8,1 15,15 8,11 1,15" fill="${boatColor}" stroke="#fff" stroke-width="1.5" stroke-linejoin="round"/></svg>`,
            iconSize: [16, 16],
            iconAnchor: [8, 8]
        });
    }

    const pinIcon = L.divIcon({
        className: 'start-pin-icon',
        html: `<svg width="20" height="20" viewBox="0 0 20 20"><polygon points="7,1 13,13 1,13" fill="${startMarksColor}" stroke-linejoin="round"/></svg>`,
        iconSize: [20, 20],
        iconAnchor: [10, 10]
    });

    const boatEndIcon = L.divIcon({
        className: 'start-boat-icon',
        html: `<div style="width:12px;height:12px;background:${startMarksColor};"></div>`,
        iconSize: [12, 12],
        iconAnchor: [6, 6]
    });

    const markIcon = L.divIcon({
        className: 'course-mark-icon',
        html: `<div style="width:12px;height:12px;background:${courseMarksColor};border:2px solid #fff;border-radius:50%;"></div>`,
        iconSize: [12, 12],
        iconAnchor: [6, 6]
    });

    return {
        initMap(elementId) {
            if (map) {
                map.remove();
                map = null;
            }

            map = L.map(elementId, {
                zoomControl: true,
                attributionControl: true
            }).setView([0, 0], 2);

            // Base layer – CartoDB Light (no labels, no street names, minimal)
            L.tileLayer('https://{s}.basemaps.cartocdn.com/light_nolabels/{z}/{x}/{y}{r}.png', {
                attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors &copy; <a href="https://carto.com/attributions">CARTO</a>',
                subdomains: 'abcd',
                maxZoom: 19
            }).addTo(map);

            // Nautical overlay – OpenSeaMap
            L.tileLayer('https://tiles.openseamap.org/seamark/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openseamap.org">OpenSeaMap</a> contributors',
                maxZoom: 19,
                opacity: 0.8
            }).addTo(map);

            trackLayer = L.layerGroup().addTo(map);
            highlightLayer = L.layerGroup().addTo(map);
            marksLayer = L.layerGroup().addTo(map);
            startLineLayer = L.layerGroup().addTo(map);

            return true;
        },

        addBoatTrack() {
            if (!map || !trackLayer) return;
            trackLayer.clearLayers();
            highlightLayer.clearLayers();

            const positions = window.positionStore.get();
            allPositions = positions;

            if (!positions || positions.length === 0) return;

            const latlngs = positions.map(p => [p.latitude, p.longitude]);

            const polyline = L.polyline(latlngs, {
                color: routeLineColor,
                weight: 3,
                opacity: 0.4
            }).addTo(trackLayer);

            map.fitBounds(polyline.getBounds(), { padding: [30, 30] });
        },

        addCourseMarks(marks) {
            if (!map || !marksLayer) return;
            marksLayer.clearLayers();

            if (!marks || marks.length === 0) return;

            for (const m of marks) {
                L.marker([m.latitude, m.longitude], { icon: markIcon })
                    .bindTooltip(m.markName, {
                        permanent: true,
                        direction: 'top',
                        offset: [0, -10],
                        className: 'mark-tooltip'
                    })
                    .addTo(marksLayer);
            }
        },

        addStartLine(pinEnd, boatEnd) {
            if (!map || !startLineLayer) return;
            startLineLayer.clearLayers();

            if (!pinEnd || !boatEnd) return;

            L.polyline(
                [[pinEnd.latitude, pinEnd.longitude], [boatEnd.latitude, boatEnd.longitude]],
                { color: startLineColor, weight: 3, dashArray: '8,6', opacity: 0.9 }
            ).addTo(startLineLayer);

            // Pin end marker – triangle
            L.marker([pinEnd.latitude, pinEnd.longitude], { icon: pinIcon })
                .bindTooltip('Pin', { direction: 'left' })
                .addTo(startLineLayer);

            // Boat end marker – square
            L.marker([boatEnd.latitude, boatEnd.longitude], { icon: boatEndIcon })
                .bindTooltip('Boat', { direction: 'right' })
                .addTo(startLineLayer);
        },

        updateCursorPosition(lat, lng, headingDeg) {
            if (!map) return;

            const icon = makeBoatCursorIcon(headingDeg ?? 0);
            if (!cursorMarker) {
                cursorMarker = L.marker([lat, lng], { icon, zIndexOffset: 1000 }).addTo(map);
            } else {
                cursorMarker.setLatLng([lat, lng]);
                cursorMarker.setIcon(icon);
            }
        },

        setTrackWindow(isoStart, isoEnd) {
            if (!map || !highlightLayer || !allPositions || allPositions.length === 0) return;
            highlightLayer.clearLayers();

            const startMs = new Date(isoStart).getTime();
            const endMs = new Date(isoEnd).getTime();

            const windowPoints = allPositions.filter(p => {
                const t = new Date(p.time).getTime();
                return t >= startMs && t <= endMs;
            });

            if (windowPoints.length === 0) return;

            const latlngs = windowPoints.map(p => [p.latitude, p.longitude]);
            const highlight = L.polyline(latlngs, {
                color: routeLineColor,
                weight: 3,
                opacity: 1.0
            }).addTo(highlightLayer);

            map.fitBounds(highlight.getBounds(), { padding: [20, 20] });
        },

        clearMap() {
            if (trackLayer) trackLayer.clearLayers();
            if (highlightLayer) highlightLayer.clearLayers();
            if (marksLayer) marksLayer.clearLayers();
            if (startLineLayer) startLineLayer.clearLayers();
            if (cursorMarker) {
                cursorMarker.remove();
                cursorMarker = null;
            }
            allPositions = null;
        },

        fitBounds() {
            if (!map || !trackLayer) return;
            const layers = trackLayer.getLayers();
            if (layers.length > 0) {
                const bounds = L.featureGroup(layers).getBounds();
                map.fitBounds(bounds, { padding: [30, 30] });
            }
        },

        dispose() {
            if (map) {
                map.remove();
                map = null;
            }
            trackLayer = null;
            highlightLayer = null;
            allPositions = null;
            marksLayer = null;
            startLineLayer = null;
            cursorMarker = null;
        }
    };
})();
