// Licensed to the.NET Core Community under one or more agreements.
// The.NET Core Community licenses this file to you under the MIT license.

using System.Collections;

namespace Mocha.Core.Storage.Query;

public class TraceReadQuery
{
    public string? ServiceName { get; set; }

    public ICollection<KeyValuePair<string, string>> SpanAttributes { get; set; } = new HashSet<KeyValuePair<string, string>>();

    public long? StartTimeStamp { get; set; }

    public long? EndTimeStamp { get; set; }

    public string? SpanName { get; set; }
}
