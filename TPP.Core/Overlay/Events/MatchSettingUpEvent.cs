using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using TPP.Model;

namespace TPP.Core.Overlay.Events;

[DataContract]
public struct MovesInputOptions
{
    [DataMember(Name = "policy")] public MoveSelectingPolicy Policy { get; set; }
    [DataMember(Name = "permitted")] public IImmutableList<string> Permitted { get; set; } // TODO
}

[DataContract]
public struct SwitchesInputOptions
{
    // TODO does this even differ from the match's switching policy...?
    [DataMember(Name = "policy")] public SwitchingPolicy Policy { get; set; }
    [DataMember(Name = "permitted")] public IImmutableList<string> Permitted { get; set; } // TODO
    [DataMember(Name = "random_chance")] public float RandomChance { get; set; }
}

[DataContract]
public struct TargetsInputOptions
{
    [DataMember(Name = "policy")] public TargetingPolicy Policy { get; set; }
    [DataMember(Name = "permitted")] public IImmutableList<string> Permitted { get; set; } // TODO
    [DataMember(Name = "ally_hit_chance")] public float AllyHitChance { get; set; }
}

[DataContract]
public struct InputOptions
{
    [DataMember(Name = "moves")] public MovesInputOptions Moves { get; set; }
    [DataMember(Name = "switches")] public SwitchesInputOptions Switches { get; set; }
    [DataMember(Name = "targets")] public TargetsInputOptions Targets { get; set; }
}

/// The overlay expects the teams to be a 2-sized list.
internal class TeamsConverter : JsonConverter<Teams>
{
    public override void WriteJson(JsonWriter writer, Teams value, JsonSerializer serializer)
    {
        writer.WriteStartArray();
        serializer.Serialize(writer, value.Blue);
        serializer.Serialize(writer, value.Red);
        writer.WriteEndArray();
    }

    public override Teams ReadJson(JsonReader reader, System.Type objectType, Teams existingValue, bool hasExistingValue,
        JsonSerializer serializer)
    {
        var sets = serializer.Deserialize<List<List<Pokemon>>>(reader);
        Debug.Assert(sets != null);
        return new Teams { Blue = sets[0].ToImmutableList(), Red = sets[1].ToImmutableList() };
    }
}

[JsonConverter(typeof(TeamsConverter))]
public struct Teams
{
    [DataMember(Name = "blue")] public IImmutableList<Pokemon> Blue { get; set; }
    [DataMember(Name = "red")] public IImmutableList<Pokemon> Red { get; set; }
}

[DataContract]
public struct MatchSettingUpEvent : IOverlayEvent
{
    public string OverlayEventType => "match_setting_up";

    [DataMember(Name = "match_id")] public int MatchId { get; set; }
    [DataMember(Name = "teams")] public Teams Teams { get; set; }
    [DataMember(Name = "betting_duration")] public double BettingDuration { get; set; }
    [DataMember(Name = "reveal_duration")] public double RevealDuration { get; set; }
    [DataMember(Name = "gimmick")] public string Gimmick { get; set; } // TODO
    [DataMember(Name = "switching")] public SwitchingPolicy Switching { get; set; }
    [DataMember(Name = "battle_style")] public BattleStyle BattleStyle { get; set; }
    [DataMember(Name = "input_options")] public InputOptions InputOptions { get; set; }
    [DataMember(Name = "hide_odds")] public bool HideOdds { get; set; }
    [DataMember(Name = "hide_bets")] public bool HideBets { get; set; }
    [DataMember(Name = "bet_bonus")] public int BetBonus { get; set; }
    [DataMember(Name = "display_bet_bonus")] public bool DisplayBetBonus { get; set; }
    [DataMember(Name = "bet_bonus_type")] public string BetBonusType { get; set; } // TODO
}
