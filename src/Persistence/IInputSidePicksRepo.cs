using System.Threading.Tasks;
using Model;

namespace Persistence;

public interface IInputSidePicksRepo
{
    public Task SetSide(string userId, string? side);
    public Task<SidePick?> GetSidePick(string userId);
    public Task ClearAll();
}
