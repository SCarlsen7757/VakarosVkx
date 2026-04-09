window.leafletInterop = (() => {
    let map = null;
    let trackLayer = null;
    let marksLayer = null;
    let startLineLayer = null;
    let cursorMarker = null;

    const boatIcon = L.divIcon({
        className: 'boat-cursor-icon',
        html: '<div style="width:14px;height:14px;background:#e63946;border:2px solid #fff;border-radius:50%;box-shadow:0 0 6px rgba(0,0,0,0.4);"></div>',
        iconSize: [14, 14],
        iconAnchor: [7, 7]
    });

    const markIcon = L.divIcon({
        className: 'course-mark-icon',
        html: '<div style="width:12px;height:12px;background:#ff9f1c;border:2px solid #fff;border-radius:50%;box-shadow:0 0 4px rgba(0,0,0,0.3);"></div>',
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

            // Base layer – standard OSM
            L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
                attribution: '&copy; OpenStreetMap contributors',
                maxZoom: 19
            }).addTo(map);

            // Nautical overlay – OpenSeaMap
            L.tileLayer('https://tiles.openseamap.org/seamark/{z}/{x}/{y}.png', {
                attribution: '&copy; <a href="https://www.openseamap.org">OpenSeaMap</a> contributors',
                maxZoom: 19,
                opacity: 0.8
            }).addTo(map);

            trackLayer = L.layerGroup().addTo(map);
            marksLayer = L.layerGroup().addTo(map);
            startLineLayer = L.layerGroup().addTo(map);

            return true;
        },

        addBoatTrack(positions, dotnetRef, callbackMethod) {
            if (!map || !trackLayer) return;
            trackLayer.clearLayers();

            if (!positions || positions.length === 0) return;

            const latlngs = positions.map(p => [p.latitude, p.longitude]);

            const polyline = L.polyline(latlngs, {
                color: '#457b9d',
                weight: 3,
                opacity: 0.8
            }).addTo(trackLayer);

            if (dotnetRef && callbackMethod) {
                polyline.on('click', (e) => {
                    // Find nearest position by distance
                    let minDist = Infinity;
                    let nearestIdx = 0;
                    for (let i = 0; i < positions.length; i++) {
                        const dx = positions[i].latitude - e.latlng.lat;
                        const dy = positions[i].longitude - e.latlng.lng;
                        const dist = dx * dx + dy * dy;
                        if (dist < minDist) {
                            minDist = dist;
                            nearestIdx = i;
                        }
                    }
                    dotnetRef.invokeMethodAsync(callbackMethod, positions[nearestIdx].time);
                });
            }

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
                { color: '#e63946', weight: 3, dashArray: '8,6', opacity: 0.9 }
            ).addTo(startLineLayer);

            // Pin end marker
            L.circleMarker([pinEnd.latitude, pinEnd.longitude], {
                radius: 5, color: '#e63946', fillColor: '#e63946', fillOpacity: 1
            }).bindTooltip('Pin', { direction: 'left' }).addTo(startLineLayer);

            // Boat end marker
            L.circleMarker([boatEnd.latitude, boatEnd.longitude], {
                radius: 5, color: '#e63946', fillColor: '#e63946', fillOpacity: 1
            }).bindTooltip('Boat', { direction: 'right' }).addTo(startLineLayer);
        },

        updateCursorPosition(lat, lng) {
            if (!map) return;

            if (!cursorMarker) {
                cursorMarker = L.marker([lat, lng], { icon: boatIcon, zIndexOffset: 1000 }).addTo(map);
            } else {
                cursorMarker.setLatLng([lat, lng]);
            }
        },

        clearMap() {
            if (trackLayer) trackLayer.clearLayers();
            if (marksLayer) marksLayer.clearLayers();
            if (startLineLayer) startLineLayer.clearLayers();
            if (cursorMarker) {
                cursorMarker.remove();
                cursorMarker = null;
            }
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
            marksLayer = null;
            startLineLayer = null;
            cursorMarker = null;
        }
    };
})();
