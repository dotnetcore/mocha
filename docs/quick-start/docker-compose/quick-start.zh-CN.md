## 启动项目

在项目根目录下的docker目录中，执行以下命令启动项目：

```bash
docker-compose up --build -d
```
此版本的存储实现为 LiteDB，属于嵌入式数据库，因此不需要额外启动数据库容器。

启动成功后，可以看到以下容器：

+ mocha-distributor: 提供用于接收 OTLP 数据的 gRPC API
+ mocha-query: 提供用于接收查询协议的 HTTP API
+ mocha-grafana: 用于展示数据

我们目前也实现了 Tracing 存储到 MySQL 和 Metrics 存储到 InfluxDB 的版本。

可以执行 `docker-compose -f docker-compose-mysql-influxdb.yml up --build -d` 来启动使用 MySQL 和 InfluxDB 的版本。

启动成功后，可以看到以除了上述容器外，还有以下容器：

+ mocha-mysql: 用于存储数据
+ mocha-influxdb: 用于存储数据

## 数据的发送

将 SDK 的 OTLP exporter 配置为 `http://localhost:4317` 即可将数据发送到 distributor。

## 数据的查询

### Trace

#### 配置 Jaeger 数据源

我们实现了支持 Jaeger 查询协议的 API，因此可以直接在 Grafana 中配置 Jaeger 数据源。

访问 http://localhost:3000/ 即可看到grafana的登录页面。用户名和密码都是admin。

登录后，点击左侧的菜单，选择 Data Sources，然后点击 Add data source。

![](./assets/add-jaeger-data-source.png)

![](./assets/add-jaeger-data-source-2.png)

选择 Jaeger。

![](./assets/add-jaeger-data-source-3.png)

配置 Jaeger 数据源的 URL 为 `http://query:5775/jaeger` 。

数据源的名称可以自定义，示例中使用的是 mocha-tracing。

![](./assets/add-jaeger-data-source-4.png)

点击 Save & Test，如果显示如下信息，则说明配置成功。

![](./assets/add-jaeger-data-source-5.png)

如果还没往 distributor 发送过数据，会显示如下警告信息。

![](./assets/add-jaeger-data-source-warning.png)

#### Trace 数据的查询

点击左侧的菜单，选择 Explore，然后选择 mocha-tracing 数据源，即可看到 Trace 数据。

![](./assets/query-trace.png)

![](./assets/query-trace-2.png)

### Metrics

#### 配置 Prometheus 数据源

我们实现了支持 PromQL 查询协议的 API，因此可以直接在 Grafana 中配置 Prometheus 数据源。

访问 http://localhost:3000/ 即可看到grafana的登录页面。用户名和密码都是admin。

登录后，点击左侧的菜单，选择 Data Sources，然后点击 Add data source。

选择 Prometheus。

![](./assets/add-prometheus-data-source.png)

配置 Prometheus 数据源的 URL 为 `http://query:5775/prometheus` 。

数据源的名称可以自定义，示例中使用的是 mocha-metrics。

![](./assets/add-prometheus-data-source-2.png)

配置 HTTP Method 为 POST。

![](./assets/add-prometheus-data-source-3.png)

点击 Save & Test，如果显示如下信息，则说明配置成功。

![](./assets/add-prometheus-data-source-4.png)

#### Metrics 数据的查询

点击左侧的菜单，选择 Explore，然后选择 mocha-metrics 数据源，即可看到 Metrics 数据。

![](./assets/query-metrics.png)

点击左侧的菜单，选择 Dashboards，即可创建一个 Dashboard。

![](./assets/create-metrics-dashboard.png)

![](./assets/create-metrics-dashboard-2.png)

选择我们刚刚创建的 Prometheus 数据源。

![](./assets/create-metrics-dashboard-3.png)

接下来就可以根据需要添加 Panel，展示 Metrics 数据了。

![](./assets/create-metrics-dashboard-4.png)
