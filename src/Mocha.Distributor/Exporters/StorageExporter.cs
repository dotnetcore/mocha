// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Buffer;
using Mocha.Core.Models.Metadata;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;

namespace Mocha.Distributor.Exporters;

public class StorageExporter(
    IBufferQueue bufferQueue,
    ITelemetryDataWriter<MochaSpanMetadata> spanMetadataWriter,
    ITelemetryDataWriter<MochaMetricMetadata> metricMetadataWriter,
    ITelemetryDataWriter<MochaSpan> spanWriter,
    ITelemetryDataWriter<MochaMetric> metricWriter,
    ILogger<StorageExporter> logger)
    : IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var groupName = "storage_exporter";

        StartConsumers<MochaSpanMetadata>(
            new BufferConsumerOptions
            {
                TopicName = "otlp-span-metadata",
                GroupName = groupName,
                AutoCommit = false,
                BatchSize = 10000
            },
            1,
            async data =>
            {
                var metadataList = data.DistinctBy(m => (m.ServiceName, m.OperationName));
                await spanMetadataWriter.WriteAsync(metadataList);
            },
            cancellationToken);

        StartConsumers<MochaMetricMetadata>(
            new BufferConsumerOptions
            {
                TopicName = "otlp-metric-metadata",
                GroupName = groupName,
                AutoCommit = false,
                BatchSize = 10000
            },
            1,
            async data =>
            {
                var metadataList = data.DistinctBy(m => (m.Metric, m.ServiceName));
                await metricMetadataWriter.WriteAsync(metadataList);
            },
            cancellationToken);

        StartConsumers<MochaSpan>(
            new BufferConsumerOptions
            {
                TopicName = "otlp-span",
                GroupName = groupName,
                AutoCommit = false,
                BatchSize = 100
            },
            Environment.ProcessorCount,
            spanWriter.WriteAsync,
            cancellationToken);

        StartConsumers<MochaMetric>(
            new BufferConsumerOptions
            {
                TopicName = "otlp-metric",
                GroupName = groupName,
                AutoCommit = false,
                BatchSize = 100
            },
            Environment.ProcessorCount,
            metricWriter.WriteAsync,
            cancellationToken);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        logger.LogInformation("Storage exporter stopped.");
        return Task.CompletedTask;
    }

    private void StartConsumers<T>(
        BufferConsumerOptions options,
        int consumerNumber,
        Func<IEnumerable<T>, Task> callback,
        CancellationToken cancellationToken)
    {
        var consumers = bufferQueue.CreateConsumers<T>(options, consumerNumber);

        var token = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;

        foreach (var consumer in consumers)
        {
            _ = ConsumeAsync(consumer, callback, token);
        }

        logger.LogInformation(
            "{Type} Storage exporter started, consuming from {ConsumerCount} consumers.",
            typeof(T).Name,
            consumerNumber);
    }

    private async Task ConsumeAsync<T>(
        IBufferConsumer<T> consumer,
        Func<IEnumerable<T>, Task> callback,
        CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var data in consumer.ConsumeAsync(cancellationToken))
            {
                var tryCount = 0;
                while (true)
                {
                    try
                    {
                        await callback(data);
                        break;
                    }
                    catch (Exception ex)
                    {
                        tryCount++;
                        if (tryCount <= 3) // TODO: Make this configurable.
                        {
                            logger.LogWarning(ex, "Failed to write {Type} to storage, retrying.", typeof(T).Name);
                            await Task.Delay(1000, cancellationToken);
                        }
                        else
                        {
                            logger.LogError(ex, "Failed to write {Type} to storage after {TryCount} retries.",
                                typeof(T).Name, tryCount);
                            break;
                        }
                    }
                }

                await consumer.CommitAsync();
            }
        }
    }
}
