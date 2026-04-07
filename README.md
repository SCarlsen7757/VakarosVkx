# Vakaros VKX Analyser

A self-hosted sailing telemetry analysis tool for [Vakaros](https://vakaros.com/) devices. Upload your `.vkx` log files, explore GPS tracks on an interactive map, and review race telemetry through live playback and historical charts — all from your own machine.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Architecture](#architecture)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Running with Docker Compose](#running-with-docker-compose)
  - [Running Locally (Development)](#running-locally-development)
- [Usage](#usage)
  - [Uploading a Session](#uploading-a-session)
  - [Managing Boats and Courses](#managing-boats-and-courses)
  - [Viewing a Race](#viewing-a-race)
- [Projects](#projects)
- [Roadmap](#roadmap)
- [Contributing](#contributing)

---

## Overview

Vakaros devices record sailing telemetry — GPS position, speed, heading, heel, VMG, and more — into compact binary `.vkx` log files. This project provides:

- A **parser** that decodes the VKX binary format into structured data
- A **REST API** that ingests, stores, and serves the telemetry
- A **Blazor web UI** for interactive visualisation of sessions and races
- A **console tool** for quick one-off conversion of `.vkx` files to JSON

---

## Features

| Feature | Description |
|---|---|
| 📤 **File Upload** | Upload `.vkx` files via the API; duplicate detection via SHA-256 hash |
| 🏁 **Automatic Race Detection** | Races are extracted automatically from the timer events embedded in each session |
| 🗺️ **Interactive Map** | GPS track rendered on a Leaflet map with course marks, start line (pin end / boat end) and leg overlays |
| 📈 **Telemetry Charts** | Synced time-series charts for speed, VMG, and heel powered by amCharts |
| 🎛️ **Live Gauges** | Heading, speed, VMG, and heel/angle gauges with scrubbing and playback |
| ⏯️ **Playback Modes** | *Historical* mode shows full-race charts with a synced cursor; *Current* mode shows live-style gauges you can scrub through |
| ⛵ **Boats** | Register boats with name, sail number, and class; link them to sessions |
| 📍 **Marks & Courses** | Define race-course marks and build ordered course legs; overlay them on any race map |
| 🐳 **Self-hosted** | One `docker compose up` starts the database and API |

---

## Architecture

```
┌──────────────────────────┐      HTTP/JSON      ┌────────────────────────────┐
│  Vakaros.Vkx.Web         │ ──────────────────► │  Vakaros.Vkx.Api           │
│  Blazor Server (.NET 10) │                     │  ASP.NET Core (.NET 10)    │
└──────────────────────────┘                     └─────────────┬──────────────┘
                                                               │ EF Core
                                                               ▼
                                                 ┌────────────────────────────┐
                                                 │  TimescaleDB (PostgreSQL)  │
                                                 └────────────────────────────┘
                                                               ▲
                                                               │
┌──────────────────────────┐      parse          ┌─────────────┴──────────────┐
│  Vakaros.Vkx.Parser      │ ◄────────────────── │  VkxIngestionService       │
│  Binary VKX decoder      │                     │  (inside the API)          │
└──────────────────────────┘                     └────────────────────────────┘

┌──────────────────────────┐
│  Vakaros.Vkx.Shared      │  ← DTOs shared between API and Web
└──────────────────────────┘

┌──────────────────────────┐
│  Vakaros.Vkx.Console     │  ← Standalone CLI: .vkx → .json
└──────────────────────────┘
```

---

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (for the compose stack)
- **or** [.NET 10 SDK](https://dotnet.microsoft.com/download) (for local development)

### Running with Docker Compose

```bash
docker compose up --build
```

This starts:
- **TimescaleDB** on port `5432`
- **API** on port `8080`

The Blazor web UI is a separate project and is not yet included in the compose file. See [Running Locally](#running-locally-development) to run it alongside the API.

### Running Locally (Development)

1. Start the database:

```bash
docker compose up db
```

2. Run the API:

```bash
cd Vakaros.Vkx.Api
dotnet run
```

3. Run the web UI (in a separate terminal):

```bash
cd Vakaros.Vkx.Web
dotnet run
```

Open your browser at the URL printed by the web project (typically `https://localhost:5001`).

---

## Usage

### Uploading a Session

Use any HTTP client to `POST` a `.vkx` file to the API:

```bash
curl -X POST http://localhost:8080/api/sessions/upload \
     -F "file=@my-session.vkx"
```

The API parses the file, detects races, and returns a `SessionDetailDto` with all metadata.

### Managing Boats and Courses

Boats, marks, and courses can be managed via the REST API:

| Resource | Endpoint |
|---|---|
| Boats | `GET/POST /api/boats`, `PUT/DELETE /api/boats/{id}` |
| Marks | `GET/POST /api/marks`, `PUT/DELETE /api/marks/{id}` |
| Courses | `GET/POST /api/courses`, `PUT/DELETE /api/courses/{id}` |
| Sessions | `GET /api/sessions`, `PATCH /api/sessions/{id}` (link boat/course) |

### Viewing a Race

1. Navigate to **Sessions** in the web UI.
2. Click a session to see its detail and list of detected races.
3. Click a race to open the **Race Viewer**:
   - The map shows the GPS track with course marks overlaid.
   - Switch between **Historical** (full-race charts with synced cursor) and **Current** (gauge-style scrubbing) modes.

### Console Tool

Convert a single `.vkx` file to JSON without running any server:

```bash
cd Vakaros.Vkx.ConsoleApplication
dotnet run
# Enter .vkx file path: C:\path\to\session.vkx
```

The output JSON is written next to the source file.

---

## Projects

| Project | Type | Purpose |
|---|---|---|
| `Vakaros.Vkx.Parser` | Class library | Decodes the VKX binary format (v1.4) into typed C# records |
| `Vakaros.Vkx.Api` | ASP.NET Core Web API | Ingestion, storage, race detection, REST endpoints |
| `Vakaros.Vkx.Web` | Blazor Server | Interactive web UI — map, charts, gauges, playback |
| `Vakaros.Vkx.Shared` | Class library | DTOs shared between the API and web projects |
| `Vakaros.Vkx.ConsoleApplication` | Console app | Standalone CLI converter: `.vkx` → `.json` |

---

## Roadmap

- [ ] **Web UI in Docker Compose** — add the Blazor frontend to the compose stack for a single-command self-hosted deployment
- [ ] **Start timing** — when a start line is available, detect the exact moment the boat crosses it and calculate the elapsed time from the start signal to the crossing
- [ ] **Mark-to-mark VMG** — calculate VMG towards the next course mark when a course is assigned to a session, replacing the current upwind/downwind approximation
- [ ] **Performance benchmarks** — compare speed, VMG, and tacking angles across multiple sessions on the same course
- [ ] **Polar diagram** — plot boat speed against true wind angle to build an empirical polar curve, if wind data is available
- [ ] **Session comparison** — overlay two or more race tracks on the same map
- [ ] **Enhanced telemetry** — surface wind, speed-through-water, depth, and load sensor data in the UI
- [ ] **Weather data** — fetch historic weather conditions (wind speed, wind direction, temperature, precipitation, cloud cover) from an external weather API and overlay them on race sessions
- [ ] **AI-generated reports** — automated post-race analysis and coaching insights powered by a language model (e.g. OpenAI / Claude)

---

## Contributing

Pull requests are welcome. For significant changes please open an issue first to discuss what you would like to change.

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/my-feature`)
3. Commit your changes (`git commit -m 'Add my feature'`)
4. Push to the branch (`git push origin feature/my-feature`)
5. Open a Pull Request
