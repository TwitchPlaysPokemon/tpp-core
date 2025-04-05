using System.Collections.Immutable;
using TPP.Model;

namespace TPP.Match;

public static class MatchTesting
{
    public static readonly Pokemon TestVenonatForOverlay = new()
    {
        Ability = new Ability { Id = 14, Name = "Compound Eyes" },
        Ball = new Item { Id = 6, Name = "Net Ball" },
        CurrHp = 262,
        Name = "Venonat",
        Setname = "Testing",
        Evs = new Stats { Atk = 252, Def = 0, Hp = 4, SpA = 0, SpD = 0, Spe = 252 },
        Ivs = new Stats { Atk = 31, Def = 31, Hp = 31, SpA = 31, SpD = 31, Spe = 31 },
        Stats = new Stats { Atk = 209, Def = 136, Hp = 262, SpA = 104, SpD = 146, Spe = 207 },
        Form = 0,
        Gender = Gender.Female,
        Happiness = 255,
        Item = new Item { Id = 245, Name = "Poison Barb" },
        Level = 100,
        Moves = ImmutableList.Create(
            new Move
            {
                Id = 305,
                Name = "Poison Fang",
                Category = Category.Physical,
                Accuracy = 100,
                Pp = 15,
                PpUps = 0,
                Target = MoveTarget.Normal,
                Type = PokemonType.Poison,
            }
        ),
        Nature = Nature.Jolly,
        Shiny = false,
        Species = new Species { Id = 48, Name = "Venonat", Types = new[] { "Bug", "Poison" } },
        Status = new Status
        {
            Nonvolatile = new NonvolatileStatus
            {
                Burn = false,
                Freeze = false,
                Paralysis = false,
                Poison = false,
                Sleep = 0,
                BadPoison = 0
            },
        },
    };
}
