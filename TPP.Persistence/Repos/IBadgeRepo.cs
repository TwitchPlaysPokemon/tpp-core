using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using TPP.Common;
using TPP.Persistence.Models;

namespace TPP.Persistence.Repos
{
    public interface IBadgeRepo
    {
        public Task<Badge> AddBadge(string? userId, PkmnSpecies species, Badge.BadgeSource source);
        public Task<List<Badge>> FindByUser(string? userId);
        public Task<long> CountByUserAndSpecies(string? userId, PkmnSpecies species);
        public Task<ImmutableSortedDictionary<PkmnSpecies, int>> CountByUserPerSpecies(string? userId);
        public Task<bool> HasUserBadge(string? userId, PkmnSpecies species);
    }
}
