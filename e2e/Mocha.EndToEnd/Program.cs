// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.E2E.Abstractions;
using Mocha.EndToEnd;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

// End-to-end driver for the Mocha OTLP ingest -> storage -> query chain.
//
// It discovers YAML case files under e2e/cases/, and for each one builds a small OTLP payload,
// sends it to the distributor over OTLP/gRPC, then polls the Query API until the declared Jaeger
// and Prometheus expectations are satisfied. All cases run; the process exits non-zero if any fail.
//
// Configuration (all optional, sensible localhost defaults):
//   MOCHA_E2E_OTLP_ENDPOINT   default http://localhost:4317
//   MOCHA_E2E_QUERY_BASEURL   default http://localhost:5775
//   MOCHA_E2E_TIMEOUT_SECONDS default 30 (per-assertion; overrides a case's timeoutSeconds ceiling)
//   MOCHA_E2E_CASES_DIR       default <app>/cases (also accepted as the first CLI argument)
//
// Exit code 0 => every case passed. Non-zero => at least one case failed (details on stderr).

var otlpEndpoint = Environment.GetEnvironmentVariable("MOCHA_E2E_OTLP_ENDPOINT") ?? "http://localhost:4317";
var queryBaseUrl = Environment.GetEnvironmentVariable("MOCHA_E2E_QUERY_BASEURL") ?? "http://localhost:5775";
var timeoutOverrideSeconds = ParseTimeoutSeconds(Environment.GetEnvironmentVariable("MOCHA_E2E_TIMEOUT_SECONDS"));

var casesDir = ResolveCasesDirectory(args);

Console.WriteLine("Mocha end-to-end test (YAML-driven)");
Console.WriteLine($"  OTLP endpoint : {otlpEndpoint}");
Console.WriteLine($"  Query base url: {queryBaseUrl}");
Console.WriteLine($"  Cases dir     : {casesDir}");
if (timeoutOverrideSeconds is not null)
{
    Console.WriteLine($"  Timeout (env) : {timeoutOverrideSeconds}s (overrides per-case timeoutSeconds)");
}

Console.WriteLine();

if (!Directory.Exists(casesDir))
{
    Console.Error.WriteLine($"Cases directory not found: {casesDir}");
    return 2;
}

var caseFiles = Directory.GetFiles(casesDir, "*.yaml").OrderBy(p => p, StringComparer.Ordinal).ToList();
if (caseFiles.Count == 0)
{
    Console.Error.WriteLine($"No *.yaml case files found in {casesDir}");
    return 2;
}

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(CamelCaseNamingConvention.Instance)
    .IgnoreUnmatchedProperties()
    .Build();

using var queryClient = new QueryClient(queryBaseUrl);

// Fail fast if the Query API never comes up at all, using the widest per-case timeout as a ceiling.
var readinessTimeout = TimeSpan.FromSeconds(
    Math.Max(timeoutOverrideSeconds ?? 0, 30));
using (var readinessCts = new CancellationTokenSource(readinessTimeout + TimeSpan.FromSeconds(30)))
{
    Console.WriteLine("Waiting for Query API to become ready...");
    try
    {
        await queryClient.WaitUntilReadyAsync(readinessTimeout, readinessCts.Token);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Query API did not become ready: {ex.Message}");
        return 1;
    }
}

var results = new List<(string Name, bool Passed, string? Error)>();

foreach (var caseFile in caseFiles)
{
    var fileName = Path.GetFileName(caseFile);
    CaseSpec caseSpec;
    try
    {
        caseSpec = deserializer.Deserialize<CaseSpec>(await File.ReadAllTextAsync(caseFile));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"[{fileName}] failed to parse: {ex.Message}");
        results.Add((fileName, false, $"parse error: {ex.Message}"));
        continue;
    }

    var caseName = string.IsNullOrWhiteSpace(caseSpec.Name) ? fileName : caseSpec.Name;
    var timeoutSeconds = timeoutOverrideSeconds ?? (caseSpec.TimeoutSeconds > 0 ? caseSpec.TimeoutSeconds : 30);
    var timeout = TimeSpan.FromSeconds(timeoutSeconds);

    var factory = new TelemetryFactory(caseSpec.ToTelemetrySpec());

    Console.WriteLine();
    Console.WriteLine($"=== Case: {caseName} ===");
    if (!string.IsNullOrWhiteSpace(caseSpec.Description))
    {
        Console.WriteLine($"  {caseSpec.Description}");
    }

    Console.WriteLine($"  Run id       : {factory.RunId}");
    Console.WriteLine($"  Service name : {factory.ServiceName}");
    Console.WriteLine($"  Operations   : {string.Join(", ", factory.OperationNames)}");
    Console.WriteLine($"  Metrics      : {string.Join(", ", factory.MetricNames)}");
    Console.WriteLine($"  Timeout      : {timeoutSeconds}s");

    using var caseCts = new CancellationTokenSource(timeout + TimeSpan.FromSeconds(30));

    try
    {
        Console.WriteLine("  Sending OTLP trace and metric...");
        using (var sender = new OtlpSender(otlpEndpoint))
        {
            if (factory.OperationNames.Count > 0)
            {
                await sender.SendTraceAsync(factory.BuildTraceRequest(), caseCts.Token);
            }

            if (factory.MetricNames.Count > 0)
            {
                await sender.SendMetricsAsync(factory.BuildMetricsRequest(), caseCts.Token);
            }
        }

        Console.WriteLine("  Asserting telemetry is readable through the Query API (poll-until-visible)...");
        await AssertCaseAsync(queryClient, caseSpec, factory, timeout, caseCts.Token);

        Console.WriteLine($"  [pass] {caseName}");
        results.Add((caseName, true, null));
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"  [fail] {caseName}: {ex.Message}");
        results.Add((caseName, false, ex.Message));
    }
}

Console.WriteLine();
Console.WriteLine("==============================================");
var passed = results.Count(r => r.Passed);
foreach (var (name, ok, error) in results)
{
    Console.WriteLine(ok ? $"  PASS  {name}" : $"  FAIL  {name} -> {error}");
}

Console.WriteLine($" {passed}/{results.Count} case(s) passed");
Console.WriteLine("==============================================");

return results.All(r => r.Passed) ? 0 : 1;

static async Task AssertCaseAsync(
    QueryClient client,
    CaseSpec caseSpec,
    TelemetryFactory factory,
    TimeSpan timeout,
    CancellationToken cancellationToken)
{
    var jaeger = caseSpec.Expect.Jaeger;
    if (jaeger is not null)
    {
        string? traceId = null;
        if (!string.IsNullOrEmpty(jaeger.TraceById))
        {
            traceId = factory.TraceIdFor(jaeger.TraceById);
        }

        await Matchers.AssertJaegerAsync(
            client,
            factory.ServiceName,
            jaeger.Service,
            jaeger.Operations,
            traceId,
            timeout,
            cancellationToken);
    }

    // The runner suffixes metric names with the run id; resolve each declared base name in the
    // Prometheus query to the actual stored name before handing it to the matcher.
    var metricNameMap = BuildMetricNameMap(caseSpec, factory);
    foreach (var prometheus in caseSpec.Expect.Prometheus)
    {
        var resolvedQuery = ResolveQuery(prometheus.Query, metricNameMap, factory.RunId);
        await Matchers.AssertPrometheusAsync(client, resolvedQuery, prometheus.Expect, timeout, cancellationToken);
    }
}

static Dictionary<string, string> BuildMetricNameMap(CaseSpec caseSpec, TelemetryFactory factory)
{
    // spec.Metrics and factory.MetricNames are in the same declaration order.
    var map = new Dictionary<string, string>(StringComparer.Ordinal);
    for (var i = 0; i < caseSpec.Send.Metrics.Count && i < factory.MetricNames.Count; i++)
    {
        map[caseSpec.Send.Metrics[i].Name] = factory.MetricNames[i];
    }

    return map;
}

static string ResolveQuery(string query, IReadOnlyDictionary<string, string> metricNameMap, string runId)
{
    var resolved = query.Replace("${runId}", runId, StringComparison.Ordinal);
    foreach (var (baseName, resolvedName) in metricNameMap)
    {
        resolved = resolved.Replace(baseName, resolvedName, StringComparison.Ordinal);
    }

    return resolved;
}

static string ResolveCasesDirectory(string[] args)
{
    var fromArgOrEnv = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0])
        ? args[0]
        : Environment.GetEnvironmentVariable("MOCHA_E2E_CASES_DIR");

    if (!string.IsNullOrWhiteSpace(fromArgOrEnv))
    {
        return Path.GetFullPath(fromArgOrEnv);
    }

    return Path.Combine(AppContext.BaseDirectory, "cases");
}

static int? ParseTimeoutSeconds(string? raw) =>
    int.TryParse(raw, out var value) && value > 0 ? value : null;
