using log4net;
using log4net.Config;
using System.Reflection;
using CommandLine;


namespace TPPCore.Service.Common
{
    /// <summary>
    /// Starts up a service by collecting command line arguments and
    /// configuration and passing it to the service.
    /// </summary>
    public class ServiceRunner
    {
        private static readonly ILog logger = LogManager.GetLogger(
            System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static int Run(IService service, string[] args)
        {
            logger.Info("Starting service");

            var argResult = Parser.Default.ParseArguments<ServiceRunnerOptions>(args);

            // TODO: use the command args to set up logging, config, etc
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            BasicConfigurator.Configure(logRepository);

            var context = new ServiceContext();

            service.Initialize(context);

            // TODO: run this service in a loop based on command args
            logger.Info("Running service");
            service.Run();

            // TODO: handle a way to shutdown the service gracefully
            // instructed by the user
            // service.Shutdown();

            logger.Info("Service stopping");

            return 0;
        }
    }
}
