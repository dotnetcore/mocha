// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Buffer;
using Mocha.Core.Models.Trace;
using Mocha.Core.Storage;

namespace Mocha.Distributor.Exporters;

public class StorageExporter(
        ISpanWriter spanWriter,
        IBufferQueue bufferQueue,
        ILogger<StorageExporter> logger)
    : IHostedService
{
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    public Task StartAsync(CancellationToken cancellationToken)
    {
        var consumerNumber = Environment.ProcessorCount;
        var consumers = bufferQueue.CreateConsumers<MochaSpan>(
            new BufferConsumerOptions
            {
                TopicName = "otlp-span",
                GroupName = "storage-exporter",
                AutoCommit = false,
                BatchSize = 100
            }, consumerNumber);

        var token = CancellationTokenSource
            .CreateLinkedTokenSource(cancellationToken, _cancellationTokenSource.Token).Token;

        foreach (var consumer in consumers)
        {
            _ = ConsumeAsync(consumer, token);
        }

        logger.LogInformation(
            "Storage exporter started, consuming from {ConsumerCount} consumers.", consumerNumber);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource.Cancel();
        logger.LogInformation("Storage exporter stopped.");
        return Task.CompletedTask;
    }

    private async Task ConsumeAsync(IBufferConsumer<MochaSpan> consumer, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await foreach (var spans in consumer.ConsumeAsync(cancellationToken))
            {
                var tryCount = 0;
                while (true)
                {
                    try
                    {
                        await spanWriter.WriteAsync(spans);
                        break;
                    }
                    catch (Exception ex)
                    {
                        tryCount++;
                        if (tryCount <= 3) // TODO: Make this configurable.
                        {
                            logger.LogWarning(ex, "Failed to write spans to storage, retrying...");
                            await Task.Delay(1000, cancellationToken);
                        }
                        else
                        {
                            logger.LogError(ex, "Failed to write spans to storage.");
                            break;
                        }
                    }
                }

                await consumer.CommitAsync();
            }
        }
    }
}
