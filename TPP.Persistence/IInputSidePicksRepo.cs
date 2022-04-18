using System.Threading.Tasks;
using TPP.Model;

namespace TPP.Persistence;

public interface IInputSidePicksRepo
{
    public Task SetSide(string userId, string? side);
    public Task<SidePick?> GetSidePick(string userId);
    public Task ClearAll();
}
