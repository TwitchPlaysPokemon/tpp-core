using System.Collections.Generic;
using System.Threading.Tasks;
using Common;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IBadgeRepo
    {
        public Task<Badge> AddBadge(string? userId, PkmnSpecies species, Badge.BadgeSource source);
        public Task<List<Badge>> FindByUser(string? userId);
    }
}
