# Mocha 版本规划 (Roadmap)

Mocha 遵循最小可行性产品 (MVP) 模式，从先实现一个能跑通 OTel Trace 流程的最小功能集开始，逐步演进为完整的可观测性平台。

## 版本概览

| 版本 | 定位 | 状态 |
|------|------|------|
| v1.0 | 基于 OTel Trace 的 APM 功能系统（MVP） | 进行中 |
| v2.0 | 基于 DSL 的流式 M.T.L 分析平台 | 规划中 |
| v3.0 | 大规模数据下的平台伸缩能力与低成本存储 | 规划中 |

---

## v1.0 — MVP（当前阶段）

目标：借助开源存储组件，实现基于 OTel Trace 的 APM 功能系统。

### 核心功能

- [x] OTel Trace 链路接收与存储（OTLP gRPC）
- [x] OTel Metrics 接收与存储
- [x] PromQL 查询引擎（基于 ANTLR4）
- [x] 多存储后端抽象（LiteDB / MySQL / InfluxDB）
- [x] Jaeger 兼容查询 API
- [x] Prometheus 兼容查询 API
- [x] Grafana 数据源对接
- [x] Docker Compose 一键部署
- [ ] 文档补齐
- [ ] CI 完善
- [ ] 性能基准测试

### 已实现模块

- `Mocha.Distributor` — OTLP 数据入口（gRPC，端口 4317）
- `Mocha.Query` — 查询服务（HTTP，端口 5775），兼容 Jaeger / Prometheus API
- `Mocha.Storage` — 存储抽象层，支持 LiteDB / EFCore(MySQL) / InfluxDB
- `Mocha.Core` — 核心模型、缓冲区、存储接口
- `Mocha.Antlr4.Generated` — PromQL 语法解析（ANTLR4）
- `Mocha.Protocol.Generated` — OTLP 协议生成代码

---

## v2.0 — 流式分析平台

目标：实现基于 DSL 的流式 M.T.L 分析平台，从 APM 演变为自定义的分析平台。

### 规划功能

- [ ] `Mocha.Streaming` 模块 — 流式数据处理核心
- [ ] DSL 流式聚合规则引擎
- [ ] 一致性 Hash 路由（Distributor → Streaming）
- [ ] ETCD 注册与服务发现
- [ ] 分布式 Shuffle 能力
- [ ] 本地 FIFO 队列（数据不丢失保障）
- [ ] R.E.D 指标自动聚合
- [ ] 服务拓扑图

---

## v3.0 — 大规模与低成本

目标：考虑大规模数据下的平台伸缩能力和存储成本，集中在架构性能和低成本 M.T.L 自定义存储上。

### 规划功能

- [ ] ClickHouse 存储后端
- [ ] VictoriaMetrics 存储后端
- [ ] 集群高可用（Distributor / Streaming / Query 集群化）
- [ ] Logs 存储与查询（兼容 Loki API）
- [ ] Alerts 报警规则管理与通知
- [ ] 基础设施监控（主机 / 容器 / Kubernetes）
- [ ] 数据降采样与冷热分层存储
- [ ] 水平扩展与自动伸缩

---

## 说明

- 标记 `[x]` 表示已完成，`[ ]` 表示待实现。
- 版本规划会根据实际进展动态调整，最新状态以本文件为准。
- 各版本的具体变更请参阅 [CHANGELOG.md](../CHANGELOG.md)。
