# Next implementation – Time-window slider for the Race view

## Goal

Replace the per-chart amcharts scrollbar with a single **time-window range slider** at the top of
the Race view. The slider controls the visible time range for every chart panel and the Leaflet map
simultaneously. Chart data is re-sampled dynamically so that the number of rendered data points
stays roughly constant regardless of how wide or narrow the selected window is.

---

## Background – current state

| Component | File | Relevant behaviour |
|---|---|---|
| `RaceViewer` | `Pages/RaceViewer.razor` | Loads all `PositionDto` records, downsamples once to ≤ 500 pts (`MaxChartPoints`), builds `List<ChartPanelConfig>` and passes it to `TelemetryCharts`. |
| `TelemetryCharts` | `Components/Charts/TelemetryCharts.razor` | Wraps amcharts panels; each panel has its own horizontal scrollbar for zooming. |
| `amchartsInterop.js` | `wwwroot/js/amchartsInterop.js` | Creates one amcharts `XYChart` per panel. Each chart gets a `Scrollbar` (`scrollbarX`). Cursors are synced across all charts via a shared `cursormoved` listener. |
| `TimelineSlicer` | `Components/Shared/TimelineSlicer.razor` | Single-handle `<input type="range">` that moves the **cursor** (current-time marker) across all charts and the map. No zooming. |
| `LeafletMap` | `Components/Map/LeafletMap.razor` | Renders the full boat track as one polyline. Cursor position is updated via `leafletInterop.updateCursorPosition`. |

The fixed `MaxChartPoints = 500` step is calculated from the *full race* duration, so narrow time
windows still show coarse data. The amcharts scrollbars exist to compensate, but they act
independently per chart and add visual clutter.

---

## Desired behaviour

1. A **time-window slider** (dual-handle range) sits just above the existing `TimelineSlicer`
   cursor slider on the Race view page.
2. The slider spans the full race from `positions[0].Time` to `positions[^1].Time`.
3. Moving either handle fires an `OnWindowChanged(DateTimeOffset windowStart, DateTimeOffset windowEnd)` event.
4. On each window change:
   - `RaceViewer` re-filters `_positions` to the selected window and re-samples that slice to
     ≤ `MaxChartPoints` points.
   - The new `ChartPanelConfig` data is pushed to `TelemetryCharts` (existing `HasDataChanged`
     detection already handles this).
   - `TelemetryCharts` calls a new JS function `amchartsInterop.setTimeWindow(isoStart, isoEnd)`
     so the x-axis zooms to the window (fast, no re-serialisation of data).
   - `LeafletMap` receives new `WindowStart` / `WindowEnd` parameters; the JS layer highlights
     only the track segment inside the window and auto-fits the map bounds to it.
5. The per-chart amcharts scrollbars are **removed**.
6. The `TimelineSlicer` cursor slider continues to work unchanged inside the current window.

---

## Dynamic sampling

```
windowPointCount  = number of raw positions whose Time is within [windowStart, windowEnd]
step              = max(1, windowPointCount / MaxChartPoints)   // MaxChartPoints = 500
```

This keeps rendered data points near 500 regardless of window width.

### Approximate tiers

| Window width | Typical raw points (10 Hz data) | Step | Rendered points |
|---|---|---|---|
| Full race (≥ 30 min) | ≥ 18 000 | 36 | ~500 |
| Medium (5–30 min) | 3 000–18 000 | 6–36 | ~500 |
| Short (1–5 min) | 600–3 000 | 1–6 | ~500 |
| Very short (< 1 min) | < 600 | 1 | < 500 (all points) |

---

## Implementation tasks

### 1. Create `TimeWindowSlicer` Blazor component

**File:** `Components/Shared/TimeWindowSlicer.razor`

- Two overlapping `<input type="range">` elements sharing the same min/max (position indices into
  `_positions`), styled with CSS so the lower thumb controls the start handle and the upper thumb
  controls the end handle.
- A label row showing `windowStart` time, window duration, and `windowEnd` time.
- Parameters:
  - `List<PositionDto>? Positions` — used to set `min`, `max`, and format labels.
  - `EventCallback<(DateTimeOffset Start, DateTimeOffset End)> OnWindowChanged`
- Debounce the callback ~150 ms (same pattern as `TimelineSlicer`) so dragging does not flood
  `RaceViewer` with re-sample requests.
- Expose a `SetWindowAsync(DateTimeOffset start, DateTimeOffset end)` public method (mirrors
  `TimelineSlicer.SetTimestampAsync`) so other components can programmatically set the window.
- JS interop: extend `timelineInterop.js` with an `initWindow` / `setWindow` section **or** create
  a separate `timeWindowInterop.js` — whichever keeps the file manageable.

### 2. Modify `RaceViewer.razor`

- Add state fields:
  ```csharp
  private DateTimeOffset _windowStart;
  private DateTimeOffset _windowEnd;
  private TimeWindowSlicer? _timeWindowSlicer;
  ```
- Initialise `_windowStart` / `_windowEnd` to `_positions[0].Time` / `_positions[^1].Time` after
  the positions load.
- Replace the current `ComputeDerivedChannels()` signature with
  `ComputeDerivedChannels(DateTimeOffset windowStart, DateTimeOffset windowEnd)` and filter
  `_positions` to the window before sampling:
  ```csharp
  var windowedPositions = _positions
      .Where(p => p.Time >= windowStart && p.Time <= windowEnd)
      .ToList();
  int step = Math.Max(1, windowedPositions.Count / MaxChartPoints);
  ```
- Add `OnWindowChanged((DateTimeOffset start, DateTimeOffset end) window)` handler that updates
  `_windowStart`, `_windowEnd` and calls `ComputeDerivedChannels(...)`.
- Place `<TimeWindowSlicer>` in the template directly above `<TimelineSlicer>`.

### 3. Modify `TelemetryCharts.razor` and `amchartsInterop.js`

**`amchartsInterop.js`**

- Remove the `chart.set("scrollbarX", ...)` line from `createChartPanel`.
- Add a new exported function:
  ```js
  setTimeWindow(isoStart, isoEnd) {
      const start = new Date(isoStart).getTime();
      const end   = new Date(isoEnd).getTime();
      for (const id in chartInstances) {
          const { xAxis } = chartInstances[id];
          xAxis.zoomToDates(new Date(start), new Date(end));
      }
  }
  ```

**`TelemetryCharts.razor`**

- Add a public method:
  ```csharp
  public async Task SetTimeWindowAsync(DateTimeOffset start, DateTimeOffset end)
  {
      if (_chartsInitialized)
          await JS.InvokeVoidAsync("amchartsInterop.setTimeWindow",
              start.ToString("O"), end.ToString("O"));
  }
  ```
- `RaceViewer` calls this after pushing new chart data so the axis zoom is applied immediately.

### 4. Modify `LeafletMap.razor` and `leafletInterop.js`

**`LeafletMap.razor`**

- Add parameters `DateTimeOffset? WindowStart` and `DateTimeOffset? WindowEnd`.
- Include them in `HasParametersChanged()` / `SnapshotParameters()`.
- Pass them to a new JS function `leafletInterop.setTrackWindow(isoStart, isoEnd)`.

**`leafletInterop.js`**

- Keep the full track polyline rendered in a muted colour (e.g. `#adb5bd`, low opacity).
- Add a second "highlight" polyline layer rendered in the current track colour (`#457b9d`) that
  contains only positions within the window.
- `setTrackWindow(isoStart, isoEnd)` rebuilds the highlight polyline and calls
  `map.fitBounds(highlightPolyline.getBounds(), { padding: [20, 20] })`.

### 5. CSS – style the `TimeWindowSlicer`

**File:** `wwwroot/app.css` (or a new `TimeWindowSlicer.razor.css` scoped file)

Dual-handle range sliders require layering two `<input type="range">` elements with `position:
absolute` and transparent backgrounds. The filled track between the two thumbs is drawn with a
CSS `linear-gradient` on the lower input's `::-webkit-slider-runnable-track`. A reference
implementation pattern:

```css
.time-window-slicer { position: relative; height: 2.5rem; }
.time-window-slicer input[type=range] {
    position: absolute; width: 100%; pointer-events: none;
    background: transparent; appearance: none;
}
.time-window-slicer input[type=range]::-webkit-slider-thumb { pointer-events: all; }
```

---

## UI layout (Race view page, top-to-bottom)

```
┌─────────────────────────────────────────────────┐
│  Breadcrumb                                     │
├─────────────────────────────────────────────────┤
│  LeafletMap  (zooms to window when window       │
│              changes; full track in grey,       │
│              window segment highlighted)        │
├─────────────────────────────────────────────────┤
│  TimeWindowSlicer  ◄══════════════════►         │
│  [start time]  [window duration]  [end time]   │
├─────────────────────────────────────────────────┤
│  TimelineSlicer  ──●──────────────────          │
│  [start] [current time] [end]                  │
├─────────────────────────────────────────────────┤
│  Historical / Current toggle                    │
├─────────────────────────────────────────────────┤
│  TelemetryCharts  (no per-chart scrollbars;     │
│  data is pre-filtered to window)                │
│   or                                           │
│  Gauges grid                                    │
└─────────────────────────────────────────────────┘
```

---

## Out of scope for this iteration

- Persisting the selected window across navigation (could be added to `PlaybackState`).
- Animating / "playing" the time window forward automatically.
- Server-side filtering of positions (current API returns all positions for the race; adding a
  `?from=&to=` query parameter to `GET /sessions/{id}/races/{n}/positions` would reduce payload
  but is deferred).

