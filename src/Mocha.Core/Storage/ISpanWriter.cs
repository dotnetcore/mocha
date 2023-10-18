// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Mocha.Core.Model.Trace;

namespace Mocha.Core.Storage;
public interface ISpanWriter
{
    Task<bool> WriteAsync(IEnumerable<Span> span);
}
