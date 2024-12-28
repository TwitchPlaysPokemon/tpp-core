using System.Threading.Tasks;

namespace TPP.Persistence.MongoDB;

/// A repository that has additional async initialization steps.
/// Calling the <see cref="InitializeAsync"/> method is mandatory before using this repository,
/// as it's basically meant as a second phase of initialization after the constructor.
/// In theory, instead of doing it this way, the repo could do the async bits in its constructor by blocking on IO,
/// but by extracting those bits it makes it possible to parallelize them when initializing multiple repositories.
/// It's a trade-off: giving up some initialization simplicity for improved program startup times.
///
/// Note 2024-12-29 Felk: Testing locally, this reduced the total duration from 300ms to 200ms.
/// That isn't a lot, but assuming the difference is larger on stream due to the DB not being localhost, let's keep it.
public interface IAsyncInitRepo
{
    public Task InitializeAsync();
}
