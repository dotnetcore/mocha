# API 文档

Mocha 提供两类 API：OTLP 数据接收 API（Distributor）和查询 API（Query）。

## 服务端口

| 服务 | 端口 | 协议 | 说明 |
|------|------|------|------|
| Distributor | 4317 | gRPC (HTTP/2) | 接收 OTLP Trace / Metrics 数据 |
| Query | 5775 | HTTP/1.1 | 提供 Jaeger 与 Prometheus 兼容查询 API |

---

## 一、OTLP 接收 API（Distributor）

Distributor 通过 gRPC 暴露 OpenTelemetry 协议 (OTLP) 接口，用于接收 SDK / Collector 上报的数据。

### 端点

| gRPC 服务 | 说明 |
|-----------|------|
| `OTelTraceExportService` | 接收 OTLP Trace 数据 |
| `OTelMetricsExportService` | 接收 OTLP Metrics 数据 |

### 配置

- 地址：`http://<host>:4317`
- 协议：HTTP/2（gRPC 必需）
- 启用了 gRPC 反射服务，便于调试

### 使用方式

将 OTel SDK / Collector 的 OTLP exporter 配置为 Distributor 地址即可：

```
http://localhost:4317
```

---

## 二、Jaeger 兼容查询 API（Query）

Query 服务实现了 Jaeger 查询协议，可作为 Grafana 的 Jaeger 数据源使用。

基础路径：`/jaeger/api`

### 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/jaeger/api/services` | 获取所有服务名称列表 |
| GET | `/jaeger/api/services/{serviceName}/operations` | 获取指定服务的操作列表 |
| GET | `/jaeger/api/traces` | 按条件查询链路（支持 service、operation、tags、时间范围、traceID 等参数） |
| GET | `/jaeger/api/traces/{traceID}` | 按 Trace ID 查询完整链路 |

### 查询参数（`/traces`）

| 参数 | 说明 |
|------|------|
| `service` | 服务名称 |
| `operation` | 操作名称 |
| `tags` | 标签过滤（JSON 格式） |
| `start` | 起始时间（微秒时间戳） |
| `end` | 结束时间（微秒时间戳） |
| `lookback` | 回溯时间（如 `1h`、`30m`） |
| `minDuration` | 最小持续时间 |
| `maxDuration` | 最大持续时间 |
| `limit` | 返回链路数量上限 |
| `traceID` | 指定 Trace ID 查询 |

### Grafana 数据源配置

- 类型：Jaeger
- URL：`http://<query-host>:5775/jaeger`

---

## 三、Prometheus 兼容查询 API（Query）

Query 服务实现了 Prometheus HTTP API（参考 LTS 版本 2.45），可作为 Grafana 的 Prometheus 数据源使用。

基础路径：`/prometheus/api/v1`

### 端点

| 方法 | 路径 | 说明 |
|------|------|------|
| GET | `/prometheus/api/v1/metadata` | 获取指标元数据 |
| GET | `/prometheus/api/v1/labels` | 获取标签名称列表 |
| GET | `/prometheus/api/v1/label/{labelName}/values` | 获取指定标签的值列表 |
| POST | `/prometheus/api/v1/series` | 查询时间序列（暂未实现） |
| GET/POST | `/prometheus/api/v1/query` | 即时查询（Instant Query） |
| GET/POST | `/prometheus/api/v1/query_range` | 范围查询（Range Query） |
| GET/POST | `/prometheus/api/v1/status/buildinfo` | 获取构建信息 |

### 即时查询 `/query`

| 参数 | 说明 |
|------|------|
| `query` | PromQL 表达式（必需） |
| `time` | 评估时间戳（可选，默认为当前时间） |
| `timeout` | 超时时间（可选，默认 30s） |
| `limit` | 返回序列数量上限（可选） |

### 范围查询 `/query_range`

| 参数 | 说明 |
|------|------|
| `query` | PromQL 表达式（必需） |
| `start` | 起始时间戳（必需） |
| `end` | 结束时间戳（必需） |
| `step` | 查询步长（必需，如 `15s`、`60`） |
| `timeout` | 超时时间（可选，默认 120s） |
| `limit` | 返回序列数量上限（可选） |

### 响应格式

所有 Prometheus API 返回统一的 JSON 格式：

```json
{
  "status": "success" | "error",
  "data": {
    "resultType": "matrix" | "vector" | "scalar",
    "result": [...]
  },
  "errorType": "...",
  "error": "..."
}
```

### Grafana 数据源配置

- 类型：Prometheus
- URL：`http://<query-host>:5775/prometheus`
- HTTP Method：POST

---

## 注意事项

- Query 服务在开发环境（`ASPNETCORE_ENVIRONMENT=Development`）下启用 Swagger UI，可访问 `/swagger` 查看接口文档。
- Prometheus API 的 `series` 端点当前返回 `NotImplementedException`，将在后续版本实现。
- PromQL 引擎基于 ANTLR4 实现，支持常用的查询语法。
