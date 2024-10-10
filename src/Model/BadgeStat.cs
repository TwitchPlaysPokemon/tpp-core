using Common;

namespace Model
{
    public record BadgeStat(
        PkmnSpecies Species,
        int Count,
        int CountGenerated,
        int RarityCount,
        int RarityCountGenerated,
        double Rarity);
}
