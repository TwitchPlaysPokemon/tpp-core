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

    public sealed class GivePoints : RuleResult
    {
        public int Points { get; }
        public string Reason { get; }
        public GivePoints(int points, string reason)
        {
            Points = points;
            Reason = reason;
        }
    }

    public sealed class Timeout : RuleResult
    {
        public string Message { get; }
        public Timeout(string message) => Message = message;
    }
}
