Mocha
=====

[![.NET Build](https://github.com/dotnetcore/mocha/actions/workflows/dotnet-build.yml/badge.svg)](https://github.com/dotnetcore/mocha/actions/workflows/dotnet-build.yml)
[![codecov](https://codecov.io/gh/dotnetcore/mocha/branch/main/graph/badge.svg?token=v9OE7dV8ZS)](https://codecov.io/gh/dotnetcore/mocha)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](./LICENSE)

[English](./README.md) | 简体中文

Mocha 是一个基于 [OpenTelemetry](https://opentelemetry.io) 的 APM 系统，同时提供可伸缩的可观测性数据分析和存储平台。

**注意：使用 `git clone --recursive` 克隆本仓库以及子模块。**

## 快速开始
现阶段，我们提供了 Docker Compose 文件，方便用户在本地体验我们的系统。

+ [快速开始](./docs/quick-start/docker-compose/quick-start.zh-CN.md)

## 平台功能

Mocha 遵循 MVP 模式：从能跑通 OpenTelemetry Trace 流程的最小功能集开始，逐步演进为完整的可观测性平台。下图展示了目标功能架构，各能力的实现状态以 [版本规划 (Roadmap)](./docs/ROADMAP.md) 为准。

![](./docs/assets/functional_architecture.png)

### 已实现 (v0.1.0)

- OTLP Trace 链路接收与存储（OTLP gRPC）
- OTLP Metrics 接收与存储
- PromQL 查询引擎（基于 ANTLR4）
- 多存储后端抽象（LiteDB / MySQL / InfluxDB）
- Jaeger 兼容查询 API
- Prometheus 兼容查询 API
- Grafana 数据源对接
- Docker Compose 一键部署

### 规划中 (v2.0 / v3.0)

- 基于 DSL 规则引擎的流式聚合平台（`Mocha.Streaming` / Aggregator）
- 服务拓扑与 R.E.D 指标自动聚合
- 日志查询与分析
- 报警：规则管理与通知
- 基础设施监控：主机、容器与 Kubernetes
- Mocha Manager：管理服务、Dashboard 与集群元数据
## 技术架构
![](./docs/assets/technical_architecture.png)

Mocha 整体架构由下面的部分组成
- Mocha Distributor Cluster：作为 mocha 系统的数据入口，负责接收 OTel SDK 和 Collector 上报的数据，并通过一致性Hash 将数据路由到对应的 aggregator 节点上。为了保证数据不丢失，最终 Distributor 应该具备本地 FIFO 队列的能力。
- Mocha Streaming Cluster：mocha 的核心组件，通过读取预配置或者用户配置的 aggr rule dsl 生成对应的 streaming data flow 并执行。Streaming 是具备分布式 shuffle 的能力的有状态组件，需要将自身信息注册到ETCD中。
- Storage：mocha M.T.L 存储，可以选用开源存储组件，如 ClickHouse、ElasticSearch、victoriametrics 等。
- Mocha Querier + Grafana: 从存储查询数据并提供给 grafana 做展示。因此要兼容 promql / jeager / loki 等数据源的查询。
- Mocha Manager : 包括 manager server、dashboard和ETCE组件，集群元数据和 M.T.L 数据分析规则存储。
- OTel SDK / Collector : 开源 OpenTelemetry 采集套件。

## 文档

- [文档索引](./docs/README.md) — 快速开始、用户指南、架构设计与开发者文档。
- [版本规划 (Roadmap)](./docs/ROADMAP.md) — v1.0 / v2.0 / v3.0 的目标与完成状态。

## 参与贡献
参与贡献的最简单的方式是参与讨论并讨论问题。您也可以通过提交代码更改的拉取请求来进行贡献。

## 许可证
Mocha 是在 MIT 许可下发布的。有关详细信息，请参阅 [LICENSE](LICENSE) 文件。
