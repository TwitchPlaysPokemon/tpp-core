using System.Collections.Generic;
using System.Threading.Tasks;
using Persistence.Models;

namespace Persistence.Repos
{
    public interface IBadgeRepo
    {
        public Task<Badge> AddBadge(string? userId, string species, Badge.BadgeSource source);
        public Task<List<Badge>> FindByUser(string? userId);
    }
}
