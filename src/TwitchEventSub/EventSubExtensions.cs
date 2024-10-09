using System;
using System.Collections.Generic;
using System.Text.Json;

namespace TPP.Twitch.EventSub;

public static class EventSubExtensions
{
    /// <summary>
    /// TwitchLib consumes conditions as a generic dictionary,
    /// while we use per-event records. This method bridges the gap when subscribing by turning the object into a dict.
    /// </summary>
    public static Dictionary<string, string> AsDict<C>(this C condition) where C : Condition
    {
        string serialized = JsonSerializer.Serialize(condition, Parsing.SerializerOptions);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(serialized)
               ?? throw new ArgumentException("invalid condition, deserialized to null: " + serialized);
    }
}
