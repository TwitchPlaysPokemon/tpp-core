using CommandLine;
using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

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
        private ConfigReader configReader;
        private Boolean running = false;

        public ServiceRunner(IService service)
        {
            this.service = service;
        }

        /// <summary>
        /// Creates a new service runner and automatically configures and runs
        /// the service.
        /// </summary>
        /// <remarks>
        /// This method should be called by command line programs.
        /// </remarks>
        public static int Run(IService service, string[] args)
        {
            var runner = new ServiceRunner(service);

            try
            {
                runner.Configure(args);
            }
            catch (ConfigException error)
            {
                logger.FatalFormat("Could not configure service: {0}", error.Message);
                Console.Error.WriteLine(error.Message);
                Environment.Exit(1);
            }
            runner.StartRestfulServer();
            runner.UseCancelKeyPress();
            runner.Run();
            runner.StopRestfulServer();
            logger.Info("Service stopped.");
            // Remove loggers etc for unit test runners or manual control
            runner.CleanUp();

            return 0;
        }

        public void Configure()
        {
            Configure(new string[] {});
        }

        /// <summary>
        /// Create the necessary contexts and configuration and provide it to
        /// the service.
        /// </summary>
        public void Configure(string[] args)
        {
            var options = parseCommandLineArgs(args);
            Configure(options);
        }

        public void Configure(ServiceRunnerOptions options)
        {
            Debug.Assert(this.options == null);
            Debug.Assert(context == null);

            logger.Info("Configuring service");

            this.options = options;
            setUpLogging();
            processConfigFiles();
            setUpContext();
            service.Initialize(context);
        }

        public void StartRestfulServer()
        {
            StartRestfulServerAsync().Wait();
        }

        /// <summary>
        /// Starts the web host containing the RESTful server and updates
        /// the related contexts.
        /// </summary>
        public async Task StartRestfulServerAsync()
        {
            logger.Info("Starting the RESTful web host");
            context.RestfulServer.BuildWebHost();
            await context.RestfulServer.AspNetWebHost.StartAsync();

            context.RestfulServer.UpdateRealPort();

            logger.InfoFormat("The RESTful web host is running at port {0}",
                context.RestfulServer.Context.RealPort);
        }

        /// <summary>
        /// Attach a Control+C key press handler to call <see cref="Stop"/.>
        /// </summary>
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

        /// <summary>
        /// Runs the service and blocks until it exits.
        /// </summary>
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
                    if (service is IServiceAsync)
                    {
                        (service as IServiceAsync).RunAsync().Wait();
                    }
                    else
                    {
                        service.Run();
                    }
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

                if (!options.RestartOnError)
                {
                    break;
                }
            }
        }

        public async Task RunAsync()
        {
            if (service is IServiceAsync)
            {
                await (service as IServiceAsync).RunAsync();
            }
            else
            {
                await Task.Run(new Action(service.Run));
            }
        }

        public void StopRestfulServer()
        {
            StopRestfulServerAsync().Wait();
        }

        /// <summary>
        /// Stop the web host from running.
        /// </summary>
        /// <returns></returns>
        public async Task StopRestfulServerAsync()
        {
            logger.Info("Stopping the RESTful web host");
            await context.RestfulServer.AspNetWebHost.StopAsync();
        }

        /// <summary>
        /// Request the service to shutdown and stop running.
        /// </summary>
        public void Stop()
        {
            service.Shutdown();
        }

        /// <summary>
        /// Perform clean up operations on data created by configuration steps.
        /// </summary>
        /// <remarks>
        /// Use this method to undo any global state changes such as global
        /// logger configuration. This is useful for unit tests.
        /// </remarks>
        public void CleanUp()
        {
            tearDownLogging();
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

        private void tearDownLogging()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            logRepository.Shutdown();
        }

        private void processConfigFiles()
        {
            configReader = new ConfigReader();

            foreach (var path in options.ConfigFiles)
            {
                configReader.Load(path);
            }

            if (options.ConfigStream != null)
            {
                configReader.Load(options.ConfigStream);
            }
        }

        private void setUpContext()
        {
            context = new ServiceContext();

            context.InitConfigReader(configReader);
            context.InitServiceAssignment(configReader);

            setUpPubSubClient();
            setUpRestfulServer();

            context.InitRestfulClient();
        }

        private void setUpPubSubClient()
        {
            var pubsubImpl = configReader.GetCheckedValueOrDefault<string, ServiceRunnerConfig>(
                new[] {"service", "pubSub"}, "dummy");

            if (pubsubImpl == "redis")
            {
                var host = configReader.GetCheckedValue<string, ServiceRunnerConfig>("redis", "host");
                var port = configReader.GetCheckedValue<int, ServiceRunnerConfig>("redis", "port");
                var extra = configReader.GetCheckedValueOrDefault<string, ServiceRunnerConfig>(new[] {"redis", "extra"}, "");
                var redisClient = new RedisPubSubClient(host, port, extra);

                logger.InfoFormat("Using pub/sub Redis address at {0}:{1}", host, port);
                context.InitPubSubClient(redisClient);
            }
            else
            {
                logger.Info("Using a dummy pub/sub client.");
                context.InitPubSubClient();
            }
        }

        private void setUpRestfulServer()
        {
            var host = configReader.GetCheckedValueOrDefault<string, ServiceRunnerConfig>(
                new[] {"restful", "host"}, "localhost");
            var port = configReader.GetCheckedValueOrDefault<int, ServiceRunnerConfig>(
                new[] {"restful", "port"}, 0);

            IPAddress ipAddress;

            if (host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
            {
                ipAddress = IPAddress.Loopback;
            }
            else
            {
                ipAddress = IPAddress.Parse(host);
            }
            context.InitRestfulServer(ipAddress, port);
        }
    }
}
