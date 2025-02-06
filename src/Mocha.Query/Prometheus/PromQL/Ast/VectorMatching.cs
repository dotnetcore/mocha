// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

namespace Mocha.Query.Prometheus.PromQL.Ast;

/// <summary>
/// VectorMatching describes how elements from two Vectors in a binary
/// operation are supposed to be matched.
/// </summary>
public class VectorMatching
{
    // The cardinality of the two Vectors.
    public VectorMatchCardinality Cardinality { get; init; }

    // MatchingLabels contains the labels which define equality of a pair of
    // elements from the Vectors.
    public required HashSet<string> MatchingLabels { get; init; }

    // On includes the given label names from matching,
    // rather than excluding them.
    public bool On { get; init; }

    // Include contains additional labels that should be included in
    // the result from the side with the lower cardinality.
    public required HashSet<string>? Include { get; init; }
}
