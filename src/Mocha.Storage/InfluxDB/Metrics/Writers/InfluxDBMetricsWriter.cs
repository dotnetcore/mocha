// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using InfluxDB.Client;
using InfluxDB.Client.Api.Domain;
using InfluxDB.Client.Writes;
using Microsoft.Extensions.Options;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage;

namespace Mocha.Storage.InfluxDB.Metrics.Writers;

public class InfluxDBMetricsWriter(
    IInfluxDBClient client,
    IOptions<InfluxDBOptions> options)
    : ITelemetryDataWriter<MochaMetric>
{
    private readonly IWriteApiAsync _writer = client.GetWriteApiAsync();

    public async Task WriteAsync(IEnumerable<MochaMetric> data)
    {
        var points = new List<PointData>();
        foreach (var metric in data)
        {
            var pointBuilder = PointData.Builder.Measurement(metric.Name);
            foreach (var (label, labelValue) in metric.Labels)
            {
                pointBuilder = pointBuilder.Tag(label, labelValue);
            }

            pointBuilder = pointBuilder.Field("value", metric.Value)
                .Timestamp((long)metric.TimestampUnixNano, WritePrecision.Ns);
            points.Add(pointBuilder.ToPointData());
        }

        await _writer.WritePointsAsync(points, options.Value.Bucket, options.Value.Org);
    }
}
