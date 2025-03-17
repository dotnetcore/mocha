## Start the Project

Execute the following command in the docker directory under the project root directory to start the project:

```bash
docker-compose up --build -d
```

After the startup is successful, you can see the following containers:

+ distributor: Provides gRPC API for receiving OTLP data
+ query: Provides HTTP API for receiving query protocol
+ mysql: Used to store data
+ grafana: Used to display data

## Send Data

Configure the OTLP exporter of the SDK as `http://localhost:4317` to send data to the distributor.

## Query Data

### Trace

#### Configure Jaeger Data Source

We have implemented an API that supports the Jaeger query protocol, so you can configure the Jaeger data source directly in Grafana.

Visit http://localhost:3000/ to see the grafana login page. Both the username and password are admin.

After logging in, click the menu on the left, select Data Sources, and then click Add data source.

![](./assets/add-jaeger-data-source.png)

![](./assets/add-jaeger-data-source-2.png)

Select Jaeger.

![](./assets/add-jaeger-data-source-3.png)

Configure the URL of the Jaeger data source as `http://query:5775/jaeger`.

![](./assets/add-jaeger-data-source-4.png)

Click Save & Test. If the following information is displayed, the configuration is successful.

![](./assets/add-jaeger-data-source-5.png)

If no data has been sent to the distributor yet, the following warning message will be displayed.

![](./assets/add-jaeger-data-source-warning.png)

#### Query Trace Data

Click the menu on the left, select Explore, and then select the Jaeger data source to see the Trace data.

![](./assets/query-trace.png)

![](./assets/query-trace-2.png)

### Metrics

#### Configure Prometheus Data Source

We have implemented an API that supports the PromQL query protocol, so you can configure the Prometheus data source directly in Grafana.

Visit http://localhost:3000/ to see the grafana login page. Both the username and password are admin.

After logging in, click the menu on the left, select Data Sources, and then click Add data source.

Select Prometheus.

![](./assets/add-prometheus-data-source.png)

Configure the URL of the Prometheus data source as `http://query:5775/prometheus`.

![](./assets/add-prometheus-data-source-2.png)

Configure the HTTP Method as POST.

![](./assets/add-prometheus-data-source-3.png)

Click Save & Test. If the following information is displayed, the configuration is successful.

![](./assets/add-prometheus-data-source-4.png)

#### Query Metrics Data

Click the menu on the left, select Explore, and then select the Prometheus data source to see the Metrics data.

![](./assets/query-metrics.png)

Click the menu on the left, select Dashboards, and then create a new dashboard.

![](./assets/create-metrics-dashboard.png)

![](./assets/create-metrics-dashboard-2.png)

Select the Prometheus data source that we just created.

![](./assets/create-metrics-dashboard-3.png)

After that, you can add panels as needed to display Metrics data.

![](./assets/create-metrics-dashboard-4.png)


