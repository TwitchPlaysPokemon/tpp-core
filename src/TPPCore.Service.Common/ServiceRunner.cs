using log4net;
using log4net.Config;
using System.Reflection;
using CommandLine;
using System;
using System.IO;

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

            var options = parseCommandLineArgs(args);
            setUpLogging(options);
            var context = setUpContext(options);
            var running = true;
            service.Initialize(context);

            Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    if (running)
                    {
                        logger.Info("Attempting to shut down service...");
                        logger.Info("Use the Cancel key press again to force exit.");
                        running = false;
                        service.Shutdown();
                        eventArgs.Cancel = true;
                    }
                    else
                    {
                        logger.Warn("Exiting without shutdown");
                        eventArgs.Cancel = false;
                    }
                };

            while (running)
            {
                try
                {
                    logger.Info("Running service");
                    service.Run();
                }
                catch (Exception error)
                {
                    if (options.RestartOnError)
                    {
                        logger.Error("Service error", error);
                    } else {
                        logger.Fatal("Service error", error);
                        throw;
                    }
                }
            }

            logger.Info("Service stopping");

            return 0;
        }

        private static ServiceRunnerOptions parseCommandLineArgs(string[] args)
        {
            var argResult = Parser.Default.ParseArguments<ServiceRunnerOptions>(args)
                .WithNotParsed(errors => Environment.Exit(1));
            var parsedResult = (Parsed<ServiceRunnerOptions>) argResult;
            var options = parsedResult.Value;
            return options;
        }

        private static void showHelp()
        {
            Parser.Default.ParseArguments<ServiceRunnerOptions>(
                new string[] {"--help"});
        }

        private static void setUpLogging(ServiceRunnerOptions options)
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());

            if (options.LogConfig != null)
            {
                if (!File.Exists(options.LogConfig))
                {
                    Console.Error.WriteLine("Config {0} does not exist.", options.LogConfig);
                    showHelp();
                    Environment.Exit(1);
                }

                var fileInfo = new FileInfo(options.LogConfig);
                XmlConfigurator.Configure(logRepository, fileInfo);
                logger.DebugFormat("Logging was configured from file {0}",
                    options.LogConfig);
            }
            else
            {
                BasicConfigurator.Configure(logRepository);
            }
        }

        private static ServiceContext setUpContext(ServiceRunnerOptions options)
        {
            var context = new ServiceContext();

            // TODO: parse the pub sub addresses, etc
            context.InitPubSubClient();

            return context;
        }
    }
}
