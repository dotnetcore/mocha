# Mocha 文档

Mocha 是一个基于 [OpenTelemetry](https://opentelemetry.io) 的 APM 系统，同时提供可伸缩的可观测性数据分析和存储平台。

本文档按受众分类，帮助你快速找到所需内容。

## 目录

### 快速开始

- [Docker Compose 快速开始](./quick-start/docker-compose/quick-start.zh-CN.md) — 使用 LiteDB 存储在本地一键启动 Mocha，体验 Trace 与 Metrics 查询。

### 用户指南

- [部署与配置](./deployment/deployment.md) — 生产环境部署、存储后端选择、端口清单与数据持久化。
- [配置参考](./deployment/configuration.md) — 所有环境变量与配置选项的完整说明。
- [API 文档](./api/overview.md) — OTLP 接收、Jaeger 兼容查询、Prometheus 兼容查询 API。

### 架构设计

- [架构概览](./architecture/overview.md) — 系统组件、数据流、存储抽象层与部署拓扑。

### 开发者文档

- [开发指南](./development/guide.md) — 项目结构、本地开发、调试、测试与基准测试。
- [贡献指南](../CONTRIBUTING.md) — 开发环境、构建测试、分支策略与代码规范。

### 项目信息

- [版本规划 (Roadmap)](./ROADMAP.md) — v1.0 / v2.0 / v3.0 的目标与完成状态。
- [变更日志 (Changelog)](../CHANGELOG.md) — 各版本的功能变更记录。
- [安全政策 (Security)](../SECURITY.md) — 漏洞报告流程与受支持版本。
- [许可证](../LICENSE) — MIT License。

## 相关资源

- [项目主页](https://github.com/dotnetcore/mocha)
- [OpenTelemetry 文档](https://opentelemetry.io/docs/)
- [Prometheus 查询 API](https://prometheus.io/docs/prometheus/latest/querying/api/)
- [Jaeger 文档](https://www.jaegertracing.io/docs/)
