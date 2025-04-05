using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public interface ILinkedAccountRepo
{
    /// Find all users that are linked to the given user
    public Task<IImmutableSet<User>> FindLinkedUsers(string userId);

    /// Mark two users as linked, returning true if that succeeded, or false if they already were linked.
    public Task<bool> Link(IImmutableSet<string> userIds);

    /// Unmark a user as linked to any other account, returning true if that succeeded,
    /// or false if they were not linked to anyone in the first place.
    public Task<bool> Unlink(string userId);

    /// Determines if two users are linked.
    public async Task<bool> AreLinked(string userId1, string userId2) =>
        (await FindLinkedUsers(userId1)).Any(u => u.Id == userId2);
}
