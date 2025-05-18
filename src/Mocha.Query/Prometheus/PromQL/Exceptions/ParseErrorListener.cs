// Licensed to the .NET Core Community under one or more agreements.
// The .NET Core Community licenses this file to you under the MIT license.

using Antlr4.Runtime;
using Antlr4.Runtime.Misc;

namespace Mocha.Query.Prometheus.PromQL.Exceptions;

internal class ParseErrorListener : BaseErrorListener
{
    public override void SyntaxError(
        TextWriter output,
        IRecognizer recognizer,
        IToken offendingSymbol,
        int line,
        int charPositionInLine,
        string msg,
        RecognitionException e)
    {
        throw new ParseCanceledException($"line {line}:{charPositionInLine} {msg}");
    }
}
