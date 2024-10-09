using System.Threading.Tasks;

namespace TPP.Persistence;

public interface IRunCounterRepo
{
    public Task<long> Increment(int? runNumber, int incrementBy = 1);
    public Task<long> Get(int? runNumber);
}
