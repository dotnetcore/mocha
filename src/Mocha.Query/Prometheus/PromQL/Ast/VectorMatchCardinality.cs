// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

/// <summary>
/// VectorMatchCardinality describes the cardinality relationship
/// of two Vectors in a binary operation.
/// </summary>
public enum VectorMatchCardinality
{
    OneToOne,
    ManyToOne,
    OneToMany,
    ManyToMany,
}
