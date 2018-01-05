using log4net;
using log4net.Config;
using System.Reflection;
using CommandLine;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;

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

        public IService Service { get { return service;} }
        public ServiceRunnerOptions Options { get { return options; } }
        public ServiceContext Context { get { return context; } }

        private IService service;
        private ServiceRunnerOptions options;
        private ServiceContext context;
        private Boolean running = false;

        public ServiceRunner(IService service)
        {
            this.service = service;
        }

        public static int Run(IService service, string[] args)
        {
            var runner = new ServiceRunner(service);

            runner.Configure(args);
            runner.StartRestfulServer();
            runner.UseCancelKeyPress();
            runner.Run();
            runner.StopRestfulServer();

            logger.Info("Service stopped");

            return 0;
        }

        public void Configure()
        {
            Configure(new string[] {});
        }

        public void Configure(string[] args)
        {
            Debug.Assert(options == null);
            Debug.Assert(context == null);

            logger.Info("Configuring service");

            options = parseCommandLineArgs(args);
            setUpLogging();
            context = setUpContext();
            service.Initialize(context);
        }

        public void StartRestfulServer()
        {
            StartRestfulServerAsync().Wait();
        }

        public async Task StartRestfulServerAsync()
        {
            logger.Info("Starting the RESTful web host");
            context.RestfulServer.BuildWebHost();
            await context.RestfulServer.AspNetWebHost.StartAsync();

            context.RestfulServer.UpdateRealPort();

            logger.InfoFormat("The RESTful web host is running at port {0}",
                context.RestfulServer.Context.RealPort);
        }

        public void UseCancelKeyPress()
        {
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                if (running)
                {
                    logger.Info("Attempting to shut down service...");
                    logger.Info("Use the Cancel key press again to force exit.");
                    running = false;
                    Stop();
                    eventArgs.Cancel = true;
                }
                else
                {
                    logger.Warn("Exiting without shutdown");
                    eventArgs.Cancel = false;
                }
            };
        }

        public void Run()
        {
            Debug.Assert(running == false);
            Debug.Assert(context != null);
            running = true;

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
        }

        public async Task RunAsync()
        {
            await Task.Run(new Action(service.Run));
        }

        public void StopRestfulServer()
        {
            StopRestfulServerAsync().Wait();
        }

        public async Task StopRestfulServerAsync()
        {
            logger.Info("Stopping the RESTful web host");
            await context.RestfulServer.AspNetWebHost.StopAsync();
        }

        public void Stop()
        {
            service.Shutdown();
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

        private void setUpLogging()
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

        private static ServiceContext setUpContext()
        {
            var context = new ServiceContext();

            // TODO: parse the pub sub addresses, etc
            context.InitPubSubClient();
            context.InitRestfulServer();

            return context;
        }
    }
}
