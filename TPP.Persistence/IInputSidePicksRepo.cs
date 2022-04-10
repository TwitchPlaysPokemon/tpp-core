using System.Threading.Tasks;

namespace TPP.Persistence;

public interface IInputSidePicksRepo
{
    public Task SetSide(string userId, string? side);
    public Task<string?> GetSide(string userId);
    public Task ClearAll();
}
