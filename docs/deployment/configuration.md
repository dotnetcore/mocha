# 配置参考

Mocha 通过环境变量或 `appsettings.json` 进行配置。Distributor 与 Query 服务共享相同的存储配置结构。

## 配置层级

配置按以下优先级加载（高优先级覆盖低优先级）：

1. 命令行参数
2. 环境变量
3. `appsettings.{Environment}.json`
4. `appsettings.json`

环境变量中使用 `__`（双下划线）替代 JSON 中的层级分隔符 `:`。例如 `Metadata__Storage__Provider` 对应 `Metadata:Storage:Provider`。

---

## 存储提供者选择

Mocha 对三类数据分别配置存储后端：元数据 (Metadata)、链路追踪 (Tracing)、指标 (Metrics)。

### Metadata 存储

| 环境变量 | 可选值 | 说明 |
|----------|--------|------|
| `Metadata__Storage__Provider` | `LiteDB` / `EFCore` | 元数据存储提供者 |

### Tracing 存储

| 环境变量 | 可选值 | 说明 |
|----------|--------|------|
| `Tracing__Storage__Provider` | `LiteDB` / `EFCore` | 链路追踪存储提供者 |

### Metrics 存储

| 环境变量 | 可选值 | 说明 |
|----------|--------|------|
| `Metrics__Storage__Provider` | `LiteDB` / `InfluxDB` | 指标存储提供者 |

---

## 各存储连接配置

### LiteDB

适用于元数据、链路追踪、指标。嵌入式数据库，无需额外服务。

| 环境变量 | 说明 | 示例 |
|----------|------|------|
| `Metadata__Storage__LiteDB__DatabasePath` | 元数据数据库路径 | `/data/litedb` |
| `Tracing__Storage__LiteDB__DatabasePath` | 链路追踪数据库路径 | `/data/litedb` |
| `Metrics__Storage__LiteDB__DatabasePath` | 指标数据库路径 | `/data/litedb` |

### EFCore (MySQL)

适用于元数据与链路追踪。

| 环境变量 | 说明 | 示例 |
|----------|------|------|
| `Metadata__Storage__EFCore` | 元数据 MySQL 连接字符串 | `server=mysql;port=3306;database=mocha;userid=mocha;password=mocha` |
| `Tracing__Storage__EFCore` | 链路追踪 MySQL 连接字符串 | `server=mysql;port=3306;database=mocha;userid=mocha;password=mocha` |

### InfluxDB

适用于指标。

| 环境变量 | 说明 | 示例 |
|----------|------|------|
| `Metrics__Storage__InfluxDB__Url` | InfluxDB 服务地址 | `http://influxdb:8086` |
| `Metrics__Storage__InfluxDB__Token` | 访问 Token | `mocha_influxdb_token` |
| `Metrics__Storage__InfluxDB__Org` | 组织名称 | `mocha_org` |
| `Metrics__Storage__InfluxDB__Bucket` | 存储桶名称 | `mocha_metrics` |

---

## 端口配置

### Distributor（OTLP 接收）

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `Kestrel__Endpoints__OTelGrpcEndpoint__Url` | `http://*:4317` | gRPC 监听地址，用于接收 OTLP 数据 |
| `Kestrel__EndpointDefaults__Protocols` | `Http2` | 必须为 Http2 以支持 gRPC |

### Query（查询服务）

| 配置项 | 默认值 | 说明 |
|--------|--------|------|
| `Kestrel__Endpoints__QueryEndpoint__Url` | `http://*:5775` | HTTP 监听地址，提供 Jaeger / Prometheus 查询 API |

---

## 缓冲区配置

Distributor 使用内存缓冲区对 OTLP 数据进行削峰填谷。缓冲区主题与消费者数量在 `Program.cs` 中配置：

| 主题 | 消费者数量 | 说明 |
|------|-----------|------|
| `otlp-span-metadata` | 1 | Span 元数据 |
| `otlp-metric-metadata` | 1 | Metric 元数据 |
| `otlp-span` | `Environment.ProcessorCount` | Span 数据 |
| `otlp-metric` | `Environment.ProcessorCount` | Metric 数据 |

---

## 日志配置

| 环境变量 | 默认值 | 说明 |
|----------|--------|------|
| `Logging__LogLevel__Default` | `Information` | 默认日志级别 |
| `Logging__LogLevel__Microsoft.AspNetCore` | `Warning` | ASP.NET Core 日志级别 |
| `Logging__LogLevel__Microsoft.EntityFrameworkCore` | - | EF Core 日志级别（建议设为 `Warning`） |

---

## 其他

| 环境变量 | 说明 | 示例 |
|----------|------|------|
| `ASPNETCORE_ENVIRONMENT` | 运行环境 | `Production` / `Development` |
| `AllowedHosts` | 允许的主机名 | `*` |

---

## 完整示例

### LiteDB 版本（快速体验）

```bash
ASPNETCORE_ENVIRONMENT=Production
Metadata__Storage__Provider=LiteDB
Tracing__Storage__Provider=LiteDB
Metrics__Storage__Provider=LiteDB
Metadata__Storage__LiteDB__DatabasePath=/data/litedb
Tracing__Storage__LiteDB__DatabasePath=/data/litedb
Metrics__Storage__LiteDB__DatabasePath=/data/litedb
```

### MySQL + InfluxDB 版本（生产建议）

```bash
ASPNETCORE_ENVIRONMENT=Production
Metadata__Storage__Provider=EFCore
Tracing__Storage__Provider=EFCore
Metrics__Storage__Provider=InfluxDB
Metadata__Storage__EFCore=server=mysql;port=3306;database=mocha;userid=mocha;password=mocha
Tracing__Storage__EFCore=server=mysql;port=3306;database=mocha;userid=mocha;password=mocha
Metrics__Storage__InfluxDB__Url=http://influxdb:8086
Metrics__Storage__InfluxDB__Token=mocha_influxdb_token
Metrics__Storage__InfluxDB__Org=mocha_org
Metrics__Storage__InfluxDB__Bucket=mocha_metrics
Logging__LogLevel__Microsoft.EntityFrameworkCore=Warning
```
