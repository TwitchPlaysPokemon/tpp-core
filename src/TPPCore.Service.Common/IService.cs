namespace TPPCore.Service.Common
{
    public interface IService
    {
        void Initialize(ServiceContext context);
        void Run();
        void Shutdown();
    }
}
