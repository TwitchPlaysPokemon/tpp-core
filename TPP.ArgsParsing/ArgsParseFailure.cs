using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace TPP.ArgsParsing;

/// <summary>
/// Exception thrown by <see cref="ArgsParser"/> if results are being queried without checking for parse failure.
/// Useful if usages of <see cref="ArgsParser"/> have no reason to handle parsing errors in any specific way and
/// just want to propagate the error.
/// The <see cref="Failures"/> property contains all failures,
/// and the exception message gets constructed from the most relevant ones.
/// </summary>
public class ArgsParseFailure : ArgumentException
{
    public IImmutableList<Failure> Failures { get; }

    private static string FailuresToFailureString(IImmutableList<Failure> failures)
    {
        ErrorRelevanceConfidence maxConfidence = failures.Max(failure => failure.Relevance);
        IEnumerable<string> relevantFailureTexts = failures
            .Where(f => f.Relevance == maxConfidence)
            .Select(f => f.Error)
            .Distinct();
        return string.Join(", or ", relevantFailureTexts);
    }

    public ArgsParseFailure(IImmutableList<Failure> failures) : base(FailuresToFailureString(failures))
    {
        Failures = failures;
    }
}
