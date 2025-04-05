using TPP.Common;

namespace TPP.Model;

public record BadgeStat(
    PkmnSpecies Species,
    int Count,
    int CountGenerated,
    int RarityCount,
    int RarityCountGenerated,
    double Rarity);
