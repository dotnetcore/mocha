<!--
Licensed to the .NET Core Community under one or more agreements.
The .NET Core Community licenses this file to you under the MIT license.
-->

# Mocha end-to-end tests

This directory contains Mocha's end-to-end coverage, organized into **two tiers** that trade
fidelity for speed and isolation:

| Tier | What it proves | How it runs | Blocking? |
| --- | --- | --- | --- |
| **Tier 1** — full stack | The whole cross-process chain (OTLP/gRPC ingest → in-memory buffer → background `StorageExporter` → storage → Query read-back over Jaeger/Prometheus) works against real containers. | `docker compose` stack + a YAML-driven .NET console runner. | Intended to block PRs to `main`. |
| **Tier 2** — component | Individual storage adapters behave correctly against a **real** database (MySQL / InfluxDB) rather than the in-memory provider. | xUnit + [Testcontainers](https://dotnet.testcontainers.org/) (no compose stack). | Informational initially (`continue-on-error`). |

```
OTLP/gRPC ingest            in-memory buffer      background drain          read-back HTTP API
(Mocha.Distributor :4317) ->  (topic queues)  ->  (StorageExporter)  ->  storage  ->  (Mocha.Query :5775)
                                                                                          |- Jaeger endpoints
                                                                                          '- Prometheus endpoints
```

## Why a separate `Mocha.E2E.sln` (and why nothing here is in `Mocha.sln`)

`Mocha.sln` has two consumers that must stay untouched:

- CI runs `dotnet test` **sln-scoped** across the whole solution; an xUnit e2e project added there
  would run with no stack/containers available.
- The release Dockerfiles (`docker/{distributor,query}/Dockerfile`) `COPY src/ tests/ proto/` and
  then `dotnet restore Mocha.sln` — a solution entry pointing under `e2e/` would break the image
  build (the build context never copies `e2e/`).

The isolation is **structural, not filter-based**: everything here lives under `e2e/` and is wired
only into a separate `e2e/Mocha.E2E.sln`. No `dotnet test` trait filter or `.slnf` is involved.
The Tier-2 xUnit project therefore lives under `e2e/` (not `tests/`).

## Layout

```
e2e/
  Mocha.E2E.sln                     # e2e-only solution (the 3 e2e projects; never Mocha.sln)
  Mocha.E2E.Abstractions/           # shared building blocks (class lib, IsTestProject=false)
    TelemetryFactory.cs             #   neutral TelemetrySpec -> OTLP proto (owns the determinism tricks)
    TelemetrySpec.cs                #   neutral builder input (no dependency on the YAML POCO)
    OtlpSender.cs                   #   OTLP/gRPC sender (insecure h2c)
    QueryClient.cs                  #   poll-until-visible Jaeger/Prometheus read-back client
    Matchers.cs                     #   Jaeger/Prometheus assertion helpers over QueryClient
  Mocha.EndToEnd/                   # Tier 1 runner (console, IsTestProject=false)
    CaseSpec.cs                     #   YAML POCO -> TelemetrySpec
    Program.cs                      #   discover cases/*.yaml -> build -> send -> assert -> aggregate
  cases/                            # Tier 1 cases (drop a YAML file, no recompile)
    trace-and-gauge-happy-path.yaml
    multi-span-and-metrics.yaml
  Mocha.E2E.Component.Tests/        # Tier 2 xUnit (IsTestProject=true, Testcontainers)
  docker-compose.e2e.yml            # small override applied on top of the base compose files
  run-e2e.sh                        # Tier 1 orchestrator
  README.md
```

The console runner and the xUnit tests both consume `Mocha.E2E.Abstractions`, so the OTLP payload
builders, the read-back client and the matchers have exactly one definition.

## Tier 1 — full-stack, YAML-driven

The runner discovers every `cases/*.yaml` file, and for each one builds a small OTLP payload, sends
it to the distributor over OTLP/gRPC, then polls the Query API until the declared expectations are
satisfied. All cases run; the process exits non-zero if any case fails.

Writes are asynchronous and eventually consistent (the distributor buffers OTLP data in memory and a
hosted `StorageExporter` drains it to storage in the background). Every assertion therefore **polls
until visible** with a timeout instead of reading once.

### Case schema

```yaml
name: trace-and-gauge-happy-path
description: One server span + one gauge, asserted through Jaeger and Prometheus.
timeoutSeconds: 30
send:
  resource:
    serviceName: mocha-e2e        # runner appends a unique run suffix -> mocha-e2e-<runId>
    attributes: { service.instance.id: "${runId}" }
  spans:
    - id: root                    # symbolic id, referenced by expect.jaeger.traceById
      name: e2e-root-span
      kind: server                # unspecified | internal | server | client | producer | consumer
      status: ok                  # unset | ok | error
      durationMs: 1
      attributes: { http.method: GET, e2e.run_id: "${runId}" }
  metrics:
    - name: mocha_e2e_gauge       # runner appends the same unique suffix
      type: gauge                 # only 'gauge' is supported today
      unit: "1"                   # "1" maps to an empty stored unit (no name suffix)
      value: 42
      attributes: { e2e.run_id: "${runId}" }
expect:
  jaeger: { service: true, operations: [e2e-root-span], traceById: root }
  prometheus:
    - query: mocha_e2e_gauge      # base metric name; runner resolves it to the suffixed name
      expect: nonEmptyVector      # matcher vocabulary (see below)
```

Semantics:

- The runner generates a per-case `runId`, substitutes every `${runId}` token in attribute values,
  and appends the suffix to the service name and to each metric name. This keeps each run's data
  unique, so assertions are immune to leftover data from previous runs.
- All complex proto construction (16-byte trace id with a non-zero first byte, unix-nano timestamps,
  kind/status string→enum, unit `"1"`→empty) lives in `TelemetryFactory` (typed C#). YAML supplies
  only scalars, enum-as-string values, attribute maps, and matcher choices.
- `expect.jaeger` maps to the `QueryClient` waiters: `service` → `WaitForServiceAsync`, each
  `operations` entry → `WaitForOperationAsync`, and `traceById` (a symbolic span id) →
  `WaitForTraceByIdAsync` on that span's generated trace id.
- `expect.prometheus[].query` is resolved (base metric name → suffixed name, `${runId}` tokens
  substituted) and handed to the matcher. The matcher vocabulary starts at **`nonEmptyVector`**
  (what `QueryClient` proves against the running stack via `WaitForMetricAsync`).

### Adding a case

Drop a new `.yaml` file into `cases/`. **No recompile** — the runner copies `cases/*.yaml` next to
its output and discovers them at runtime (the discovery path is overridable via the first CLI
argument or `MOCHA_E2E_CASES_DIR`).

### Running locally

From the repository root (or anywhere — the script resolves its own paths):

```bash
# LiteDB stack (default)
e2e/run-e2e.sh

# MySQL + InfluxDB stack
e2e/run-e2e.sh --backend mysql-influxdb

# Custom assertion timeout, and keep the stack up afterwards for debugging
e2e/run-e2e.sh --backend litedb --timeout 45 --keep
```

Exit code `0` means the stack came up and every case passed. On failure the script prints
`docker compose ps` and the last 200 log lines from each container, then tears the stack down
(unless `--keep` was passed).

> **Local port note:** both backend variants publish host ports `4317`/`5775`, and the
> `mysql-influxdb` variant also publishes `3306`. If a host-native MySQL already occupies `:3306`,
> add a **local, uncommitted** compose override that remaps just the host port (e.g. `ports:
> !override [ "13306:3306" ]`) — the distributor/query reach MySQL over the compose network at
> `mysql:3306`, so only the host mapping needs to change. Do not commit port changes to the tracked
> compose files.

### Running the sender against an already-running stack

```bash
MOCHA_E2E_OTLP_ENDPOINT=http://localhost:4317 \
MOCHA_E2E_QUERY_BASEURL=http://localhost:5775 \
MOCHA_E2E_TIMEOUT_SECONDS=30 \
  dotnet run --project e2e/Mocha.EndToEnd/Mocha.EndToEnd.csproj -c Release
```

| Environment variable | Default | Meaning |
| --- | --- | --- |
| `MOCHA_E2E_OTLP_ENDPOINT` | `http://localhost:4317` | Distributor OTLP/gRPC endpoint (plaintext h2c). |
| `MOCHA_E2E_QUERY_BASEURL` | `http://localhost:5775` | Query API base URL. |
| `MOCHA_E2E_TIMEOUT_SECONDS` | `30` | Per-assertion poll timeout (overrides a case's `timeoutSeconds`). |
| `MOCHA_E2E_CASES_DIR` | `<app>/cases` | Directory to discover `*.yaml` cases in (also the first CLI arg). |

## Tier 2 — component tests (Testcontainers)

`Mocha.E2E.Component.Tests` spins up **real** databases with Testcontainers (not the compose stack)
and reuses the `Mocha.E2E.Abstractions` builders so no proto/model construction is duplicated. Each
backend container is a per-collection `IAsyncLifetime` fixture shared via `ICollectionFixture<T>`, so
it starts once and is reused. Because Testcontainers maps container ports to random host ports, these
tests never collide with a host-native database.

The first tests are:

1. **Storage adapter vs real MySQL** (`mysql:8.2.0`): round-trips a span through
   `ITelemetryDataWriter<MochaSpan>` (`EFSpanWriter`) → `EFJaegerSpanReader`, with the schema seeded
   from `scripts/mysql/init/*.sql` (read at runtime, not hand-copied).
2. **PromQL over real InfluxDB** (`influxdb:2.7.7`): writes a gauge through the real
   `ITelemetryDataWriter<MochaMetric>` (`InfluxDBMetricsWriter`), then runs a PromQL instant query
   through the production Query Prometheus path (`InfluxDbPrometheusMetricsReader` + the PromQL
   engine) and asserts the returned result vector.

### Running locally

```bash
dotnet test e2e/Mocha.E2E.Component.Tests/Mocha.E2E.Component.Tests.csproj -c Release
```

Requires a working Docker daemon. The tests pull `mysql:8.2.0`, `influxdb:2.7.7` and the
Testcontainers `ryuk` reaper image on first run.

## CI

`.github/workflows/e2e.yml` (ubuntu only):

- **Tier 1** — a `backend: [litedb, mysql-influxdb]` matrix that runs
  `e2e/run-e2e.sh --backend <backend>` (submodule init happens inside the script). Intended to block
  PRs to `main`.
- **Tier 2** — `dotnet test e2e/Mocha.E2E.Component.Tests/...`, marked `continue-on-error: true`
  (informational initially). Uses the Docker daemon on the ubuntu runner.

The existing `dotnet-build.yml` and `docker.yml` workflows are untouched.

## Prerequisites

- Docker with the Compose v2 plugin (`docker compose`) or legacy `docker-compose`.
- .NET SDK capable of building `net8.0` (the projects target `net8.0`).
- Network access to pull base images (mysql, influxdb, grafana, .NET SDK/runtime, Testcontainers
  ryuk) and to clone the `proto/otlp` git submodule.

## Troubleshooting

- **Submodule build errors** (`OpenTelemetry.Proto.*` not found): run
  `git submodule update --init --recursive`; the OTLP protos live in `proto/otlp`.
- **Readiness timeout on the MySQL variant**: MySQL first-boot initialization can take a while, and
  `depends_on` only waits for container start, not database readiness. The distributor/query rely on
  their `restart: always` policy to reconnect; the script polls the ports for up to 180s.
- **Port already in use**: a previous stack (either variant) is still running, or a host-native
  service occupies a published port. Tear the stack down with
  `docker compose -f docker/docker-compose.yml -f e2e/docker-compose.e2e.yml down -v`, or use the
  local port-remap override described above.
