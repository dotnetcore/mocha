Mocha
=====

[![codecov](https://codecov.io/gh/dotnetcore/mocha/branch/main/graph/badge.svg?token=v9OE7dV8ZS)](https://codecov.io/gh/dotnetcore/mocha)

English | [简体中文](./README.zh-CN.md)

Mocha is an application performance monitor tools based on [OpenTelemetry](https://opentelemetry.io), which also provides a scalable platform for observability data analysis and storage.

**Note: Use `git clone --recursive` to clone this repository with submodules.**

## Quick Start
In the beta phase, we provide a Docker Compose file for users to experience our system locally.

+ [Quick Start](./docs/quick-start/docker-compose/quick-start.en-US.md)

## Functional Architecture
![](./docs/assets/functional_architecture.png)

The set of features that Mocha will provide:
- APM and distributed tracing
  - Service overview, R.E.D metrics, and availability monitoring
  - Service topology
  - Endpoints monitoring, including HTTP, RPC, Cache, DB, MQ, etc.
  - Traces query
- Infrastructure monitoring
  - Host monitoring
  - Container and Kubernetes monitoring
- Logs
  - Log query
  - Log analysis
- Alerts
  - Alert rule management
  - Alert notifications
- Metrics/Logs/Traces data explore

## Technical Architecture
![](./docs/assets/technical_architecture.png)

The components of Mocha are as follows:
- Mocha Distributor Cluster: As the gateway of the Mocha system, it is responsible for receiving data reported by OTel SDK and Collectors and routed them to the corresponding aggregator nodes through consistent hashing. To ensure data is not lost, the Distributor should eventually have the ability to locally store data in a FIFO queue.
- Mocha Streaming Cluster: The core component of Mocha, which generates corresponding streaming data flows and executes them by reading the pre-configured or user-configured aggr rule DSL. Streaming is a stateful component with the ability to distribute shuffled data and needs to register its information in ETCD.
- Storage: Mocha MTL storage, which can use open-source storage components such as ClickHouse, Elastic Search, and victoriametrics.
- Mocha Querier + Grafana: Querying data from storage and providing it to Grafana for display. Therefore, it is necessary to compatibility with promql/jeager/loki and other data sources.
- Mocha Manager : Consisting of a manager server, dashboard, and ETCD for cluster metadata and data analysis rules storage.
- OTel SDK / Collector : Open-source OpenTelemetry collection kits

## Contribute
One of the easiest ways to contribute is to participate in discussions and discuss issues. You can also contribute by submitting pull requests with code changes.

## License
Mocha is under the MIT license. See the [LICENSE](LICENSE) file for details.
