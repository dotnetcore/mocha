// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Storage.LiteDB;

public static class LiteDBConstants
{
    public const string SpansMetadataDatabaseFileName = "spans_metadata.db";

    public static string SpansMetadataCollectionName => "spans_metadata";

    public const string SpansDatabaseFileName = "spans.db";

    public const string SpansCollectionName = "spans";

    public const string MetricsMetadataDatabaseFileName = "metrics_metadata.db";

    public const string MetricsMetadataCollectionName = "metrics_metadata";

    public const string MetricsDatabaseFileName = "metrics.db";

    public const string MetricsCollectionName = "metrics";
}
