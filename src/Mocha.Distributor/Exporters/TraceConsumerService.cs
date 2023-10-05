using Grpc.Core;
using Mocha.Distributor.Consumer;
using OpenTelemetry.Proto.Trace.V1;

namespace Mocha.Distributor.Exporters;

public class TraceConsumerService : TraceConsumer.TraceConsumerBase
{
    public override async Task Consume(ConsumeRequest request, IServerStreamWriter<ResourceSpans> responseStream, ServerCallContext context)
    {
        await Task.CompletedTask;
    }
}