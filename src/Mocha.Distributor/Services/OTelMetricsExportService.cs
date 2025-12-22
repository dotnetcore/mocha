// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Grpc.Core;
using Mocha.Core.Buffer;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Models.Metrics;
using OpenTelemetry.Proto.Collector.Metrics.V1;

namespace Mocha.Distributor.Services;

public class OTelMetricsExportService(IBufferQueue bufferQueue) : MetricsService.MetricsServiceBase
{
    private readonly IBufferProducer<MochaMetric> _bufferProducer =
        bufferQueue.CreateProducer<MochaMetric>("otlp-metric");

    private readonly IBufferProducer<MochaMetricMetadata> _metadataBufferProducer =
        bufferQueue.CreateProducer<MochaMetricMetadata>("otlp-metric-metadata");

    public override async Task<ExportMetricsServiceResponse> Export(
        ExportMetricsServiceRequest request,
        ServerCallContext context)
    {
        var metrics = new List<MochaMetric>();
        var metricsMetadata = new List<MochaMetricMetadata>();
        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            var resource = resourceMetrics.Resource;
            var resourceLabels = resource.Attributes.ToMochaMetricLabels();
            foreach (var scopeMetrics in resourceMetrics.ScopeMetrics)
            {
                foreach (var metric in scopeMetrics.Metrics)
                {
                    metrics.AddRange(metric.ToMochaMetric(resourceLabels));
                    metricsMetadata.AddRange(metric.ToMochaMetricMetadata(resourceLabels));
                }
            }
        }

        foreach (var metric in metrics)
        {
            var valueTask = _bufferProducer.ProduceAsync(metric);
            if (!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.AsTask();
            }
        }

        foreach (var metadata in metricsMetadata)
        {
            var valueTask = _metadataBufferProducer.ProduceAsync(metadata);
            if (!valueTask.IsCompletedSuccessfully)
            {
                await valueTask.AsTask();
            }
        }

        var totalMetricCount = metrics.Count;
        var acceptedMetricCount = 0;
        try
        {
            for (; acceptedMetricCount < totalMetricCount; acceptedMetricCount++)
            {
                await _bufferProducer.ProduceAsync(metrics[acceptedMetricCount]);
            }
        }
        catch (Exception ex)
        {
            return new ExportMetricsServiceResponse
            {
                PartialSuccess = new ExportMetricsPartialSuccess
                {
                    // TODO: The rejected data points should be calculated based on the number of data points in the metrics.
                    RejectedDataPoints = totalMetricCount - acceptedMetricCount,
                    ErrorMessage = ex.Message
                }
            };
        }

        return new ExportMetricsServiceResponse();
    }
}
