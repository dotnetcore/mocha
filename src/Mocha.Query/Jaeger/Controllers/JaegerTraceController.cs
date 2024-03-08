using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Mocha.Core.Extensions;
using Mocha.Core.Storage.Jaeger;
using Mocha.Core.Storage.Jaeger.Trace;
using Mocha.Query.Jaeger.DTOs;

namespace Mocha.Query.Jaeger.Controllers;

[Route("/jaeger/api")]
public class JaegerTraceController(IJaegerSpanReader spanReader) : Controller
{
    [HttpGet("services")]
    public async Task<JaegerResponse<IEnumerable<string>>> GetSeries()
    {
        return new(await spanReader.GetServicesAsync());
    }

    [HttpGet("services/{serviceName}/operations")]
    public async Task<JaegerResponse<IEnumerable<string>>> GetOperations(string serviceName)
    {
        return new(await spanReader.GetOperationsAsync(serviceName));
    }

    [HttpGet("traces")]
    public async Task<JaegerResponse<IEnumerable<JaegerTrace>>> FindTraces([FromQuery] FindTracesRequest request)
    {
        static ulong? ParseAsNanoseconds(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            var m = Regex.Match(input,
                @"^((?<days>\d+)d)?((?<hours>\d+)h)?((?<minutes>\d+)m)?((?<seconds>\d+)s)?((?<milliseconds>\d+)ms)?((?<microseconds>\d+)μs)?$",
                RegexOptions.ExplicitCapture
                | RegexOptions.Compiled
                | RegexOptions.CultureInvariant
                | RegexOptions.RightToLeft);

            if (!m.Success)
            {
                return null;
            }

            var days = m.Groups["days"].Success ? long.Parse(m.Groups["days"].Value) : 0;
            var hours = m.Groups["hours"].Success ? long.Parse(m.Groups["hours"].Value) : 0;
            var minutes = m.Groups["minutes"].Success ? long.Parse(m.Groups["minutes"].Value) : 0;
            var seconds = m.Groups["seconds"].Success ? long.Parse(m.Groups["seconds"].Value) : 0;
            var milliseconds = m.Groups["milliseconds"].Success ? long.Parse(m.Groups["milliseconds"].Value) : 0;
            var microseconds = m.Groups["microseconds"].Success ? long.Parse(m.Groups["microseconds"].Value) : 0;

            return
                (ulong)(((days * 24 * 60 * 60 + hours * 60 * 60 + minutes * 60 + seconds) * 1000 + milliseconds)
                    * 1000 + microseconds) * 1000;
        }

        var startTimeMin = request.Start * 1000;


        var startTimeMax = request.End * 1000;

        var lookBack = ParseAsNanoseconds(request.LookBack);

        if (lookBack.HasValue)
        {
            var now = DateTimeOffset.Now.ToUnixTimeNanoseconds();
            startTimeMin = now - lookBack.Value;
            startTimeMax = now;
        }

        IEnumerable<JaegerTrace> traces;

        if (request.TraceID?.Any() ?? false)
        {
            traces = await spanReader.FindTracesAsync(request.TraceID, startTimeMin, startTimeMax);
        }
        else
        {
            traces = await spanReader.FindTracesAsync(new JaegerTraceQueryParameters
            {
                ServiceName = request.Service,
                OperationName = request.Operation,
                Tags = (request.Tags ?? "{}").FromJson<Dictionary<string, object>>()!,
                StartTimeMinUnixNano = startTimeMin,
                StartTimeMaxUnixNano = startTimeMax,
                DurationMinNanoseconds =
                    string.IsNullOrWhiteSpace(request.MinDuration)
                        ? null
                        : ParseAsNanoseconds(request.MinDuration)!,
                DurationMaxNanoseconds =
                    string.IsNullOrWhiteSpace(request.MaxDuration)
                        ? null
                        : ParseAsNanoseconds(request.MaxDuration)!,
                NumTraces = request.Limit
            });
        }

        JaegerResponseError? error = null;
        if (traces.Any() is false)
        {
            error = new JaegerResponseError { Code = (int)HttpStatusCode.NotFound, Message = "trace not found" };
        }

        return new JaegerResponse<IEnumerable<JaegerTrace>>(traces) { Error = error };
    }

    [HttpGet("traces/{traceID}")]
    public async Task<JaegerResponse<IEnumerable<JaegerTrace>>> GetTrace(string traceID)
    {
        var traces = await spanReader.FindTracesAsync([traceID]);

        JaegerResponseError? error = null;
        if (traces.Any() is false)
        {
            error = new JaegerResponseError { Code = (int)HttpStatusCode.NotFound, Message = "trace not found" };
        }

        return new JaegerResponse<IEnumerable<JaegerTrace>>(traces) { Error = error };
    }
}
