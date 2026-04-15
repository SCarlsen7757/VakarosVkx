window.echartsInterop = (() => {
    let chartInstances = {}; // containerId -> { chart, config }
    let allCharts = [];
    let resizeObserver = null;
    let cursorSyncEnabled = true;
    let dotNetRef = null;
    let callbackMethod = null;

    const GRID = { left: 60, right: 15, top: 10, bottom: 30 };

    function createChartPanel(containerId, config) {
        const el = document.getElementById(containerId);
        if (!el) {
            console.error(`[echartsInterop] Container not found: #${containerId}`);
            return null;
        }

        const chart = echarts.init(el);

        chart.setOption({
            animation: false,
            tooltip: {
                trigger: 'axis',
                confine: true,
                axisPointer: {
                    type: 'line',
                    lineStyle: {
                        color: '#457b9d',
                        type: 'dashed',
                        width: 1
                    }
                },
                formatter: function (params) {
                    if (!params || params.length === 0) return '';
                    let html = '';
                    for (const p of params) {
                        const val = Array.isArray(p.value) ? p.value[1] : p.value;
                        html += `${p.marker}${p.seriesName}: <b>${val != null ? val.toFixed(2) : '\u2014'}</b><br/>`;
                    }
                    return html;
                }
            },
            grid: GRID,
            xAxis: {
                type: 'time',
                boundaryGap: false,
                axisLabel: {
                    formatter: function (value) {
                        const d = new Date(value);
                        return `${String(d.getHours()).padStart(2, '0')}:${String(d.getMinutes()).padStart(2, '0')}:${String(d.getSeconds()).padStart(2, '0')}`;
                    }
                }
            },
            yAxis: {
                type: 'value',
                name: config.yAxisLabel,
                nameLocation: 'middle',
                nameGap: 45,
                nameRotate: 90,
                nameTextStyle: { fontSize: 11 }
            },
            series: config.series.map(s => ({
                name: s.name,
                type: 'line',
                symbol: 'none',
                data: []
            }))
        });

        return chart;
    }

    return {
        initCharts(configs, dotNetReference, cursorCallbackMethod) {
            this.disposeCharts();

            dotNetRef = dotNetReference;
            callbackMethod = cursorCallbackMethod;

            for (const config of configs) {
                const chart = createChartPanel(config.containerId, config);
                if (chart) {
                    chartInstances[config.containerId] = { chart, config };
                    allCharts.push(chart);
                }
            }

            // Link all charts so the tooltip and axis pointer sync across all panels
            // when the user hovers any one of them.
            if (allCharts.length > 1) {
                echarts.connect(allCharts);
            }

            // ZRender mousemove: extract the hovered time, update the timeline slider
            // and notify .NET for gauge / map cursor updates — no WASM round-trip for
            // the timeline/map because those are driven directly in JS.
            for (const chart of allCharts) {
                chart.getZr().on('mousemove', (e) => {
                    if (!cursorSyncEnabled) return;

                    const timeMs = chart.convertFromPixel({ xAxisIndex: 0 }, e.offsetX);
                    if (timeMs == null || !isFinite(timeMs) || timeMs <= 0) return;

                    cursorSyncEnabled = false;
                    const isoTs = new Date(timeMs).toISOString();

                    if (window.timelineInterop) {
                        window.timelineInterop.setTimestamp(isoTs);
                    }

                    if (dotNetRef && callbackMethod) {
                        dotNetRef.invokeMethodAsync(callbackMethod, isoTs);
                    }

                    cursorSyncEnabled = true;
                });
            }

            // Resize observer keeps charts filling their containers after layout changes.
            resizeObserver = new ResizeObserver(() => {
                for (const chart of allCharts) {
                    chart.resize();
                }
            });
            for (const id of Object.keys(chartInstances)) {
                const el = document.getElementById(id);
                if (el) resizeObserver.observe(el);
            }
        },

        setChartData(containerId, data) {
            const instance = chartInstances[containerId];
            if (!instance) {
                console.error(`[echartsInterop] setChartData: no chart for #${containerId}`);
                return;
            }

            const { chart, config } = instance;

            // data entries have { time: <ISOstring|ms>, [valueField]: <number> }
            // Convert to ECharts [[timeMs, value], ...] per series.
            chart.setOption({
                series: config.series.map(s => ({
                    data: data.map(d => [
                        new Date(d.time).getTime(),
                        d[s.valueField] != null ? d[s.valueField] : null
                    ])
                }))
            });
        },

        syncCursor(isoTimestamp) {
            cursorSyncEnabled = false;
            const timeMs = new Date(isoTimestamp).getTime();

            // Dispatch showTip on every chart at the pixel position that corresponds
            // to the requested timestamp.  Each chart gets its own pixel conversion
            // so the position is time-accurate regardless of individual chart width.
            for (const id of Object.keys(chartInstances)) {
                const { chart } = chartInstances[id];
                const xPixel = chart.convertToPixel({ xAxisIndex: 0 }, timeMs);
                if (xPixel != null && isFinite(xPixel)) {
                    chart.dispatchAction({
                        type: 'showTip',
                        x: Math.max(0, xPixel),
                        y: Math.floor(chart.getHeight() / 2)
                    });
                }
            }

            cursorSyncEnabled = true;
        },

        setTimeWindow(isoStart, isoEnd) {
            const startMs = new Date(isoStart).getTime();
            const endMs = new Date(isoEnd).getTime();
            for (const id of Object.keys(chartInstances)) {
                chartInstances[id].chart.setOption({
                    xAxis: { min: startMs, max: endMs }
                });
            }
        },

        disposeCharts() {
            if (resizeObserver) {
                resizeObserver.disconnect();
                resizeObserver = null;
            }
            for (const id of Object.keys(chartInstances)) {
                chartInstances[id].chart.dispose();
            }
            chartInstances = {};
            allCharts = [];
            dotNetRef = null;
            callbackMethod = null;
            cursorSyncEnabled = true;
        }
    };
})();
