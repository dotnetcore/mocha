# 架构概览

## 系统定位

Mocha 是一个基于 OpenTelemetry 的 APM 系统，提供统一的 Metrics / Logs / Traces 存储与查询平台。当前为 MVP 阶段，已实现 OTel Trace 与 Metrics 的接收、存储和查询。

## 组件说明

Mocha 由以下核心组件组成：

### Mocha Distributor（数据入口）

- 作为系统的数据入口，接收 OTel SDK 和 Collector 上报的数据
- 通过 gRPC 暴露 OTLP 接口（端口 4317）
- 接收 Trace（`OTelTraceExportService`）与 Metrics（`OTelMetricsExportService`）
- 使用内存缓冲区对数据进行削峰填谷，再异步写入存储
- 未来版本将通过一致性 Hash 将数据路由到 Streaming 节点

### Mocha Streaming（流式处理，未实现）

- 核心组件，通过读取预配置或用户配置的 aggr rule DSL 生成流式数据流并执行
- 有状态组件，具备分布式 Shuffle 能力
- 需将自身信息注册到 ETCD
- 当前版本（v1.0）尚未实现，数据由 Distributor 直接写入存储

### Mocha Query（查询服务）

- 从存储查询数据并提供给 Grafana 展示
- 兼容 Jaeger 查询 API（`/jaeger/api/*`）
- 兼容 Prometheus 查询 API（`/prometheus/api/v1/*`）
- 内置基于 ANTLR4 的 PromQL 引擎
- HTTP 服务，端口 5775

### Mocha Storage（存储抽象层）

- 提供统一的存储接口，支持多后端切换
- 三类数据分别配置：Metadata / Tracing / Metrics
- 已实现后端：
  - **LiteDB** — 嵌入式，适用于快速体验
  - **EFCore (MySQL)** — 适用于 Metadata 与 Tracing
  - **InfluxDB** — 适用于 Metrics

### Mocha Manager（管理组件，未实现）

- 包括 manager server、dashboard 和 ETCD
- 存储集群元数据和 M.T.L 数据分析规则
- 当前版本（v1.0）尚未实现

### OTel SDK / Collector

- 开源 OpenTelemetry 采集套件
- 负责从应用中采集 Trace / Metrics / Logs 并上报到 Distributor

## 数据流

```
OTel SDK / Collector
        |
        v (OTLP gRPC, 端口 4317)
Mocha Distributor
        |
        | (内存缓冲区削峰)
        v
[Mocha Streaming]  ← v2.0 实现，当前跳过
        |
        v
Mocha Storage (LiteDB / MySQL / InfluxDB)
        |
        v
Mocha Query (HTTP, 端口 5775)
        |
        v (Jaeger / Prometheus API)
Grafana
```

**当前 v1.0 数据流**：OTel SDK → Distributor → Storage → Query → Grafana

**v2.0 目标数据流**：OTel SDK → Distributor → Streaming → Storage → Query → Grafana

## 存储抽象层设计

存储层通过 `Mocha.Storage` 项目提供统一的扩展接口：

```
AddStorage()
    .WithMetadata(...)   // 配置元数据存储
    .WithTracing(...)    // 配置链路追踪存储
    .WithMetrics(...)    // 配置指标存储
```

每类数据可独立选择存储后端，通过配置项 `Xxx:Storage:Provider` 切换。

存储接口定义在 `Mocha.Core/Storage/` 中：
- `ITelemetryDataWriter` — 数据写入接口
- `IJaegerSpanReader` / `IJaegerSpanMetadataReader` — Jaeger 查询接口
- `IPrometheusMetricsReader` / `IPrometheusMetricsMetadataReader` — Prometheus 查询接口

各存储后端在 `Mocha.Storage/` 下实现对应接口：
- `Mocha.Storage/LiteDB/`
- `Mocha.Storage/EntityFrameworkCore/`
- `Mocha.Storage/InfluxDB/`

## 部署拓扑

### 单机部署（LiteDB，快速体验）

```
┌─────────────────────────────────────────┐
│              Docker Compose             │
│                                         │
│  ┌──────────────┐   ┌──────────────┐   │
│  │ Distributor  │   │    Query     │   │
│  │  :4317       │   │  :5775       │   │
│  └──────┬───────┘   └──────┬───────┘   │
│         │                  │           │
│         ▼                  ▼           │
│  ┌─────────────────────────────────┐   │
│  │     LiteDB (共享数据卷)          │   │
│  └─────────────────────────────────┘   │
│                                         │
│  ┌──────────────┐                      │
│  │   Grafana    │ :3000                │
│  └──────────────┘                      │
└─────────────────────────────────────────┘
```

### 生产部署（MySQL + InfluxDB）

```
┌──────────────────────────────────────────────────┐
│                  Docker Compose                   │
│                                                   │
│  ┌──────────────┐   ┌──────────────┐             │
│  │ Distributor  │   │    Query     │             │
│  │  :4317       │   │  :5775       │             │
│  └──┬───────┬───┘   └──┬───────┬───┘             │
│     │       │          │       │                 │
│     ▼       ▼          ▼       ▼                 │
│  ┌─────┐ ┌────────┐ ┌─────┐ ┌────────┐          │
│  │MySQL│ │InfluxDB│ │MySQL│ │InfluxDB│          │
│  │ :3306│ │ :8086  │ │ :3306│ │ :8086  │          │
│  └─────┘ └────────┘ └─────┘ └────────┘          │
│                                                   │
│  ┌──────────────┐                                │
│  │   Grafana    │ :3000                          │
│  └──────────────┘                                │
└──────────────────────────────────────────────────┘
```

## 技术架构图

参见 `docs/assets/technical_architecture.png`。
