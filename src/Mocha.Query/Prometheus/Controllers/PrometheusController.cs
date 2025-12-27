// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Mocha.Core.Models.Metrics;
using Mocha.Core.Storage.Prometheus;
using Mocha.Core.Storage.Prometheus.Metrics;
using Mocha.Query.Prometheus.DTOs;
using Mocha.Query.Prometheus.PromQL.Engine;
using Mocha.Query.Prometheus.PromQL.Values;

namespace Mocha.Query.Prometheus.Controllers;

/// <summary>
/// Reference to the LTS version of the Prometheus API, implemented part of the PromQL functionality.
/// https://prometheus.io/docs/prometheus/2.45/querying/api/
/// </summary>
[ServiceFilter<PrometheusExceptionFilter>]
[Route("/prometheus/api/v1")]
public partial class PrometheusController : Controller
{
    [HttpGet("metadata")]
    public async Task<QueryResponse<Dictionary<string, List<PrometheusMetricMetadata>>>> GetMetadata(
        [FromQuery] int? limit,
        [FromQuery] string? metric,
        [FromServices] IPrometheusMetricsMetadataReader metadataReader)
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
        [FromServices] IPrometheusMetricsReader metricReader)
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
        [FromServices] IPrometheusMetricsReader metricReader)
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
        [FromQuery] int? limit,
        [FromQuery] int? lookbackDelta,
        [FromServices] IPromQLEngine engine)
    {
        // https://prometheus.io/docs/prometheus/latest/querying/api/#instant-queries
        // query=<string>: Prometheus expression query string.
        // time=<rfc3339 | unix_timestamp>: Evaluation timestamp. Optional.
        // timeout=<duration>: Evaluation timeout. Optional. Defaults to and is capped by the value of the -query.timeout flag.
        // limit=<number>: Maximum number of returned series. Doesn’t affect scalars or strings but truncates the number of series for matrices and vectors. Optional. 0 means disabled.
        // lookback_delta=<number>: Override the the lookback period just for this query. Optional.

        if (string.IsNullOrWhiteSpace(query))
        {
            return InValidParameterResponse<ResponseData>(nameof(query), "query is required");
        }

        // default timeout
        var timeoutDuration = TimeSpan.FromSeconds(30);
        if (!string.IsNullOrWhiteSpace(timeout))
        {
            if (!TryParseDuration(timeout, out timeoutDuration))
            {
                return InValidParameterResponse<ResponseData>(
                    nameof(timeout), $"cannot parse {timeout} to a valid duration");
            }
        }

        // TODO: support lookback_delta

        using var timeoutCts = new CancellationTokenSource(timeoutDuration);
        var cancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(HttpContext.RequestAborted, timeoutCts.Token).Token;

        time ??= DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var result = await engine.QueryInstantAsync(query, time.Value, limit, cancellationToken);

        return SuccessResponse(result);
    }

    [HttpGet]
    [HttpPost]
    [Route("query_range")]
    public async Task<QueryResponse<ResponseData>> QueryRange(
        [FromForm] string query,
        [FromForm] long start,
        [FromForm] long end,
        [FromForm] string step,
        [FromForm] string? timeout,
        [FromForm] int? limit,
        [FromServices] IPromQLEngine engine)
    {
        // https://prometheus.io/docs/prometheus/latest/querying/api/#range-queries
        // query=<string>: Prometheus expression query string.
        // start=<rfc3339 | unix_timestamp>: Start timestamp, inclusive.
        // end=<rfc3339 | unix_timestamp>: End timestamp, inclusive.
        // step=<duration | float>: Query resolution step width in duration format or float number of seconds.
        // timeout=<duration>: Evaluation timeout. Optional. Defaults to and is capped by the value of the -query.timeout flag.
        // limit=<number>: Maximum number of returned series. Optional. 0 means disabled.
        // lookback_delta=<number>: Override the the lookback period just for this query. Optional.

        if (string.IsNullOrWhiteSpace(query))
        {
            return InValidParameterResponse<ResponseData>(nameof(query), "query is required");
        }

        if (!TryParseDuration(step, out var stepDuration))
        {
            return InValidParameterResponse<ResponseData>(
                nameof(step), $"cannot parse {step} to a valid duration");
        }

        if (stepDuration <= TimeSpan.Zero)
        {
            return InValidParameterResponse<ResponseData>(
                nameof(step),
                "zero or negative query resolution step widths are not accepted. Try a positive integer.");
        }

        var points = (end - start) / stepDuration.TotalSeconds;
        if (points > 11000)
        {
            return InValidParameterResponse<ResponseData>(
                nameof(step),
                "exceeded maximum resolution of 11,000 points per timeseries. Try decreasing the query resolution (?step=XX)");
        }

        // default timeout
        var timeoutDuration = TimeSpan.FromSeconds(120);

        if (!string.IsNullOrWhiteSpace(timeout))
        {
            if (!TryParseDuration(timeout, out timeoutDuration))
            {
                return InValidParameterResponse<ResponseData>(
                    nameof(timeout), $"cannot parse {timeout} to a valid duration");
            }
        }

        if (limit is <= 0)
        {
            return InValidParameterResponse<ResponseData>(
                nameof(limit), "limit must be a positive integer");
        }

        // TODO: support lookback_delta

        using var timeoutCts = new CancellationTokenSource(timeoutDuration);
        var cancellationToken = CancellationTokenSource
            .CreateLinkedTokenSource(HttpContext.RequestAborted, timeoutCts.Token).Token;

        var result = await engine.QueryRangeAsync(
            query, start, end, stepDuration, limit, cancellationToken);

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
                    values[1] = p.Value.ToString(CultureInfo.InvariantCulture);
                    return values;
                }).ToList()
            }).ToList(),
            VectorResult vectorResult => vectorResult.Select(s => new VectorDataDTO
            {
                Metric = s.Metric,
                Value = [s.Point.TimestampUnixSec, s.Point.Value.ToString(CultureInfo.InvariantCulture)]
            }).ToList(),
            ScalarResult scalarResult => new object[]
            {
                scalarResult.TimestampUnixSec, scalarResult.Value.ToString(CultureInfo.InvariantCulture)
            },
            _ => throw new NotSupportedException($"Unsupported result type: {parseResult.GetType()}")
        };

        return new QueryResponse<ResponseData>
        {
            Status = ResultStatus.Success,
            Data = new ResponseData { ResultType = parseResult.Type, Result = data }
        };
    }

    private static bool TryParseDuration(string input, out TimeSpan result)
    {
        result = TimeSpan.Zero;

        if (double.TryParse(input, out var seconds))
        {
            result = TimeSpan.FromSeconds(seconds);
            return true;
        }

        var regex = DurationRegex();

        var match = regex.Match(input);
        if (!match.Success)
        {
            return false;
        }

        var value = int.Parse(match.Groups[1].Value);
        var unit = match.Groups[2].Value;
        result = unit switch
        {
            "y" => TimeSpan.FromDays(value * 365),
            "w" => TimeSpan.FromDays(value * 7),
            "d" => TimeSpan.FromDays(value),
            "h" => TimeSpan.FromHours(value),
            "m" => TimeSpan.FromMinutes(value),
            "s" => TimeSpan.FromSeconds(value),
            "ms" => TimeSpan.FromMilliseconds(value),
            _ => TimeSpan.Zero
        };
        return true;
    }

    [GeneratedRegex("^([0-9]+)(y|w|d|h|m|s|ms)$",
        RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.RightToLeft |
        RegexOptions.CultureInvariant)]
    private static partial Regex DurationRegex();
}
