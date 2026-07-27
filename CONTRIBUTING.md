# 贡献指南

感谢你对 Mocha 的关注！本文件说明如何参与项目开发。

## 项目简介

Mocha 是一个基于 [OpenTelemetry](https://opentelemetry.io) 的 .NET APM 平台，提供统一的 Metrics / Logs / Traces 存储与查询。技术栈为 C# / .NET 8 / ASP.NET Core / gRPC / EF Core。

## 开发环境要求

- **.NET 8 SDK**（必需）
- **Git**（必需）
- **JDK 17+**（仅在构建 `Mocha.Antlr4.Generated` 时需要，用于 ANTLR4 代码生成）
- **Docker** & **Docker Compose**（用于本地运行集成环境）

## 克隆仓库

本仓库包含子模块（OTLP 协议定义），请使用 `--recursive` 克隆：

```bash
git clone --recursive https://github.com/dotnetcore/mocha.git
```

如果已经克隆但未初始化子模块：

```bash
git submodule update --init --recursive
```

## 构建

```bash
# 还原依赖
dotnet restore

# 构建第三方协议生成模块（需先构建）
dotnet build ./src/Mocha.Protocol.Generated

# 构建整个解决方案
dotnet build
```

## 运行测试

```bash
# 运行所有测试
dotnet test

# 以 Release 配置运行并收集代码覆盖率
dotnet test -c Release --collect:"XPlat Code Coverage"
```

## 代码格式化

项目使用 `.editorconfig` 定义代码风格，并通过 `dotnet format` 强制检查：

```bash
# 格式化代码
dotnet format

# 检查是否有格式问题（CI 中使用）
dotnet format --verify-no-changes --verbosity diagnostic
```

## 本地运行

使用 Docker Compose 启动完整环境（LiteDB 版本，无需额外数据库）：

```bash
cd docker
docker-compose up --build -d
```

启动后：
- Distributor（OTLP 接收）：`http://localhost:4317`
- Query（查询 API）：`http://localhost:5775`
- Grafana：`http://localhost:3000`（用户名/密码：admin / admin）

也可使用 MySQL + InfluxDB 版本：

```bash
docker-compose -f docker-compose-mysql-influxdb.yml up --build -d
```

## 分支策略

- `main` — 主分支，保护分支，所有变更通过 PR 合入
- 功能分支 — 从 `main` 拉出，命名建议：`feat/xxx`、`fix/xxx`、`docs/xxx`

## PR 流程

1. Fork 仓库并创建功能分支
2. 确保代码通过 `dotnet build`、`dotnet test`、`dotnet format --verify-no-changes`
3. 提交 PR，描述清楚变更内容与动机
4. 通过 CI 检查后，由维护者审核合入

## 提交信息规范

遵循 [Conventional Commits](https://www.conventionalcommits.org/) 规范：

```
<type>: <description>

<optional body>
```

常用类型：
- `feat` — 新功能
- `fix` — 缺陷修复
- `refactor` — 重构
- `docs` — 文档变更
- `test` — 测试相关
- `chore` — 构建/工具链变更
- `perf` — 性能优化
- `ci` — CI 配置变更

## 代码规范

- 遵循项目根目录的 `.editorconfig`
- 缩进：4 空格
- 最大行宽：120 字符
- 使用 `var` 而非显式类型
- 大括号换行（Allman 风格）
- 系统命名空间优先排序

## 寻求帮助

- 提交 [Issue](https://github.com/dotnetcore/mocha/issues) 讨论问题或提出建议
- 参与项目讨论

## 许可证

Mocha 采用 MIT 许可证，详见 [LICENSE](LICENSE)。
