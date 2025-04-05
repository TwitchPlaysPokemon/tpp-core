namespace TPP.Core.Moderation;

public interface IModerationRule
{
    public string Id { get; }
    public RuleResult Check(Message message);
}

public abstract record RuleResult
{
    private RuleResult()
    {
    }

    public sealed record Nothing : RuleResult;
    public sealed record DeleteMessage : RuleResult;
    public sealed record GivePoints(int Points, string Reason) : RuleResult;
    public sealed record Timeout(string Message) : RuleResult;
}
