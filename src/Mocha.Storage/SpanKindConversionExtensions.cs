// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Enums;
using OTelSpanKind = OpenTelemetry.Proto.Trace.V1.Span.Types.SpanKind;

namespace Mocha.Storage;

public static class SpanKindConversionExtensions
{
    internal static SpanKind ToMochaSpanKind(this OTelSpanKind spanKind)
    {
        return spanKind switch
        {
            OTelSpanKind.Unspecified => SpanKind.Unspecified,
            OTelSpanKind.Internal => SpanKind.Internal,
            OTelSpanKind.Server => SpanKind.Server,
            OTelSpanKind.Client => SpanKind.Client,
            OTelSpanKind.Producer => SpanKind.Producer,
            OTelSpanKind.Consumer => SpanKind.Consumer,
            _ => throw new ArgumentOutOfRangeException(nameof(spanKind), spanKind, null)
        };
    }
}
