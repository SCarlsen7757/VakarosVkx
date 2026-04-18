# Design Specification – Vakaros VKX Frontend

This document describes the features, screens, and behaviour of the Vakaros VKX web application.

---

## Application Overview

The application lets users upload Vakaros `.vkx` sailing data files, browse the recorded sessions
and races, and analyse telemetry through an interactive map and charts. It also provides
administration pages for the reference data that annotates sessions (boats, boat classes, course
marks, and courses).

---

## Layout & Navigation

The application uses a **responsive navigation** design that adapts to the available screen size:

- **Mobile & portrait tablet** – a fixed **bottom tab bar** with icons and short labels. This keeps navigation thumb-reachable on touch devices.
- **Landscape tablet** – a **left icon rail** with icons and labels, always visible.
- **Desktop** – a **left sidebar** that can be collapsed to an icon-only rail to reclaim space.

The active tab / sidebar item is visually highlighted.

### Navigation tabs / sidebar items

| Icon | Label | Route | Notes |
| --- | --- | --- | --- |
| 🏠 | Home | `/` | |
| ⬆️ | Upload | `/upload` | |
| 📋 | Sessions | `/sessions` | |
| ⚓ | Fleet | `/boats` | Secondary nav to `/boat-classes` |
| 🗺️ | Courses | `/courses` | Secondary nav to `/marks` |
| ⚙️ | Settings | `/settings` | |

**Fleet** groups Boats and Boat Classes. Tapping/clicking the Fleet tab lands on the Boats list; a secondary link or tab within the Fleet section navigates to Boat Classes.

**Courses** groups Courses and Marks. Tapping/clicking the Courses tab lands on the Courses list; a secondary link within the section navigates to Marks.

### Deep-page navigation

Session Detail and Race Viewer are reached by navigating into sessions and races, not directly from the tab bar.

- **Session Detail** shows a breadcrumb: *Sessions → (session file name)*
- **Race Viewer** shows a breadcrumb: *Sessions → (session file name) → Race n*

The Race Viewer hides the bottom tab bar and left sidebar entirely to maximise screen space. A **← Back** button in the page header returns the user to Session Detail.

---

## General UI Conventions

- **Loading state** – every data-dependent view shows a skeleton placeholder while the API call is in flight.
- **Error state** – if an API call fails, an inline error banner (warning icon + message) replaces the content area. The rest of the page remains usable.
- **Empty state** – if a list or page has no data, a friendly message is shown, often with a call-to-action link (e.g. "Upload a VKX file to get started").
- **Inline editing** – tables that support editing switch the affected row into an editable state in-place; other rows remain read-only and unaffected.
- **Confirmation on save** – a brief success indicator (e.g. "✓ Saved") appears after a successful write operation.

---

## Pages

---

### 1. Dashboard — `/`

The landing page. Displays a card grid of six global statistics:

| Card | Value |
| --- | --- |
| Boats | Total number of registered boats |
| Sessions | Total number of uploaded sessions |
| Races | Total number of detected races |
| Total Distance | Total distance sailed (User preference (kn / km)) |
| Top Speed | All-time top speed (User preference (kn / km/h)) |
| Race Time | Total accumulated race duration in hours and minutes |

When no data exists at all, the cards are replaced by a prompt that links to the Upload page.

**API used:** `GET /api/stats/summary`

---

### 2. Upload — `/upload`

A single file-picker input, filtered to `.vkx` files.

**Behaviour:**

1. The user selects a `.vkx` file (maximum 100 MB).
2. The file is uploaded to the API as multipart form data.
3. While uploading an "Uploading…" indicator is displayed and the input is disabled.
4. On success the user is automatically navigated to the new session's detail page.
5. On failure an inline error banner explains the problem.

**API used:** `POST /api/sessions/upload`

---

### 3. Sessions List — `/sessions`

A table of all recorded sessions.

**Columns:**

| Column | Notes |
| --- | --- |
| File | File name; clicking navigates to Session Detail |
| Boat | Assigned boat name |
| Course | Assigned course name |
| Started | Local date and time |
| Duration | Human-readable duration (e.g. 2 h 15 min) |
| Races | Number of detected races in the session |

**API used:**

- `GET /api/sessions` – session list
- `GET /api/boats` – boat list for the dropdown

---

### 4. Session Detail — `/sessions/{id}`

Detail view for a single session.

**Breadcrumb:** Sessions → *(current session file name)*

**Metadata panel:**

| Field | Notes |
| --- | --- |
| File name | |
| Boat | |
| Course | |
| Started | Local date-time |
| Ended | Local date-time |
| Telemetry Rate | Hz |
| Format Version | |
| Notes | Only shown when present |

**Race table:**

| Column | Notes |
| --- | --- |
| Race # | Sequential number |
| Started | Local time |
| Ended | Local time |
| Duration | Formatted |
| Course | Assigned course |

**Boat assignment:** A dropdown of known boats is shown in the metadata panel. Changing the selection does **not** save automatically; the user must press an explicit **Save** button. A brief "✓ Saved" indicator confirms success.

**Course assignment:** Because a session may contain multiple races that could be assigned to different courses, the Course column in the race table is the most intuitive place to manage course assignments. Each cell in that column shows a dropdown of known courses. Changing the selection does **not** save automatically; an explicit **Save** button per row (or a single **Save** button in the row's edit state) confirms the change. A brief "✓ Saved" indicator confirms success.

**View Race →** navigates to the Race Viewer for that race.

**API used:**

- `GET /api/sessions/{id}`
- `GET /api/courses` – course list for the dropdown
- `PATCH /api/sessions/{id}` – save boat assignment
- `PATCH /api/sessions/{id}/races/{raceNumber}` – save course assignment

---

### 5. Race Viewer — `/sessions/{id}/races/{n}`

The most interactive page. Two-column layout: map on the left, controls and data on the right.

#### 5.1 Map

An interactive map (e.g. Leaflet.js) that fills the left column.

**What the map shows:**

- The full GPS track for the race as a coloured polyline.
- Pre-race countdown track (if a countdown start time is known), prepended to the race track.
- Course leg overlays (mark-to-mark line segments) when a course is assigned to the race.
- Start-line markers for the pin end and the boat end when the API provides them.
- A highlighted segment of the track corresponding to the currently selected time window
  (see Time Window Slicer). When no window is selected the plain track is shown.

The map auto-fits its view to the full track on load.

#### 5.2 Controls (right column, top)

##### Time Window Slicer

A dual-handle range slider spanning the full duration of the race. The slider covers the race duration only (it does not extend before or after the race). The user drags the left and right handles to define a sub-window of interest. The user can also drag the entire window (the bar between the two handles) to shift it in time; the window silently clamps when it reaches either boundary.

Moving either handle:

- Highlights the corresponding segment on the map.
- Resamples and updates the telemetry charts to cover only the selected window.

##### Timeline Slicer

A single-handle scrubber also spanning the full race duration. Dragging the handle:

- Moves the crosshair cursor on all telemetry charts to the corresponding time.
- Updates the live-value gauges (in Current mode) to the interpolated telemetry at that time.

Both slicers are independent: the time window defines *what the charts show*, while the timeline
cursor defines *where the crosshair is* within that window.

##### Replay Controls

Play / pause buttons that automatically advance the timeline cursor. When playing, the timeline cursor moves forward and the charts/gauges update accordingly. When the cursor reaches the end of the selected time window it automatically loops back to the beginning of that window. When paused, the user can still move the timeline cursor manually.
There is also a speed multiplier dropdown (e.g. 0.5×, 1×, 2×, 4×) that controls how fast the timeline cursor advances relative to real time.

##### Mode Toggle

A two-button toggle that switches the data panel between:

- **Historical** – shows telemetry charts.
- **Current** – shows live-value gauges.

#### 5.3 Telemetry Charts (Historical mode)

A set of time-series chart panels. Each panel has a labelled Y-axis and one or more data series.
The X-axis is shared across all panels and represents elapsed race time.

| Panel | Series | Unit |
| --- | --- | --- |
| Speed (SOG) | Speed over ground | User preference (kn / km/h) |
| Heading | Course over ground | Degrees |
| Wind | Wind speed · Wind direction | User preference (kn / m/s) · Degrees |
| Heel & Trim | Heel angle · Trim angle | Degrees |
| Speed Through Water | Speed through water | User preference *(shown only when data is present)* |
| Depth | Water depth | Metres *(shown only when data is present)* |
| Temperature | Water temperature | °C *(shown only when data is present)* |
| Load | Sensor load | Raw units *(shown only when data is present)* |
| Shift Angles | Wind shift angles | Degrees *(shown only when data is present)* |

All chart crosshairs are **locked together** – moving the crosshair on any one chart moves it on all others simultaneously. Moving the crosshair also updates the Timeline Slicer position, and vice versa (bidirectional synchronisation).

**Derived channels** (computed from raw telemetry, not stored in the API):

| Channel | Formula |
| --- | --- |
| Heel | Derived from IMU quaternion (positive = starboard heel) |
| Trim | Derived from IMU quaternion (positive = bow up) |

#### 5.4 Gauges (Current mode)

Four analogue-style gauges showing the interpolated values at the cursor position:

| Gauge | Value |
| --- | --- |
| Speed | Speed over ground (user unit) |
| Heading | Heading in degrees, compass-style |
| Heel | Heel angle in degrees |
| Trim | Trim angle in degrees |

For the Heading gauge, there needs to be two display modes:

- **Numeric** – just show the heading value in degrees (e.g. "135°").
- **Compass** – show the heading on a compass rose, with a pointer indicating the direction. The compass should be oriented so that the boat's current course is always pointing up, and the rose rotates around it to show the heading relative to the course.

For the Heel and Trim gauges, there needs to be two display modes:

- **Numeric** – just show the angle value in degrees (e.g. "15°").
- **Inclinometer** – show the angle on a semicircular gauge, with a pointer indicating the angle. The gauge should be oriented so that 0° is in the center, positive angles (e.g. starboard heel) point to the right, and negative angles (e.g. port heel) point to the left. Heel is typically a larger angle (e.g. up to 45° or more), while Trim is usually smaller (e.g. up to 5°), so the gauge scales should reflect that.

#### 5.5 Optional telemetry visibility

Chart panels for optional channels (Speed Through Water, Depth, Temperature, Load, Shift Angles) are **hidden entirely** when the API returns no data for that channel. They are never shown in a collapsed or disabled state.

#### 5.6 Loading behaviour

All data fetches for the Race Viewer are fired **in parallel** when the page loads. Each data source (positions, course, wind, speed-through-water, depth, temperature, load, shift angles) loads independently and shows its own skeleton placeholder until its response arrives. This means parts of the page become interactive as soon as their data is ready, without waiting for slower channels.

The frontend **caches each response** keyed by `(sessionId, raceNumber, channel)`. Navigating back to the same race reuses the cached data without re-fetching, making reloads near-instant.

**API used:**

- `GET /api/sessions/{id}/races/{n}`
- `GET /api/sessions/{id}/races/{n}/positions`
- `GET /api/courses/{courseId}`
- `GET /api/sessions/{id}/races/{n}/wind`
- `GET /api/sessions/{id}/races/{n}/speed-through-water`
- `GET /api/sessions/{id}/races/{n}/depth`
- `GET /api/sessions/{id}/races/{n}/temperature`
- `GET /api/sessions/{id}/races/{n}/load`
- `GET /api/sessions/{id}/races/{n}/shift-angles`

All telemetry endpoints accept optional `from` / `to` query parameters (seconds relative to race start) so only the visible time window needs to be (re-)fetched when the user adjusts the Time Window Slicer.

---

### 6. Boats — `/boats`

Full CRUD management of boats.

**List table columns:** Name · Sail Number · Boat Class · Description · Actions (Edit / Delete)

Clicking a boat's **Name** navigates to the Boat Detail page.

**Create:** A *+ New Boat* button adds an inline editable row at the top of the table.

**Edit:** Clicking *Edit* on a row turns all cells into inputs:

- Name – text input
- Sail Number – text input (optional)
- Boat Class – dropdown populated from the Boat Classes list
- Description – text input (optional)

**Delete:** Removes the boat.

Every boat must be assigned to a boat class.

**API used:** `GET /api/boats` · `POST /api/boats` · `PUT /api/boats/{id}` · `DELETE /api/boats/{id}`
`GET /api/boatclasses` (for the dropdown)

---

### 6a. Boat Detail — `/boats/{id}`

Detail and statistics page for a single boat.

**Breadcrumb:** Fleet → *(boat name)*

**Metadata panel:**

| Field | Notes |
| --- | --- |
| Name | |
| Sail Number | Only shown when present |
| Boat Class | Links to the Boat Classes section |
| Description | Only shown when present |

**Statistics cards:**

| Card | Value |
| --- | --- |
| Sessions | Total number of sessions this boat has been used in |
| Races | Total number of races sailed |
| Total Distance | Total distance sailed across all sessions (user's preferred unit) |
| Top Speed | All-time top speed recorded (user's preferred unit) |
| Total Time Sailed | Accumulated time on the water across all sessions |
| Total Race Time | Accumulated time spent racing (race start to finish only) |

**API used:**

- `GET /api/boats/{id}`
- `GET /api/boats/{id}/stats`

---

### 7. Boat Classes — `/boat-classes`

Full CRUD management of boat class templates.

**List table columns:** Name · LOA (m) · Beam (m) · Weight (kg) · Actions (Edit / Delete)

**Create / Edit form** (shown as a panel below the list or in a side panel):

| Field | Input type |
| --- | --- |
| Name | Text |
| LOA – length overall | Number (metres, 2 decimal places) |
| Beam | Number (metres, 2 decimal places) |
| Weight | Number (kg, 1 decimal place) |
| Bowsprit | Number (metres, 2 decimal places) |
| **Sails** | Sub-table – each row has: Name (text) · Area in m² (number). Rows can be added and removed. |

**API used:** `GET /api/boatclasses` · `POST /api/boatclasses` · `PUT /api/boatclasses/{id}` · `DELETE /api/boatclasses/{id}`

---

### 8. Marks — `/marks`

Full CRUD management of physical course marks (buoys, fixed points on the water).

**List table columns:**

| Column | Notes |
| --- | --- |
| Name | |
| Latitude | Decimal degrees, 6 decimal places |
| Longitude | Decimal degrees, 6 decimal places |
| Active From | Date (optional) |
| Active Until | Date (optional) |
| Description | Optional |
| Actions | Edit / Delete |

The *Active From* / *Active Until* date range lets the same named mark have different positions
in different seasons. Filtering by `activeOn` date or `activeOnly` flag is supported.

Editing is done inline in the table row.

**API used:** `GET /api/marks` · `POST /api/marks` · `PUT /api/marks/{id}` · `DELETE /api/marks/{id}`

---

### 9. Courses — `/courses`

Full CRUD management of racing course definitions.

**List table columns:** Name · Year · Legs (count) · Distance · Description · Actions (Edit / Delete)

The **Distance** column shows the total length of the course, calculated as the sum of straight-line distances between the GPS coordinates of consecutive marks. The value is displayed in the user's preferred Course Length unit (NM / km / m).

**Create / Edit form** (shown as a panel):

| Field | Input type |
| --- | --- |
| Name | Text |
| Year | Number (integer) |
| Description | Text (optional) |
| **Legs** | Sub-table (see below) |

**Legs sub-table** – defines the sequence of mark roundings that make up the course:

| Column | Notes |
| --- | --- |
| # | Row number (display only) |
| Mark | Dropdown – select from known marks |
| Leg Name | Text (optional label for the leg, e.g. "Beat to windward") |
| Leg Distance | Calculated straight-line distance from the previous mark to this mark (display only). Shown in the user's preferred Course Length unit. Empty for the first leg (no previous mark). |
| Actions | ↑ move up · ↓ move down · Remove |

Below the sub-table a **Total Distance** field displays the sum of all leg distances, updated live as marks are added, removed, or reordered.

Leg distances are computed from the GPS coordinates stored on each mark. If a selected mark has no coordinates, its leg distance is shown as "–".

Legs can be added with an *+ Add Leg* button, removed individually, and reordered with the arrow buttons.

**API used:** `GET /api/courses` · `POST /api/courses` · `PUT /api/courses/{id}` · `DELETE /api/courses/{id}`
`GET /api/marks` (for the mark dropdown)

---

### 10. Settings — `/settings`

User display preferences. Preferences are stored locally in the browser (e.g. `localStorage`) so
they persist between visits without requiring a user account.

**Preference fields:**

| Preference | Options | Default |
| --- | --- | --- |
| Boat Speed unit | Knots (kn), Kilometres per hour (km/h), Miles per hour (mph) | Knots |
| Wind Speed unit | Knots (kn), Metres per second (m/s) | Knots |
| Course Length unit | Metres (m), Kilometres (km), Nautical miles (NM) | Nautical miles (NM) |

The chosen units are applied everywhere values are displayed: chart Y-axis labels, gauge labels, and the stats cards on the Dashboard.

**Unit conversion is display-only.** The API always returns data in SI / base units (metres per second for speeds, metres for distances, etc.). All conversions to the user's preferred units are performed in the frontend before rendering.

---

## API Summary

All data is read from and written to a REST API. The frontend communicates with the API via a
proxy on the same origin so the browser never calls the API host directly. All list endpoints
support optional query parameters for filtering.

| Domain | Endpoints used |
| --- | --- |
| Stats | `GET /api/stats/summary` |
| Sessions | `GET /api/sessions` , `GET /api/sessions/{id}` , `POST /api/sessions/upload` , `PATCH /api/sessions/{id}` , `DELETE /api/sessions/{id}` |
| Races | `GET /api/sessions/{id}/races` , `GET /api/sessions/{id}/races/{n}` , `PATCH /api/sessions/{id}/races/{n}` |
| Positions | `GET /api/sessions/{id}/races/{n}/positions` |
| Wind | `GET /api/sessions/{id}/races/{n}/wind` |
| Speed Through Water | `GET /api/sessions/{id}/races/{n}/speed-through-water` |
| Depth | `GET /api/sessions/{id}/races/{n}/depth` |
| Temperature | `GET /api/sessions/{id}/races/{n}/temperature` |
| Load | `GET /api/sessions/{id}/races/{n}/load` |
| Shift Angles | `GET /api/sessions/{id}/races/{n}/shift-angles` |
| Courses | `GET /api/courses` , `GET /api/courses/{id}` , `POST /api/courses` , `PUT /api/courses/{id}` , `DELETE /api/courses/{id}` |
| Marks | `GET /api/marks` , `GET /api/marks/{id}` , `POST /api/marks` , `PUT /api/marks/{id}` , `DELETE /api/marks/{id}` |
| Boat Classes | `GET /api/boatclasses` , `GET /api/boatclasses/{id}` , `POST /api/boatclasses` , `PUT /api/boatclasses/{id}` , `DELETE /api/boatclasses/{id}` |
| Boats | `GET /api/boats` , `GET /api/boats/{id}` , `GET /api/boats/{id}/stats` , `POST /api/boats` , `PUT /api/boats/{id}` , `DELETE /api/boats/{id}` |
