# 变更日志 (Changelog)

本文件记录 Mocha 各版本的功能变更。格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.1.0/)。

## [Unreleased]

### 待完成
- 文档体系补齐
- CI 流程完善
- 性能基准测试

---

## [0.1.0] - 2026-07-28

初始 MVP 版本，实现基于 OTel Trace 的最小可用 APM 功能。

### 新增

#### 核心模块
- `Mocha.Distributor` — OTLP 数据入口，通过 gRPC 接收 Trace 与 Metrics（端口 4317）
- `Mocha.Query` — 查询服务，提供 Jaeger 与 Prometheus 兼容的 HTTP API（端口 5775）
- `Mocha.Core` — 核心数据模型、缓冲区、存储接口定义
- `Mocha.Storage` — 存储抽象层，支持多后端切换
- `Mocha.Antlr4.Generated` — 基于 ANTLR4 的 PromQL 语法解析
- `Mocha.Protocol.Generated` — OTLP 协议生成代码（子模块）

#### 存储后端
- LiteDB（嵌入式，适用于快速体验与小规模场景）
- MySQL（通过 EF Core，适用于 Trace 与元数据存储）
- InfluxDB（适用于 Metrics 存储）

#### 查询 API
- Jaeger 兼容 API：`/jaeger/api/services`、`/jaeger/api/traces` 等
- Prometheus 兼容 API：`/prometheus/api/v1/query`、`/prometheus/api/v1/query_range` 等
- PromQL 引擎：支持即时查询与范围查询

#### 部署
- Docker Compose 一键部署（LiteDB 版本）
- Docker Compose MySQL + InfluxDB 版本
- Grafana 数据源预配置（Jaeger + Prometheus）

#### 工程化
- .editorconfig 代码风格规范
- GitHub Actions CI（多 OS 构建、测试、格式化检查、代码覆盖率）
- CodeQL 安全扫描
- Docker 镜像构建
