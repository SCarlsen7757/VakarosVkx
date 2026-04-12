window.amchartsInterop = (() => {
    let rootInstances = {};
    let chartInstances = {};
    let cursorSyncEnabled = true;
    let dotNetRef = null;
    let callbackMethod = null;

    function createChartPanel(containerId, config) {
        const el = document.getElementById(containerId);
        if (!el) {
            console.error(`[amchartsInterop] Container not found: #${containerId}`);
        } else {
            console.log(`[amchartsInterop] Creating chart in #${containerId} (${el.offsetWidth}x${el.offsetHeight})`);
        }
        const root = am5.Root.new(containerId);
        root.setThemes([am5themes_Animated.new(root)]);

        const chart = root.container.children.push(am5xy.XYChart.new(root, {
            panX: false,
            panY: false,
            wheelX: "none",
            wheelY: "none",
            layout: root.verticalLayout
        }));

        // Shared date axis
        const xAxis = chart.xAxes.push(am5xy.DateAxis.new(root, {
            baseInterval: { timeUnit: "millisecond", count: 100 },
            renderer: am5xy.AxisRendererX.new(root, {}),
            tooltip: am5.Tooltip.new(root, {})
        }));

        const yAxis = chart.yAxes.push(am5xy.ValueAxis.new(root, {
            renderer: am5xy.AxisRendererY.new(root, {})
        }));

        // Add label to y-axis
        if (config.yAxisLabel) {
            yAxis.children.unshift(am5.Label.new(root, {
                text: config.yAxisLabel,
                rotation: -90,
                y: am5.p50,
                centerX: am5.p50,
                fontSize: 12
            }));
        }

        // Create series for each data set in the panel
        const seriesList = [];
        for (const s of config.series) {
            const series = chart.series.push(am5xy.LineSeries.new(root, {
                name: s.name,
                xAxis: xAxis,
                yAxis: yAxis,
                valueYField: s.valueField,
                valueXField: "time",
                stroke: am5.color(s.color),
                tooltip: am5.Tooltip.new(root, {
                    labelText: `${s.name}: {valueY.formatNumber('#.##')}`
                })
            }));

            seriesList.push(series);
        }

        // Cursor with synced behavior — always visible
        const cursor = chart.set("cursor", am5xy.XYCursor.new(root, {
            xAxis: xAxis,
            behavior: "none",
            alwaysShow: true
        }));

        cursor.lineY.set("visible", false);
        cursor.lineX.setAll({
            visible: true,
            stroke: am5.color(0x457b9d),
            strokeWidth: 1,
            strokeDasharray: [4, 4]
        });

        return { root, chart, xAxis, yAxis, cursor, seriesList };
    }

    return {
        initCharts(configs, dotNetReference, cursorCallbackMethod) {
            // Dispose any existing charts
            this.disposeCharts();

            dotNetRef = dotNetReference;
            callbackMethod = cursorCallbackMethod;

            const allCursors = [];

            for (const config of configs) {
                const result = createChartPanel(config.containerId, config);
                rootInstances[config.containerId] = result.root;
                chartInstances[config.containerId] = result;
                allCursors.push({ cursor: result.cursor, xAxis: result.xAxis, chart: result.chart });
            }

            // Sync cursors across all chart panels
            for (let i = 0; i < allCursors.length; i++) {
                const source = allCursors[i];
                source.cursor.events.on("cursormoved", (ev) => {
                    if (!cursorSyncEnabled) return;
                    cursorSyncEnabled = false;

                    const x = source.xAxis.positionToValue(
                        source.xAxis.toAxisPosition(ev.target.getPrivate("positionX"))
                    );

                    if (!Number.isFinite(x)) { cursorSyncEnabled = true; return; }

                    const isoTimestamp = new Date(x).toISOString();

                    // Sync other chart cursors
                    for (let j = 0; j < allCursors.length; j++) {
                        if (i === j) continue;
                        const target = allCursors[j];
                        const position = target.xAxis.valueToPosition(x);
                        target.cursor.set("positionX", target.xAxis.toGlobalPosition(position));
                    }

                    // Sync timeline slider + map cursor directly in JS — no WASM round-trip
                    if (window.timelineInterop) {
                        window.timelineInterop.setTimestamp(isoTimestamp);
                    }

                    // Notify .NET only for gauge value updates
                    if (dotNetRef && callbackMethod) {
                        dotNetRef.invokeMethodAsync(callbackMethod, isoTimestamp);
                    }

                    cursorSyncEnabled = true;
                });
            }
        },

        setChartData(containerId, data) {
            const instance = chartInstances[containerId];
            if (!instance) {
                console.error(`[amchartsInterop] setChartData: no chart instance for #${containerId}`);
                return;
            }
            console.log(`[amchartsInterop] setChartData #${containerId}: ${data.length} points, first=`, data[0]);

            // Convert ISO timestamps to numeric for amCharts date axis
            const processed = data.map(d => {
                const obj = { ...d };
                obj.time = new Date(d.time).getTime();
                return obj;
            });

            for (const series of instance.seriesList) {
                series.data.setAll(processed);
            }
        },

        syncCursor(isoTimestamp) {
            // Suppress .NET callback while we programmatically move all cursors.
            // Without this guard every chart fires its own cursormoved event back
            // to .NET, causing 4x redundant OnChartCursorMoved calls per tick.
            cursorSyncEnabled = false;
            const time = new Date(isoTimestamp).getTime();
            for (const containerId of Object.keys(chartInstances)) {
                const instance = chartInstances[containerId];
                if (!instance) continue;
                const position = instance.xAxis.valueToPosition(time);
                instance.cursor.set("positionX", instance.xAxis.toGlobalPosition(position));
            }
            cursorSyncEnabled = true;
        },

        setTimeWindow(isoStart, isoEnd) {
            const start = new Date(isoStart).getTime();
            const end = new Date(isoEnd).getTime();
            for (const id in chartInstances) {
                const { xAxis } = chartInstances[id];
                xAxis.zoomToDates(new Date(start), new Date(end));
            }
        },

        disposeCharts() {
            for (const id of Object.keys(rootInstances)) {
                rootInstances[id].dispose();
            }
            rootInstances = {};
            chartInstances = {};
            dotNetRef = null;
            callbackMethod = null;
        }
    };
})();
