using System;
using System.Collections.Generic;

namespace TPP.Core.Chat;

public class MessageSplitter
{
    private readonly int _maxMessageLength;
    private readonly string _messageSplitAppendix;
    private readonly char _splitChar;

    public MessageSplitter(
        int maxMessageLength,
        string messageSplitAppendix = "...",
        char splitChar = ' ')
    {
        if (messageSplitAppendix.Length >= maxMessageLength)
        {
            throw new ArgumentException("Maximum message length max not exceed the split appendix length.");
        }
        _maxMessageLength = maxMessageLength;
        _messageSplitAppendix = messageSplitAppendix;
        _splitChar = splitChar;
    }

    public IEnumerable<string> FitToMaxLength(string message)
    {
        int maxPartLength = _maxMessageLength - _messageSplitAppendix.Length;
        int pos = 0;
        int Remaining() => message.Length - pos;
        while (Remaining() > _maxMessageLength)
        {
            int availableLength = Math.Min(maxPartLength, Remaining());
            int splitIndex = message.LastIndexOf(_splitChar, pos + availableLength - 1, availableLength - 1);
            int partLength = splitIndex - pos + 1; // keep the space
            bool autoSplitSufficientlyLarge = partLength >= availableLength * (2f / 3f);
            if (splitIndex == -1 || !autoSplitSufficientlyLarge)
            {
                partLength = availableLength;
            }
            yield return message.Substring(pos, partLength) + _messageSplitAppendix;
            pos += partLength;
        }
        yield return message[pos..];
    }
}
