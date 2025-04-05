namespace TPP.Core.Moderation;

public interface IModerationRule
{
    public string Id { get; }
    public RuleResult Check(Message message);
}

public abstract class RuleResult
{
    private RuleResult()
    {
    }

    public sealed class Nothing : RuleResult
    {
    }

    public sealed class DeleteMessage : RuleResult
    {
    }

    public sealed class GivePoints(int points, string reason) : RuleResult
    {
        public int Points { get; } = points;
        public string Reason { get; } = reason;
    }

    public sealed class Timeout(string message) : RuleResult
    {
        public string Message { get; } = message;
    }
}
