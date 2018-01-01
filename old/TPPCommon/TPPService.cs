using Microsoft.Extensions.CommandLineUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using TPPCommon.Configuration;
using TPPCommon.Logging;
using TPPCommon.PubSub;

namespace TPPCommon
{
    /// <summary>
    /// Base class for all TPP services.
    /// </summary>
    public abstract class TPPService
    {
        protected abstract int StartupDelayMilliseconds { get; }

        protected abstract string[] ConfigNames { get; }
        protected IDictionary<string, string> ConfigOverrides = new Dictionary<string, string>();
        protected string ConfigFileOverride = string.Empty;

        protected IPublisher Publisher;
        protected ISubscriber Subscriber;
        protected ITPPLoggerFactory LoggerFactory;
        protected IConfigReader ConfigReader;

        public TPPService(IPublisher publisher, ISubscriber subscriber, ITPPLoggerFactory loggerFactory, IConfigReader configReader)
        {
            this.Publisher = publisher;
            this.Subscriber = subscriber;
            this.LoggerFactory = loggerFactory;
            this.ConfigReader = configReader;
        }

        protected abstract void Initialize();
        protected abstract void Run();

        protected T GetConfig<T>() where T : BaseConfig
        {
            return BaseConfig.GetConfig<T>(this.ConfigReader, this.ConfigOverrides, this.ConfigFileOverride, this.ConfigNames);
        }

        /// <summary>
        /// Initializes and runs the service.
        /// </summary>
        /// <param name="args">commandline arguments</param>
        public void RunService(params string[] args)
        {
            var app = new CommandLineApplication();
            CommandOption configOverrideOption = app.Option("--config|-c", "Override config value with \"key:value\". Multiple overrides may be given.", CommandOptionType.MultipleValue);
            CommandOption configFileOverrideOption = app.Option("--config-file|-cf", "Specify specific config file to load.", CommandOptionType.SingleValue);
            app.OnExecute(() =>
            {
                // Respect config values coming from command line arguments.
                if (configOverrideOption.HasValue())
                {
                    foreach (string kvp in configOverrideOption.Values)
                    {
                        var parts = kvp.Split(new char[] { ':' }, 2, StringSplitOptions.None);
                        string configName = parts[0];
                        string configValue = parts[1];

                        this.ConfigOverrides[configName] = configValue;
                    }
                }

                // Respect config file commandline option.
                if (configFileOverrideOption.HasValue())
                {
                    if (!File.Exists(configFileOverrideOption.Value()))
                    {
                        throw new ArgumentException($"Invalid config file specified from the commandline: {configFileOverrideOption.Value()}");
                    }

                    this.ConfigFileOverride = configFileOverrideOption.Value();
                }

                this.Initialize();

                Thread.Sleep(this.StartupDelayMilliseconds);
                this.Run();

                return 0;
            });

            app.Execute(args);
        }
    }
}
