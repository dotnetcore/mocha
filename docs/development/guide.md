# 开发指南

本文档面向 Mocha 的开发者，说明项目结构、本地开发环境搭建、调试、测试与扩展方法。

## 项目结构

```
mocha/
├── src/
│   ├── Mocha.Core/                  # 核心：模型、缓冲区、存储接口
│   │   ├── Buffer/                  # 内存缓冲区
│   │   ├── Models/                  # 数据模型（Metadata / Metrics / Trace）
│   │   ├── Extensions/              # 扩展方法
│   │   └── Storage/                 # 存储接口定义（Jaeger / Prometheus）
│   ├── Mocha.Distributor/           # OTLP 数据入口（gRPC :4317）
│   │   ├── Services/                # gRPC 服务（Trace / Metrics 接收）
│   │   └── Exporters/               # 数据导出（写入存储）
│   ├── Mocha.Query/                 # 查询服务（HTTP :5775）
│   │   ├── Jaeger/                  # Jaeger 兼容 API
│   │   └── Prometheus/              # Prometheus 兼容 API + PromQL 引擎
│   ├── Mocha.Storage/               # 存储抽象层
│   │   ├── LiteDB/                  # LiteDB 实现
│   │   ├── EntityFrameworkCore/     # MySQL (EF Core) 实现
│   │   └── InfluxDB/                # InfluxDB 实现
│   ├── Mocha.Protocol.Generated/    # OTLP 协议生成代码（子模块）
│   ├── Mocha.Antlr4.Generated/      # PromQL 语法生成代码（ANTLR4）
│   └── Mocha.Streaming/             # 流式处理（未实现，仅有 csproj）
├── tests/
│   ├── Mocha.Core.Tests/            # 核心单元测试
│   ├── Mocha.Query.Tests/           # 查询服务测试
│   ├── Mocha.Storage.Tests/         # 存储测试
│   ├── Mocha.Core.Benchmarks/       # 核心基准测试
│   ├── Mocha.Query.Benchmarks/      # 查询基准测试
│   └── Mocha.Storage.Benchmarks/    # 存储基准测试
├── docker/                          # Docker 与 Compose 配置
├── proto/                           # OTLP 协议定义（子模块）
├── scripts/                         # 辅助脚本（如 MySQL 初始化）
└── docs/                            # 文档
```

## 本地开发环境搭建

### 前置要求

- .NET 8 SDK
- Git
- JDK 17+（仅构建 `Mocha.Antlr4.Generated` 时需要）
- Docker & Docker Compose（可选，用于运行集成环境）

### 克隆与初始化

```bash
git clone --recursive https://github.com/dotnetcore/mocha.git
cd mocha
```

### 构建

```bash
dotnet restore
dotnet build ./src/Mocha.Protocol.Generated   # 先构建协议生成模块
dotnet build                                   # 构建整个解决方案
```

## 如何调试各组件

### 调试 Distributor

1. 在 IDE 中将 `Mocha.Distributor` 设为启动项目
2. 默认监听 `http://localhost:4317`（gRPC）
3. 使用 OTel SDK 或 `otel-cli` 发送测试数据
4. 可通过 gRPC 反射工具（如 grpcurl）调试接口

### 调试 Query

1. 在 IDE 中将 `Mocha.Query` 设为启动项目
2. 默认监听 `http://localhost:5775`（HTTP）
3. 开发环境下访问 `http://localhost:5775/swagger` 查看 API 文档
4. 使用 Postman / curl 调用 Jaeger / Prometheus API

### 使用 Docker Compose 调试

```bash
cd docker
docker-compose up --build -d
```

启动后：
- Distributor：`http://localhost:4317`
- Query：`http://localhost:5775`
- Grafana：`http://localhost:3000`（admin / admin）

## 如何运行测试

```bash
# 运行所有测试
dotnet test

# 运行指定项目的测试
dotnet test tests/Mocha.Core.Tests

# 以 Release 配置运行并收集覆盖率
dotnet test -c Release --collect:"XPlat Code Coverage"
```

## 如何添加新的存储后端

以添加一个新的 Metrics 存储后端为例：

1. 在 `Mocha.Storage/` 下创建新目录，如 `Mocha.Storage/NewStorage/`
2. 实现 `Mocha.Core/Storage/` 中定义的接口：
   - 写入：`ITelemetryDataWriter`
   - 查询：`IPrometheusMetricsReader` / `IPrometheusMetricsMetadataReader`（Metrics）
   - 查询：`IJaegerSpanReader` / `IJaegerSpanMetadataReader`（Tracing）
3. 在 `Mocha.Storage/MetricsStorageProvider.cs`（或对应 Provider 类）中添加新的提供者标识
4. 在 `Mocha.Storage/MetricsStorageOptionsBuilder.cs` 中添加 `UseNewStorage()` 扩展方法
5. 在 `Mocha.Distributor/Program.cs` 与 `Mocha.Query/Program.cs` 的 `switch` 中添加新分支
6. 编写测试并验证

## 如何运行基准测试

```bash
# 运行 Core 基准测试
dotnet run -c Release --project tests/Mocha.Core.Benchmarks

# 运行 Query 基准测试
dotnet run -c Release --project tests/Mocha.Query.Benchmarks

# 运行 Storage 基准测试
dotnet run -c Release --project tests/Mocha.Storage.Benchmarks
```

基准测试基于 BenchmarkDotNet，结果会输出到控制台与 `BenchmarkDotNet.Artifacts/` 目录。

## 代码规范

- 遵循 `.editorconfig`（4 空格缩进、最大行宽 120、使用 var、Allman 大括号风格）
- 提交前运行 `dotnet format` 确保格式一致
- CI 会执行 `dotnet format --verify-no-changes` 检查

## 常见问题

### 构建 `Mocha.Protocol.Generated` 失败

确保已初始化子模块：
```bash
git submodule update --init --recursive
```

### 构建 `Mocha.Antlr4.Generated` 失败

确保已安装 JDK 17+，ANTLR4 代码生成需要 Java 运行时。

### gRPC 在 macOS 上无法连接

macOS 上 gRPC 需要额外配置 Kestrel，参见 [微软文档](https://go.microsoft.com/fwlink/?linkid=2099682)。
