# System Design & Domain Model - Dotnet Services Viewer

## 1. Domain-Driven Design (DDD)

### Ubiquitous Language
- **Node:** A physical or virtual server reachable via SSH.
- **Service:** Any monitored entity (Docker, Podman, Web Endpoint, DB). Unified term for consistency.
- **Agent:** The logic within the app that connects to nodes to fetch data.
- **Status:** The current health state (Up, Down, Warning, Maintenance).
- **Heartbeat:** A single check event with metrics and status.

### Entities & Aggregates
- **Node (Aggregate Root):**
    - `Id`, `Hostname`, `IpAddress`, `SshConfig`.
    - List of `Containers`.
- **Container (Entity):**
    - `Id`, `Name`, `Type` (Systemd, Docker, Podman, Web), `CheckInterval`.
    - `LastKnownStatus`.
- **LogEntry (Value Object):**
    - `Timestamp`, `Message`, `Level`.

---

## 2. System Design (SDD)

### Architecture: Extended MVC
We keep the MVC structure for the UI, but we add a **Domain Layer** to separate monitoring logic from controllers.

- **Presentation (MVC):** Controllers (`NodeController`, `DashboardController`), Views (Razor + SignalR for real-time updates).
- **Application (Services):** `MonitoringService` (Background worker that orchestrates checks).
- **Domain:** Pure logic, Entities, and Interfaces.
### Persistence (Database)
- **Engine:** SQLite (single file for simplicity and portability).
- **History Policy:**
    - **Persisted:** Status changes (Up/Down events), resource usage snapshots (CPU, RAM).
    - **Not Persisted:** Service logs. Logs will be fetched in real-time via SSH and streamed to the UI, but won't be stored in the database to keep the database lightweight.

- **Infrastructure:** SSH Client implementation, Docker/Podman command parsers, Entity Framework Core (SQLite).

### Data Flow
1. `MonitoringService` (Background) picks a `Container` to check.
2. It uses an `ISshClient` to run `podman/docker stats` or `systemctl status`.
3. The parser converts terminal output into `Metric` objects.
4. Historical status and resource data is saved to SQLite; real-time updates are pushed via SignalR.

---

## 3. TDD Strategy

- **Project:** `dotnet_services_viewer.Tests` (xUnit).
- **First Test Case:** Validate that a `Container` correctly calculates its "Warning" state based on resource thresholds.
- **Mocking:** We will mock the SSH connection to simulate different server responses without needing a real server.
