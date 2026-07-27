# AGENTS.md

Guidance for AI agents working in the **Mocha** repository (`dotnetcore/mocha`).
All paths below are relative to the repository root unless stated otherwise.

## 1. Project Overview

Mocha is an **Application Performance Monitoring (APM) platform built on [OpenTelemetry](https://opentelemetry.io)**, providing a scalable pipeline for ingesting, storing, and querying observability data (traces, metrics, metadata).

- **Language / Runtime:** C# 12 on **.NET 8** (`net8.0`; no `global.json`, SDK pinned to `8.0.x` in CI).
- **Web stack:** ASP.NET Core — gRPC (`Grpc.AspNetCore` 2.76.0) for ingest, Web API + Swagger (`Swashbuckle.AspNetCore` 6.4.0) for query.
- **Storage backends (pluggable, per-signal):** LiteDB 5.0.21 (embedded default), EF Core 8.0.5 + `Pomelo.EntityFrameworkCore.MySql` 8.0.2 (MySQL), InfluxDB (`InfluxDB.Client` 4.16.0).
- **Codegen:** OTLP protobuf via `Google.Protobuf` 3.35.1 + `Grpc.Tools` 2.76.0 (from a git submodule); PromQL parser via `Antlr4.Runtime.Standard` 4.13.1 (requires JDK 17+ to regenerate).
- **Data flow:** `Mocha.Distributor` (OTLP gRPC ingest) → in-memory `BufferQueue` → `StorageExporter` hosted service → pluggable `Mocha.Storage`; `Mocha.Query` serves PromQL- and Jaeger-compatible read APIs over the same storage.

> **CRITICAL:** This repo uses a git submodule (`proto/otlp`). Clone with `git clone --recursive`, or run `git submodule update --init --recursive` before building — otherwise `Mocha.Protocol.Generated` will not build.

## 2. Project Structure Map

| Directory | Purpose | Local Documentation |
|-----------|---------|---------------------|
| `src/Mocha.Core/` | Shared domain models (`Models/Trace`, `Models/Metrics`, `Models/Metadata`), in-memory `Buffer/Memory` queue (`IBufferQueue`, `MemoryBufferQueue`), storage abstraction interfaces, OTel→Mocha conversion extensions | – |
| `src/Mocha.Distributor/` | ASP.NET Core **gRPC host** — OTLP export services (`OTelTraceExportService`, `OTelMetricsExportService`) + `StorageExporter` background service draining the buffer to storage | – |
| `src/Mocha.Query/` | ASP.NET Core **Web API host** — Prometheus/PromQL (`Prometheus/PromQL/Engine`) and Jaeger query surfaces | – |
| `src/Mocha.Storage/` | Storage providers: `LiteDB/`, `EntityFrameworkCore/` (MySQL), `InfluxDB/`; per-signal provider selection | – |
| `src/Mocha.Streaming/` | Planned streaming/aggregator tier (`RootNamespace=Mocha.Aggregator`) — **csproj only, no source yet** | – |
| `src/Mocha.Protocol.Generated/` | Compiles upstream OTLP `.proto` (from `proto/otlp` submodule) into C# | – |
| `src/Mocha.Antlr4.Generated/` | ANTLR4-generated PromQL lexer/parser from `PromoQL/*.g4` (needs JDK 17+) | – |
| `tests/` | xUnit unit tests (`Mocha.Core.Tests`, `Mocha.Storage.Tests`, `Mocha.Query.Tests`) + BenchmarkDotNet suites (`*.Benchmarks`) | – |
| `proto/otlp/` | Git submodule → `open-telemetry/opentelemetry-proto` (OTLP definitions) | – |
| `docker/` | `docker-compose.yml` (LiteDB), `docker-compose-mysql-influxdb.yml`, `distributor/` + `query/` Dockerfiles, Grafana provisioning | – |
| `scripts/mysql/init/` | MySQL seed scripts (`trace.sql`, `metadata.sql`) | – |
| `docs/` | Architecture, deployment, development guide, quick-start, proposals, roadmap | `docs/development/guide.md` |

Entry points (runnable hosts): `src/Mocha.Distributor/Program.cs` (gRPC, top-level statements) and `src/Mocha.Query/Program.cs` (Web API). Both use minimal hosting — no explicit `Main`.

## 3. Build & Development Commands

Run from the repository root. Commands are transcribed from `.github/workflows/dotnet-build.yml`, `CONTRIBUTING.md`, and `docs/development/guide.md`.

```bash
# 0. Submodules are REQUIRED (proto/otlp) — do this first, once.
git submodule update --init --recursive

# 1. Restore
dotnet restore

# 2. Build — build the generated protocol module FIRST, then the whole solution.
dotnet build ./src/Mocha.Protocol.Generated
dotnet build

# 3. Format (see Testing section for the CI check form)
dotnet format
```

Notes:
- Build ordering is load-bearing: `dotnet build ./src/Mocha.Protocol.Generated` must precede the full `dotnet build`.
- All documented commands use bare `dotnet build` / `dotnet test` (no explicit `Mocha.sln` argument, no `-c Release` on build). Do **not** invent a `dotnet build Mocha.sln -c Release` form.
- `Mocha.Antlr4.Generated` requires **JDK 17+** installed to regenerate the PromQL parser.

Local run via Docker Compose (from the `docker/` directory; docs use the legacy hyphenated `docker-compose`):

```bash
cd docker
docker-compose up --build -d                                        # default LiteDB stack
docker-compose -f docker-compose-mysql-influxdb.yml up --build -d   # MySQL + InfluxDB variant
```

Endpoints after startup: Distributor OTLP gRPC `:4317`, Query API `:5775` (Swagger at `/swagger` in Development), Grafana `:3000` (admin/admin); MySQL variant adds MySQL `:3306` and InfluxDB `:8086`.

## 4. Testing Instructions

Test framework: **xUnit** (`xunit` 2.9.2, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk` 17.11.1) with **FluentAssertions** 6.12.1; `Mocha.Query.Tests` also uses **Moq** 4.20.72, `Mocha.Storage.Tests` uses EF Core InMemory. Tests use `[Fact]` / `[Theory]`.

```bash
# All tests
dotnet test

# CI form: Release + coverage (coverlet). Codecov patch target 98% is ENFORCED; project target 70% is informational.
dotnet test -c Release --collect:"XPlat Code Coverage"

# A single test project
dotnet test tests/Mocha.Core.Tests
dotnet test tests/Mocha.Query.Tests
dotnet test tests/Mocha.Storage.Tests

# A single class / test (standard xUnit dotnet-test filter syntax)
dotnet test tests/Mocha.Core.Tests --filter "FullyQualifiedName~Mocha.Core.Tests.Conversions.OTelToMochaMetricConversionExtensionsTests"
```

Benchmarks are BenchmarkDotNet and run via `dotnet run` (not `dotnet test`):

```bash
dotnet run -c Release --project tests/Mocha.Core.Benchmarks -- --filter "*"
```

## 5. Git Workflow

- **Trunk:** `main` is protected — **(CRITICAL)** all changes land via PR, never push directly to `main`.
- **Branches:** cut from `main`, named `feat/xxx`, `fix/xxx`, or `docs/xxx`.
- **Commits:** **Conventional Commits** — `<type>: <description>` (optional body). Types: `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `perf`, `ci`.
- **PR requirements** (`CONTRIBUTING.md`): fork + feature branch; the change must pass `dotnet build`, `dotnet test`, and `dotnet format --verify-no-changes`; describe the change and motivation; pass CI; get maintainer review before merge.
- **Required CI checks** (on PR to `main`): `.NET Build` (Ubuntu/Windows/macOS matrix — includes format gate + tests + coverage), `CodeQL` (C#), and `Docker Build & Scan` (Trivy runs with `exit-code: 0`, so it does not fail the build).
- No DCO/CLA sign-off, no PR template, and no `CODEOWNERS` file exist in this repo.

## 6. Code Style Guidelines

Enforced by `.editorconfig` (severity `:suggestion`) + `dotnet format --verify-no-changes` in CI (a hard CI failure) + in-build .NET analyzers (`EnforceCodeStyleInBuild=true`, `AnalysisLevel=8.0`). `Nullable` and `ImplicitUsings` are **enabled**. `max_line_length = 120`, 4-space indent.

**Every `.cs` file MUST start with the MIT license header** (also declared via `file_header_template` in `.editorconfig`):

```csharp
// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.
```

Use `var`; file-scoped namespaces; Allman braces; braces even on single-line blocks:

```csharp
// ✅ Correct
namespace Mocha.Core.Buffer;

public class BufferConsumer
{
    private readonly IBufferQueue _queue;   // private field: _camelCase with leading underscore

    public void Consume()
    {
        var batch = _queue.Pull();          // var required
        if (batch.IsEmpty)
        {
            return;                         // braces required even here
        }
    }
}
```

```csharp
// ❌ Wrong
namespace Mocha.Core.Buffer {              // block-scoped namespace
    public class BufferConsumer {          // brace on same line (K&R)
        private IBufferQueue queue;         // missing _ prefix
        public void Consume() {
            BufferBatch batch = queue.Pull(); // explicit type instead of var
            if (batch.IsEmpty) return;        // missing braces
        }
    }
}
```

Other rules: `System.*` usings sorted first; no `this.` qualifier; predefined types (`int`, not `Int32`); constant fields PascalCase. Note: `TreatWarningsAsErrors` and `LangVersion` are **not** set — compiler warnings do not fail the build, but the `dotnet format` gate does.

## 7. Boundaries & Guardrails

- ✅ **Always** run `git submodule update --init --recursive` before building, and `dotnet build ./src/Mocha.Protocol.Generated` before the full build.
- ✅ **Always** prefix new `.cs` files with the MIT license header, use file-scoped namespaces, `var`, and `_camelCase` private fields.
- ✅ **Always** run `dotnet format --verify-no-changes` and `dotnet test` before opening a PR — both are hard CI gates.
- ✅ **Always** keep new/changed code covered — the Codecov **patch target is 98% (enforced)**.
- ⚠️ **Ask first** before changing storage schemas (`scripts/mysql/init/*.sql`, EF Core models), the OTLP/gRPC service contracts, or Docker Compose topology — these are cross-cutting.
- ⚠️ **Ask first** before starting work inside `src/Mocha.Streaming/` (unimplemented) or upgrading the target framework / major dependencies.
- 🚫 **Never** commit into or hand-edit `proto/otlp/` (it is an upstream submodule) or files under `src/*.Generated/` output.
- 🚫 **Never** push directly to `main` or bypass the PR + review flow.
- 🚫 **Never** commit secrets, credentials, or local DB/data volumes produced by `docker-compose`.

## 8. Related Documentation

- `README.md` / `README.zh-CN.md` — project intro, feature set, architecture diagrams.
- `CONTRIBUTING.md` — contribution flow, commit/PR conventions.
- `docs/development/guide.md` — detailed local dev, build ordering, common issues.
- `docs/architecture/overview.md` — technical architecture.
- `docs/quick-start/` — Docker Compose quick start.
- `docs/ROADMAP.md`, `docs/proposal/` — direction and design proposals.
- `SECURITY.md` — security policy.

No top-level `deepwiki/` exists in this repository.
