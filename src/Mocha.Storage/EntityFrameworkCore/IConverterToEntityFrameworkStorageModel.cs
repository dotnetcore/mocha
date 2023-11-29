// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Storage.EntityFrameworkCore.Trace;

namespace Mocha.Storage.EntityFrameworkCore;

public interface IConverterToEntityFrameworkStorageModel
{
    Span ConverterToSpan(OpenTelemetry.Proto.Trace.V1.Span span);
}
