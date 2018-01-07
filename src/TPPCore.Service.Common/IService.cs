using System.Threading.Tasks;

namespace TPPCore.Service.Common
{
    public interface IService
    {
        void Initialize(ServiceContext context);
        void Run();
        void Shutdown();
    }

    public interface IServiceAsync : IService
    {
        Task RunAsync();
    }
}
