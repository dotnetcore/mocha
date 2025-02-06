// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Exceptions;

public class TooManySamplesException()
    : Exception("Query processing would load too many samples into memory");
