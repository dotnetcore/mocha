// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Mvc;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.DTOs;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Exceptions;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.Controllers;

/// <summary>
/// Reference to the LTS version of the Prometheus API, implemented part of the PromQL functionality.
/// https://prometheus.io/docs/prometheus/2.45/querying/api/
/// </summary>
[ServiceFilter<PrometheusExceptionFilter>]
[Route("/prometheus/api/v1")]
public class PrometheusController() : Controller
{
    [HttpGet("metadata")]
    public async Task<QueryResponse<Dictionary<string, List<PrometheusMetricMetadata>>>> GetMetadata(
        [FromQuery] int? limit,
        [FromQuery] string? metric,
        [FromServices] IPrometheusMetricMetadataReader metadataReader)
    {
        var metadataList = await metadataReader.GetMetadataAsync(metric, limit);
        return new QueryResponse<Dictionary<string, List<PrometheusMetricMetadata>>>
        {
            Status = ResultStatus.Success,
            Data = metadataList
        };
    }

    [HttpGet]
    [Route("labels")]
    public async Task<QueryResponse<IEnumerable<string>>> GetLabels(
        [FromQuery(Name = "match[]")] string[]? match,
        [FromQuery] int? start,
        [FromQuery] int? end,
        [FromServices] IPromQLParser parser,
        [FromServices] IPrometheusMetricReader metricReader)
    {
        try
        {
            if (match == null || match.Length == 0)
            {
                var labels = await metricReader.GetLabelNamesAsync(new LabelNamesQueryParameters
                {
                    LabelMatchers = [],
                    StartTimestampUnixSec = start,
                    EndTimestampUnixSec = end
                });

                return new QueryResponse<IEnumerable<string>> { Status = ResultStatus.Success, Data = labels };
            }

            var result = new List<string>();

            foreach (var m in match)
            {
                var matchers = parser.ParseMetricSelector(m);

                var labels = await metricReader.GetLabelNamesAsync(new LabelNamesQueryParameters
                {
                    LabelMatchers = matchers,
                    StartTimestampUnixSec = start,
                    EndTimestampUnixSec = end
                });

                result.AddRange(labels);
            }

            return new QueryResponse<IEnumerable<string>> { Status = ResultStatus.Success, Data = result };
        }
        catch (Exception ex)
        {
            return InValidParameterResponse<IEnumerable<string>>(nameof(match), ex.Message);
        }
    }

    [HttpGet("label/{labelName}/values")]
    public async Task<QueryResponse<List<string>>> GetLabelValues(
        [FromRoute] string labelName,
        [FromQuery(Name = "match[]")] string[]? match,
        [FromQuery] long? start,
        [FromQuery] long? end,
        [FromQuery] int? limit,
        [FromServices] IPrometheusMetricReader metricReader)
    {
        if (Labels.IsLabelNameValid(labelName) == false)
        {
            return InValidParameterResponse<List<string>>(nameof(labelName), $"invalid label name: {labelName}");
        }

        var labelValues = await metricReader.GetLabelValuesAsync(new LabelValuesQueryParameters
        {
            LabelName = labelName,
            StartTimestampUnixSec = start,
            EndTimestampUnixSec = end,
            Limit = limit ?? 100
        });

        return new QueryResponse<List<string>> { Status = ResultStatus.Success, Data = labelValues.ToList() };
    }

    [HttpPost]
    [Route("series")]
    public Task<IActionResult> GetSeries(
        [FromQuery(Name = "match[]")] string[] match,
        [FromQuery] long start,
        [FromQuery] long end)
    {
        throw new NotImplementedException();
    }

    [HttpPost]
    [Route("query")]
    public async Task<QueryResponse<ResponseData>> Query(
        [FromForm] string query,
        [FromForm] long? time,
        [FromForm] string? timeout,
        [FromServices] IPromQLEngine engine)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return InValidParameterResponse<ResponseData>(nameof(query), "query is required");
        }

        // TODO: parse timeout
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var cancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(HttpContext.RequestAborted, timeoutCts.Token).Token;

        time ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = await engine.QueryInstantAsync(query, time.Value, cancellationToken);

        return SuccessResponse(result);
    }

    [HttpGet]
    [HttpPost]
    [Route("query_range")]
    public async Task<QueryResponse<ResponseData>> QueryRange(
        [FromForm] string query,
        [FromForm] long start,
        [FromForm] long end,
        [FromForm] int? step,
        [FromForm] string? timeout,
        [FromServices] IPromQLEngine engine)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return InValidParameterResponse<ResponseData>(nameof(query), "query is required");
        }

        if (step <= 0)
        {
            return InValidParameterResponse<ResponseData>(
                nameof(step),
                "zero or negative query resolution step widths are not accepted. Try a positive integer.");
        }

        var points = (end - start) / step;
        if (points > 11000)
        {
            return InValidParameterResponse<ResponseData>(
                nameof(step),
                "exceeded maximum resolution of 11,000 points per timeseries. Try decreasing the query resolution (?step=XX)");
        }

        // TODO: parse timeout
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

        var cancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(HttpContext.RequestAborted, timeoutCts.Token).Token;
        var result = await engine.QueryRangeAsync(
            query, start, end, step > 0 ? TimeSpan.FromSeconds(step.Value) : null, cancellationToken);

        return SuccessResponse(result);
    }

    [HttpGet]
    [HttpPost]
    [Route("status/buildinfo")]
    public QueryResponse<BuildInfo> GetBuildInfo()
    {
        return new QueryResponse<BuildInfo>
        {
            Status = ResultStatus.Success,
            Data = new BuildInfo
            {
                // LTS version of the Prometheus API
                Version = "2.45.0",
                Revision = "",
                Branch = "",
                BuildUser = "",
                BuildDate = "",
                GoVersion = ""
            }
        };
    }

    private static QueryResponse<T> InValidParameterResponse<T>(string parameter, string message)
    {
        return new QueryResponse<T>
        {
            Status = ResultStatus.Error,
            ErrorType = ErrorType.BadData,
            Error = $"Invalid parameter: {parameter}, {message}"
        };
    }

    private static QueryResponse<ResponseData> SuccessResponse(IParseResult parseResult)
    {
        object data = parseResult switch
        {
            MatrixResult matrixResult => matrixResult.Select(s => new MatrixDataDTO
            {
                Metric = s.Metric,
                Values = s.Points.Select(p =>
                {
                    var values = new object[2];
                    values[0] = p.TimestampUnixSec;
                    values[1] = p.Value.ToString();
                    return values;
                }).ToList()
            }).ToList(),
            VectorResult vectorResult => vectorResult.Select(s => new VectorDataDTO
            {
                Metric = s.Metric,
                Value = [s.Point.TimestampUnixSec, s.Point.Value.ToString()]
            }).ToList(),
            ScalarResult scalarResult => new object[]
            {
                scalarResult.TimestampUnixSec, scalarResult.Value.ToString()
            },
            _ => throw new NotSupportedException($"Unsupported result type: {parseResult.GetType()}")
        };

        return new QueryResponse<ResponseData>
        {
            Status = ResultStatus.Success,
            Data = new ResponseData
            {
                ResultType = parseResult.Type,
                Result = data
            }
        };
    }
}
