using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Core.Moderation.Rules;
using Microsoft.Extensions.Logging;
using NodaTime;

namespace TPP.Core.Moderation
{
    public interface IModerator
    {
        /// Checks a message and may perform some punitive actions,
        /// returning whether the message was okay.
        Task<bool> Check(Message message);
    }

    public class Moderator : IModerator
    {
        private readonly ILogger<Moderator> _logger;
        private readonly IExecutor _executor;
        private readonly IImmutableList<IModerationRule> _rules;

        public Moderator(ILogger<Moderator> logger, IExecutor executor)
        {
            _logger = logger;
            _executor = executor;
            _rules = ImmutableList.Create<IModerationRule>(
                new ActionRule(),
                new ActionBaitRule()
            );
        }

        private RuleResult ApplyPoints(int points)
        {
            return new RuleResult.Nothing();
        }

        public async Task<bool> Check(Message message)
        {
            List<RuleResult> results = _rules.Select(rule => rule.Check(message)).ToList();

            int points = results.OfType<RuleResult.GivePoints>().Sum(p => p.Points);
            RuleResult additionalAction = ApplyPoints(points);
            results.Add(additionalAction);

            List<RuleResult.Timeout> timeouts = results.OfType<RuleResult.Timeout>().ToList();
            List<RuleResult.DeleteMessage> deletions = results.OfType<RuleResult.DeleteMessage>().ToList();

            if (timeouts.Any())
            {
                Duration timeoutDuration = Duration.FromSeconds(1); // TODO calculate default timeout
                await _executor.Timeout(message.User, timeouts.First().Message, timeoutDuration);
                return false;
            }
            if (deletions.Any())
            {
                if (message.Details.MessageId != null)
                    await _executor.DeleteMessage(message.Details.MessageId);
                else
                    // Regular messages should always have an id. Whispers don't, but shouldn't be checked by modbot.
                    _logger.LogWarning($"Modbot cannot delete message because it's missing a message id: {message}");
                return false;
            }
            return true;
        }
    }
}
