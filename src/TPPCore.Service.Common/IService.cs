using System.Threading.Tasks;

namespace TPPCore.Service.Common
{
    /// <summary>
    /// A microservice.
    /// </summary>
    public interface IService
    {
        /// <summary>
        /// Initializes and configures the service to be run later.
        /// </summary>
        void Initialize(ServiceContext context);

        /// <summary>
        /// Executes the microservice.
        /// </summary>
        void Run();

        /// <summary>
        /// Requests the microservice to be stopped from running.
        /// </summary>
        void Shutdown();
    }

    /// <summary>
    /// A microservice that runs under the Task-based Asynchronous Pattern (TAP)
    /// </summary>
    public interface IServiceAsync : IService
    {
        /// <summary>
        /// Executes the microservice async.
        /// </summary>
        Task RunAsync();
    }
}
