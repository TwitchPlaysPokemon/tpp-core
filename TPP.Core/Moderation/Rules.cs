using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using F23.StringSimilarity;
using NodaTime;
using TPP.Core.Utils;
using static System.Globalization.UnicodeCategory;

namespace TPP.Core.Moderation
{
    /// Some hardcoded URLs that are immediately timeout-worthy. These are mostly screamer-sites.
    public class BannedUrlsRule : IModerationRule
    {
        public string Id => "banned_url";
        private static readonly string[] BannedUrls =
        {
            "strawpoii.me", "strawpoil.me", "findminecraft.com", "akk.li/pics/anne.jpg", "shafou.com", "herobrine.com"
        };

        public RuleResult Check(Message message) =>
            BannedUrls.Any(url => message.MessageText.Contains(url, StringComparison.InvariantCultureIgnoreCase))
                ? new RuleResult.Timeout("message contained a banned URL")
                : new RuleResult.Nothing();
    }

    /// Fairly new users are not allowed to post links in chat.
    /// This is mostly effective against spambots.
    public class NewUserLinkRule : IModerationRule
    {
        public string Id => "new-user-link";
        private static readonly Regex UrllikeRegex =
            new(@"(?:^|\s)((?:\w+://)?(?:[a-zA-Z-]+\.)+[a-zA-Z-]{2,}(?:/[\w._-]+)*)/?(?:\s|$)");

        private static readonly Duration MinAgeForLinkPosting = Duration.FromHours(48);

        private readonly IClock _clock;

        public NewUserLinkRule(IClock clock) => _clock = clock;

        public RuleResult Check(Message message)
        {
            Duration knownFor = _clock.GetCurrentInstant() - message.User.FirstActiveAt;
            if (knownFor < MinAgeForLinkPosting && UrllikeRegex.IsMatch(message.MessageText))
                return new RuleResult.Timeout("account too new to TPP for posting links");
            return new RuleResult.Nothing();
        }
    }

    /// Excessive amounts of picture-like symbols accumulate points.
    public class EmoteRule : IModerationRule
    {
        public string Id => "emote";

        private readonly int _freeEmotes;
        private readonly double _powerOfEmotes;

        public EmoteRule(int freeEmotes = 2, double powerOfEmotes = 3)
        {
            _freeEmotes = freeEmotes;
            _powerOfEmotes = powerOfEmotes;
        }

        /// <summary>
        /// This regex was generated using <a href="https://github.com/Felk/UnicodeEmojiRegex">UnicodeEmojiRegex</a>.
        /// It matches all emojis listed in the official 'Emoji Keyboard/Display Test Data' that are fully-qualified.
        /// </summary>
        private static readonly Regex EmojiRegex = new(
            @"\uD83D(?:\uDC68(?:\uD83C(?:[\uDFFB-\uDFFF]\u200D(?:\u2764\uFE0F\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68\uD83C[\uDFFB-\uDFFF]|\uD83E(?:\uDD1D\u200D\uD83D\uDC68\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])|[\u2695\u2696\u2708]\uFE0F|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|[\uDFFB-\uDFFF])|\u200D(?:\u2764\uFE0F\u200D\uD83D(?:\uDC8B\u200D\uD83D)?\uDC68|\uD83D(?:(?:[\uDC68\uDC69]\u200D\uD83D)?(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|[\u2695\u2696\u2708]\uFE0F|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83E[\uDDAF-\uDDB3\uDDBC\uDDBD]))?|\uDC69(?:\uD83C(?:[\uDFFB-\uDFFF]\u200D(?:\u2764\uFE0F\u200D\uD83D(?:\uDC8B\u200D\uD83D[\uDC68\uDC69]|[\uDC68\uDC69])\uD83C[\uDFFB-\uDFFF]|\uD83E(?:\uDD1D\u200D\uD83D[\uDC68\uDC69]\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])|[\u2695\u2696\u2708]\uFE0F|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|[\uDFFB-\uDFFF])|\u200D(?:\u2764\uFE0F\u200D\uD83D(?:\uDC8B\u200D\uD83D[\uDC68\uDC69]|[\uDC68\uDC69])|\uD83D(?:(?:\uDC69\u200D\uD83D)?(?:\uDC66(?:\u200D\uD83D\uDC66)?|\uDC67(?:\u200D\uD83D[\uDC66\uDC67])?)|[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|[\u2695\u2696\u2708]\uFE0F|\uD83C[\uDF3E\uDF73\uDF7C\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83E[\uDDAF-\uDDB3\uDDBC\uDDBD]))?|(?:(?:\uDD75(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)|\uDC6F)\u200D[\u2640\u2642]|\uDC41(?:\uFE0F\u200D\uD83D\uDDE8)?|[\uDC3F\uDCFD\uDD49\uDD4A\uDD6F\uDD70\uDD73\uDD74\uDD76-\uDD79\uDD87\uDD8A-\uDD8D\uDD90\uDDA5\uDDA8\uDDB1\uDDB2\uDDBC\uDDC2-\uDDC4\uDDD1-\uDDD3\uDDDC-\uDDDE\uDDE1\uDDE3\uDDE8\uDDEF\uDDF3\uDDFA\uDECB\uDECD-\uDECF\uDEE0-\uDEE5\uDEE9\uDEF0\uDEF3])\uFE0F|[\uDC6E\uDC70\uDC71\uDC73\uDC77\uDC81\uDC82\uDC86\uDC87\uDE45-\uDE47\uDE4B\uDE4D\uDE4E\uDEA3\uDEB4-\uDEB6](?:(?:\uD83C[\uDFFB-\uDFFF])?\u200D[\u2640\u2642]\uFE0F|\uD83C[\uDFFB-\uDFFF])?|\uDE36(?:\u200D\uD83C\uDF2B\uFE0F)?|\uDC15(?:\u200D\uD83E\uDDBA)?|\uDC3B(?:\u200D\u2744\uFE0F)?|\uDE2E(?:\u200D\uD83D\uDCA8)?|\uDE35(?:\u200D\uD83D\uDCAB)?|[\uDC42\uDC43\uDC46-\uDC50\uDC66\uDC67\uDC6B-\uDC6D\uDC72\uDC74-\uDC76\uDC78\uDC7C\uDC83\uDC85\uDC8F\uDC91\uDCAA\uDD7A\uDD95\uDD96\uDE4C\uDE4F\uDEC0\uDECC](?:\uD83C[\uDFFB-\uDFFF])?|[\uDD74\uDD90]\uD83C[\uDFFB-\uDFFF]|\uDC08(?:\u200D\u2B1B)?|\uDD75(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)|[\uDC00-\uDC07\uDC09-\uDC14\uDC16-\uDC3A\uDC3C-\uDC3E\uDC40\uDC44\uDC45\uDC51-\uDC65\uDC6A\uDC6F\uDC79-\uDC7B\uDC7D-\uDC80\uDC84\uDC88-\uDC8E\uDC90\uDC92-\uDCA9\uDCAB-\uDCFC\uDCFF-\uDD3D\uDD4B-\uDD4E\uDD50-\uDD67\uDDA4\uDDFB-\uDE2D\uDE2F-\uDE34\uDE37-\uDE44\uDE48-\uDE4A\uDE80-\uDEA2\uDEA4-\uDEB3\uDEB7-\uDEBF\uDEC1-\uDEC5\uDED0-\uDED2\uDED5-\uDED7\uDEEB\uDEEC\uDEF4-\uDEFC\uDFE0-\uDFEB])|\uD83E(?:\uDDD1(?:\uD83C(?:[\uDFFB-\uDFFF]\u200D(?:\u2764\uFE0F\u200D(?:\uD83D\uDC8B\u200D)?\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|\uD83E(?:\uDD1D\u200D\uD83E\uDDD1\uD83C[\uDFFB-\uDFFF]|[\uDDAF-\uDDB3\uDDBC\uDDBD])|[\u2695\u2696\u2708]\uFE0F|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92])|[\uDFFB-\uDFFF])|\u200D(?:\uD83E(?:\uDD1D\u200D\uD83E\uDDD1|[\uDDAF-\uDDB3\uDDBC\uDDBD])|[\u2695\u2696\u2708]\uFE0F|\uD83C[\uDF3E\uDF73\uDF7C\uDF84\uDF93\uDFA4\uDFA8\uDFEB\uDFED]|\uD83D[\uDCBB\uDCBC\uDD27\uDD2C\uDE80\uDE92]))?|[\uDD26\uDD35\uDD37-\uDD39\uDD3D\uDD3E\uDDB8\uDDB9\uDDCD-\uDDCF\uDDD4\uDDD6-\uDDDD](?:(?:\uD83C[\uDFFB-\uDFFF])?\u200D[\u2640\u2642]\uFE0F|\uD83C[\uDFFB-\uDFFF])?|[\uDD3C\uDDDE\uDDDF]\u200D[\u2640\u2642]\uFE0F|[\uDD0C\uDD0F\uDD18-\uDD1C\uDD1E\uDD1F\uDD30-\uDD34\uDD36\uDD77\uDDB5\uDDB6\uDDBB\uDDD2\uDDD3\uDDD5](?:\uD83C[\uDFFB-\uDFFF])?|[\uDD0D\uDD0E\uDD10-\uDD17\uDD1D\uDD20-\uDD25\uDD27-\uDD2F\uDD3A\uDD3C\uDD3F-\uDD45\uDD47-\uDD76\uDD78\uDD7A-\uDDAF\uDDB4\uDDB7\uDDBA\uDDBC-\uDDCB\uDDD0\uDDDE-\uDDFF\uDE70-\uDE74\uDE78-\uDE7A\uDE80-\uDE86\uDE90-\uDEA8\uDEB0-\uDEB6\uDEC0-\uDEC2\uDED0-\uDED6])|\uD83C(?:\uDFF4(?:\uDB40\uDC67\uDB40\uDC62\uDB40(?:\uDC65\uDB40\uDC6E\uDB40\uDC67|\uDC73\uDB40\uDC63\uDB40\uDC74|\uDC77\uDB40\uDC6C\uDB40\uDC73)\uDB40\uDC7F|\u200D\u2620\uFE0F)?|[\uDFC3\uDFC4\uDFCA](?:(?:\uD83C[\uDFFB-\uDFFF])?\u200D[\u2640\u2642]\uFE0F|\uD83C[\uDFFB-\uDFFF])?|[\uDFCB\uDFCC](?:(?:(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)\u200D[\u2640\u2642])?\uFE0F|\uD83C[\uDFFB-\uDFFF])|(?:\uDFF3(?:\uFE0F\u200D\u26A7)?|[\uDD70\uDD71\uDD7E\uDD7F\uDE02\uDE37\uDF21\uDF24-\uDF2C\uDF36\uDF7D\uDF96\uDF97\uDF99-\uDF9B\uDF9E\uDF9F\uDFCD\uDFCE\uDFD4-\uDFDF\uDFF5\uDFF7])\uFE0F|(?:\uDFF3\uFE0F\u200D\uD83C)?\uDF08|[\uDF85\uDFC2\uDFC7](?:\uD83C[\uDFFB-\uDFFF])?|\uDDE6\uD83C[\uDDE8-\uDDEC\uDDEE\uDDF1\uDDF2\uDDF4\uDDF6-\uDDFA\uDDFC\uDDFD\uDDFF]|\uDDE7\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEF\uDDF1-\uDDF4\uDDF6-\uDDF9\uDDFB\uDDFC\uDDFE\uDDFF]|\uDDE8\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDEE\uDDF0-\uDDF5\uDDF7\uDDFA-\uDDFF]|\uDDE9\uD83C[\uDDEA\uDDEC\uDDEF\uDDF0\uDDF2\uDDF4\uDDFF]|\uDDEA\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDED\uDDF7-\uDDFA]|\uDDEB\uD83C[\uDDEE-\uDDF0\uDDF2\uDDF4\uDDF7]|\uDDEC\uD83C[\uDDE6\uDDE7\uDDE9-\uDDEE\uDDF1-\uDDF3\uDDF5-\uDDFA\uDDFC\uDDFE]|\uDDED\uD83C[\uDDF0\uDDF2\uDDF3\uDDF7\uDDF9\uDDFA]|\uDDEE\uD83C[\uDDE8-\uDDEA\uDDF1-\uDDF4\uDDF6-\uDDF9]|\uDDEF\uD83C[\uDDEA\uDDF2\uDDF4\uDDF5]|\uDDF0\uD83C[\uDDEA\uDDEC-\uDDEE\uDDF2\uDDF3\uDDF5\uDDF7\uDDFC\uDDFE\uDDFF]|\uDDF1\uD83C[\uDDE6-\uDDE8\uDDEE\uDDF0\uDDF7-\uDDFB\uDDFE]|\uDDF2\uD83C[\uDDE6\uDDE8-\uDDED\uDDF0-\uDDFF]|\uDDF3\uD83C[\uDDE6\uDDE8\uDDEA-\uDDEC\uDDEE\uDDF1\uDDF4\uDDF5\uDDF7\uDDFA\uDDFF]|\uDDF4\uD83C\uDDF2|\uDDF5\uD83C[\uDDE6\uDDEA-\uDDED\uDDF0-\uDDF3\uDDF7-\uDDF9\uDDFC\uDDFE]|\uDDF6\uD83C\uDDE6|\uDDF7\uD83C[\uDDEA\uDDF4\uDDF8\uDDFA\uDDFC]|\uDDF8\uD83C[\uDDE6-\uDDEA\uDDEC-\uDDF4\uDDF7-\uDDF9\uDDFB\uDDFD-\uDDFF]|\uDDF9\uD83C[\uDDE6\uDDE8\uDDE9\uDDEB-\uDDED\uDDEF-\uDDF4\uDDF7\uDDF9\uDDFB\uDDFC\uDDFF]|\uDDFA\uD83C[\uDDE6\uDDEC\uDDF2\uDDF3\uDDF8\uDDFE\uDDFF]|\uDDFB\uD83C[\uDDE6\uDDE8\uDDEA\uDDEC\uDDEE\uDDF3\uDDFA]|\uDDFC\uD83C[\uDDEB\uDDF8]|\uDDFD\uD83C\uDDF0|\uDDFE\uD83C[\uDDEA\uDDF9]|\uDDFF\uD83C[\uDDE6\uDDF2\uDDFC]|[\uDC04\uDCCF\uDD8E\uDD91-\uDD9A\uDE01\uDE1A\uDE2F\uDE32-\uDE36\uDE38-\uDE3A\uDE50\uDE51\uDF00-\uDF07\uDF09-\uDF20\uDF2D-\uDF35\uDF37-\uDF7C\uDF7E-\uDF84\uDF86-\uDF93\uDFA0-\uDFC1\uDFC5\uDFC6\uDFC8\uDFC9\uDFCF-\uDFD3\uDFE0-\uDFF0\uDFF8-\uDFFA])|\u26F9(?:(?:(?:\uD83C[\uDFFB-\uDFFF]|\uFE0F)\u200D[\u2640\u2642])?\uFE0F|\uD83C[\uDFFB-\uDFFF])|\u2764\uFE0F\u200D(?:\uD83D\uDD25|\uD83E\uDE79)|[\#\*0-9]\uFE0F\u20E3|[\u261D\u270C\u270D]\uD83C[\uDFFB-\uDFFF]|[\u270A\u270B](?:\uD83C[\uDFFB-\uDFFF])?|[\u00A9\u00AE\u203C\u2049\u2122\u2139\u2194-\u2199\u21A9\u21AA\u2328\u23CF\u23ED-\u23EF\u23F1\u23F2\u23F8-\u23FA\u24C2\u25AA\u25AB\u25B6\u25C0\u25FB\u25FC\u2600-\u2604\u260E\u2611\u2618\u261D\u2620\u2622\u2623\u2626\u262A\u262E\u262F\u2638-\u263A\u2640\u2642\u265F\u2660\u2663\u2665\u2666\u2668\u267B\u267E\u2692\u2694-\u2697\u2699\u269B\u269C\u26A0\u26A7\u26B0\u26B1\u26C8\u26CF\u26D1\u26D3\u26E9\u26F0\u26F1\u26F4\u26F7\u26F8\u2702\u2708\u2709\u270C\u270D\u270F\u2712\u2714\u2716\u271D\u2721\u2733\u2734\u2744\u2747\u2763\u2764\u27A1\u2934\u2935\u2B05-\u2B07\u3030\u303D\u3297\u3299]\uFE0F|[\u231A\u231B\u23E9-\u23EC\u23F0\u23F3\u25FD\u25FE\u2614\u2615\u2648-\u2653\u267F\u2693\u26A1\u26AA\u26AB\u26BD\u26BE\u26C4\u26C5\u26CE\u26D4\u26EA\u26F2\u26F3\u26F5\u26FA\u26FD\u2705\u2728\u274C\u274E\u2753-\u2755\u2757\u2795-\u2797\u27B0\u27BF\u2B1B\u2B1C\u2B50\u2B55]",
            RegexOptions.Compiled);

        public RuleResult Check(Message message)
        {
            int numEmotes = message.Details.Emotes.Count;
            int numEmojis = EmojiRegex.Matches(message.MessageText).Count;
            int numActionable = numEmotes + numEmojis - _freeEmotes;
            if (numActionable > 0)
            {
                return new RuleResult.GivePoints(
                    (int)Math.Pow(numActionable, _powerOfEmotes),
                    "excessive usage of emotes/emojis");
            }
            return new RuleResult.Nothing();
        }
    }

    /// Copypastaing recent messages accumulates points.
    public class CopypastaRule : IModerationRule
    {
        public string Id => "copypasta";

        private static readonly NormalizedLevenshtein NormLevenshtein = new();
        private readonly float _pointsPerCopypastaChar;
        private readonly TtlQueue<string> _recentMessages;
        private readonly int _minMessageLength;
        private readonly double _minSimilarity;

        public CopypastaRule(
            IClock clock,
            Duration? recentMessagesTtl = null,
            float pointsPerCopypastaChar = 1,
            int minMessageLength = 60,
            double minSimilarity = 0.75)
        {
            Duration ttl = recentMessagesTtl ?? Duration.FromMinutes(2);
            _recentMessages = new TtlQueue<string>(ttl, clock);
            _pointsPerCopypastaChar = pointsPerCopypastaChar;
            _minMessageLength = minMessageLength;
            _minSimilarity = minSimilarity;
        }

        private bool IsCopypasta(string message)
        {
            if (message.Length < _minMessageLength)
                return false;
            bool isCopypasta = _recentMessages.Any(m => NormLevenshtein.Similarity(m, message) > _minSimilarity);
            _recentMessages.Enqueue(message);
            return isCopypasta;
        }

        public RuleResult Check(Message message) =>
            IsCopypasta(message.MessageText)
                ? new RuleResult.GivePoints(
                    (int)(_pointsPerCopypastaChar * message.MessageText.Length),
                    "participating in copypasta")
                : new RuleResult.Nothing();
    }

    /// Excessive amounts of symbols that are typically rare in regular text accumulate points.
    /// This detects e.g. zalgo or ascii-art
    public class UnicodeCharacterCategoryRule : IModerationRule
    {
        public string Id => "unicode-char-category";

        private readonly float _pointsPerBadChar;
        private readonly double _minBadness;
        private readonly int _minMessageLength;

        public UnicodeCharacterCategoryRule(
            int pointsPerBadChar = 2,
            double minBadness = 0.3,
            int minMessageLength = 60)
        {
            _pointsPerBadChar = pointsPerBadChar;
            _minBadness = minBadness;
            _minMessageLength = minMessageLength;
        }

        public RuleResult Check(Message message)
        {
            if (message.MessageText.Length < _minMessageLength)
                return new RuleResult.Nothing();
            int numGood = 0;
            int numBad = 0;
            foreach (char c in message.MessageText.Normalize(NormalizationForm.FormD))
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(c);
                if (c == ' ' || category is LowercaseLetter or UppercaseLetter or DecimalDigitNumber)
                    numGood++;
                else
                    numBad++;
            }
            int total = numGood + numBad;
            Debug.Assert(total > 0);
            double badness = numBad / (double)total;
            if (badness > _minBadness)
            {
                return new RuleResult.GivePoints(
                    (int)(_pointsPerBadChar * numBad),
                    "suspiciously excessive usage of special characters");
            }
            return new RuleResult.Nothing();
        }
    }
}
