# 部署文档

本文档说明如何在生产环境部署 Mocha，以及如何选择合适的存储后端。

## 部署方式

### Docker Compose（推荐）

Mocha 提供两种 Docker Compose 配置：

#### 1. LiteDB 版本（快速体验 / 小规模）

```bash
cd docker
docker-compose up --build -d
```

包含服务：
- `mocha-distributor` — OTLP 接收（端口 4317）
- `mocha-query` — 查询服务（端口 5775）
- `mocha-grafana` — 可视化（端口 3000）

LiteDB 为嵌入式数据库，数据存储在挂载的 `./litedb_data` 卷中，无需额外数据库容器。

#### 2. MySQL + InfluxDB 版本（生产建议）

```bash
cd docker
docker-compose -f docker-compose-mysql-influxdb.yml up --build -d
```

包含服务：
- `mocha-distributor` — OTLP 接收（端口 4317）
- `mocha-query` — 查询服务（端口 5775）
- `mocha-grafana` — 可视化（端口 3000）
- `mocha-mysql` — 元数据与链路追踪存储（端口 3306）
- `mocha-influxdb` — 指标存储（端口 8086）

### 自定义部署

Distributor 与 Query 均为独立的 ASP.NET Core 应用，可单独部署：

- Distributor 镜像：基于 `mcr.microsoft.com/dotnet/aspnet:8.0`
- Query 镜像：基于 `mcr.microsoft.com/dotnet/aspnet:8.0`（构建阶段需要 JDK 17 用于 ANTLR4）

镜像构建参见 `docker/distributor/Dockerfile` 与 `docker/query/Dockerfile`。

---

## 存储后端选择指南

| 存储 | 适用数据 | 适用场景 | 优点 | 缺点 |
|------|---------|---------|------|------|
| **LiteDB** | Metadata / Tracing / Metrics | 快速体验、开发测试、小规模 | 零依赖、嵌入式、部署简单 | 不适合高并发、大规模生产 |
| **MySQL (EFCore)** | Metadata / Tracing | 生产环境链路追踪 | 成熟稳定、生态完善、事务支持 | 大规模写入性能有限 |
| **InfluxDB** | Metrics | 生产环境指标存储 | 时序数据优化、写入性能好 | 需额外维护 InfluxDB 集群 |

### 选择建议

- **本地开发 / 功能验证**：LiteDB，零配置启动
- **中小规模生产**：MySQL（Trace）+ InfluxDB（Metrics）
- **大规模生产**：等待 v3.0 的 ClickHouse / VictoriaMetrics 支持

---

## 端口清单

| 服务 | 端口 | 协议 | 说明 |
|------|------|------|------|
| Distributor | 4317 | gRPC | OTLP 数据接收 |
| Query | 5775 | HTTP | Jaeger / Prometheus 查询 API |
| Grafana | 3000 | HTTP | 可视化面板 |
| MySQL | 3306 | TCP | 关系型数据库（可选） |
| InfluxDB | 8086 | HTTP | 时序数据库（可选） |

---

## 资源需求建议

### 最小配置（LiteDB 版本）

| 资源 | 建议 |
|------|------|
| CPU | 2 核 |
| 内存 | 4 GB |
| 磁盘 | 10 GB（取决于数据量） |

### 推荐配置（MySQL + InfluxDB 版本）

| 服务 | CPU | 内存 |
|------|-----|------|
| Distributor | 2 核 | 2 GB |
| Query | 2 核 | 2 GB |
| MySQL | 2 核 | 4 GB |
| InfluxDB | 2 核 | 4 GB |
| Grafana | 1 核 | 1 GB |

---

## 数据持久化

### LiteDB

数据存储在挂载的目录中，需持久化 `./litedb_data` 卷：

```yaml
volumes:
  - ./litedb_data:/data/litedb
```

### MySQL

持久化 MySQL 数据目录（默认注释，生产环境请取消注释）：

```yaml
volumes:
  - ./mysql:/var/lib/mysql
```

初始化脚本位于 `scripts/mysql/init/`，会在首次启动时自动执行。

### InfluxDB

持久化 InfluxDB 数据目录（默认注释，生产环境请取消注释）：

```yaml
volumes:
  - ./influxdb:/var/lib/influxdb2
```

### Grafana

持久化 Grafana 数据（默认注释，生产环境请取消注释）：

```yaml
volumes:
  - ./grafana:/var/lib/grafana
```

---

## 配置

所有配置通过环境变量传递，详见 [配置参考](./configuration.md)。

---

## 安全建议

- 不要将 4317 / 5775 端口直接暴露到公网
- 使用反向代理（Nginx）并启用 TLS
- 为 MySQL / InfluxDB 设置强密码
- 定期更新镜像
- 限制 Grafana 管理账户权限
