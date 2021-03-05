using TPP.Common;

namespace TPP.Persistence.Models
{
    public record BadgeStat(
        PkmnSpecies Species,
        int Count,
        int CountGenerated,
        int RarityCount,
        int RarityCountGenerated,
        double Rarity);
}
