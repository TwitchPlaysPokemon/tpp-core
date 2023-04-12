using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DocoptNet;
using JsonNet.ContractResolvers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema.Generation;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.Discord;
using TPP.Core.Configuration;
using TPP.Core.Modes;
using TPP.Core.Utils;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace TPP.Core
{
    internal static class Program
    {
        private static string Usage => $@"
Usage:
  core start --mode=<mode> [--config=<file>] [--mode-config=<file>]
  core testconfig [--mode=<mode>] [--config=<file>] [--mode-config=<file>]
  core gendefaultconfig [--mode=<mode>] [--outfile=<file>]
  core regenjsonschemas
  core -h | --help

Options:
  -h --help                  Show this screen.
  -m <mode>, --mode=<mode>   Name of the mode to use. Possible values: {string.Join(", ", DefaultConfigs.Keys)}
  --config=<file>            Specifies the base config file for all modes. [default: config.json]
  --mode-config=<file>       Specifies the mode-specific config file. [default: config.<mode>mode.json]
  --outfile=<file>           Specifies the file to output to. Default is printing to stdout.
";

        // null for no mode-specific config
        private static readonly Dictionary<string, IRootConfig?> DefaultConfigs = new Dictionary<string, IRootConfig?>
        {
            ["run"] = new RunmodeConfig(),
            ["match"] = new MatchmodeConfig(),
            ["dualcore"] = null,
            ["dummy"] = null,
        };
        private static bool ModeHasItsOwnConfig(string mode) => DefaultConfigs[mode] != null;

        /// Ensures different environments don't change the application's behaviour.
        private static void NormalizeRuntimeEnvironment()
        {
            // Just always use UTF-8, don't bother with weird encodings.
            Console.InputEncoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;
            // Stuff like double.Parse(str) is culture dependent, which is a great way to introduce subtle bugs.
            // Sadly we can't use the 'InvariantGlobalization' project property for this, since it additionally silently
            // makes string unicode normalization be a no-op, which is another great way to introduce subtle bugs.
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        }

        private static void Main(string[] argv)
        {
            NormalizeRuntimeEnvironment();

            IDictionary<string, ValueObject> args = new Docopt().Apply(Usage, argv, exit: true)!;
            string? mode = null;
            string modeConfigFilename = args["--mode-config"].ToString();
            if (args["--mode"] is { IsNullOrEmpty: false })
            {
                mode = args["--mode"].ToString();
                if (!DefaultConfigs.ContainsKey(mode))
                {
                    Console.WriteLine(
                        $"Unknown mode '{mode}', possible values: {string.Join(", ", DefaultConfigs.Keys)}");
                    Environment.Exit(1);
                }
                modeConfigFilename = modeConfigFilename.Replace("<mode>", mode);
            }
            if (args["start"].IsTrue) Mode(mode!, args["--config"].ToString(), modeConfigFilename);
            else if (args["testconfig"].IsTrue) TestConfig(args["--config"].ToString(), mode, modeConfigFilename);
            else if (args["gendefaultconfig"].IsTrue) OutputDefaultConfig(mode, args["--outfile"]);
            else if (args["regenjsonschemas"].IsTrue) RegenerateJsonSchema();
        }

        private static readonly JsonSerializerSettings ConfigSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new PrivateSetterContractResolver(),
            Converters = { new StringEnumConverter() },
            Formatting = Formatting.Indented,
            // if we don't force objects to be fully replaced, it might e.g. append to default collections,
            // making it impossible to remove elements from said collections via configuration.
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        private static T ReadConfig<T>(string filename) where T : ConfigBase
        {
            return (T)ReadConfig(filename, typeof(T));
        }

        private static ConfigBase ReadConfig(string filename, Type type)
        {
            if (!type.IsSubclassOf(typeof(ConfigBase))) throw new ArgumentException("", nameof(type));
            string json = File.ReadAllText(filename);
            var config = (ConfigBase?)JsonConvert.DeserializeObject(json, type, ConfigSerializerSettings);
            if (config == null) throw new ArgumentException("config must not be null");
            ConfigUtils.WriteUnrecognizedConfigsToStderr(config);
            if (config is BaseConfig { LogPath: null })
            {
                Console.Error.WriteLine("no logging path is configured, logs will only be printed to console");
            }
            return config;
        }

        private static ILoggerFactory BuildLoggerFactory(BaseConfig baseConfig) =>
            LoggerFactory.Create(builder =>
            {
                IDictionary<string, LogLevel> levelOverrides = new Dictionary<string, LogLevel>
                {
                    // The TwitchHttpClientHandler, which uses the below log category,
                    // logs all requests and successful responses at info level, and failed responses at error level.
                    // That is counterproductive for at least these reasons:
                    // - Logging requests may include auth calls, which include secrets in their query params.
                    // - Logging all errors, even though those errors manifest themselves as exceptions,
                    //   prevents us from catching and deliberately ignoring some error cases.
                    ["TwitchLib.Api.Core.HttpCallHandlers.TwitchHttpClient"] = LogLevel.Critical,
                    // Also mute TwitchLib.Client's warn level until https://github.com/TwitchLib/TwitchLib.Client/pull/218 is fixed
                    ["TwitchLib.Client"] = LogLevel.Error,
                };

                builder.SetMinimumLevel(LogLevel.Debug);
                foreach ((string loggerPrefix, LogLevel logLevel) in levelOverrides)
                    builder.AddFilter(loggerPrefix, logLevel);

                builder.AddConsole();
                if (baseConfig.LogPath != null)
                {
                    builder.AddFile(
                        pathFormat: Path.Combine(baseConfig.LogPath, "tpp-{Date}.log"),
                        outputTemplate: "{Timestamp:o} [{Level:u3}] {Message}{NewLine}{Exception}",
                        levelOverrides: levelOverrides);
                }
                if (baseConfig.DiscordLoggingConfig != null)
                {
                    builder.AddSerilog(new LoggerConfiguration()
                        .WriteTo.Discord(baseConfig.DiscordLoggingConfig.WebhookId, baseConfig.DiscordLoggingConfig.WebhookToken)
                        .MinimumLevel.Is(baseConfig.DiscordLoggingConfig.MinLogLevel)
                        .Filter.ByExcluding(logEvent =>
                        {
                            string? context = (logEvent.Properties["SourceContext"] as ScalarValue)?.Value as string;
                            if (context == null)
                                return false;
                            foreach ((string loggerPrefix, LogLevel logLevel) in levelOverrides)
                                if (context.StartsWith(loggerPrefix) && logEvent.Level < logLevel.ToSerilogLogLevel())
                                    return true;
                            return false;
                        })
                        .CreateLogger());
                }
            });

        private static void Mode(string modeName, string baseConfigFilename, string modeConfigFilename)
        {
            if (ModeHasItsOwnConfig(modeName) && !File.Exists(baseConfigFilename))
            {
                Console.Error.WriteLine(MissingConfigErrorMessage(null, baseConfigFilename));
                return;
            }
            if (ModeHasItsOwnConfig(modeName) && !File.Exists(modeConfigFilename))
            {
                Console.Error.WriteLine(MissingConfigErrorMessage(modeName, modeConfigFilename));
                return;
            }
            BaseConfig baseConfig = ReadConfig<BaseConfig>(baseConfigFilename);
            ILoggerFactory loggerFactory = BuildLoggerFactory(baseConfig);
            ILogger logger = loggerFactory.CreateLogger("main");
            IMode mode = modeName switch
            {
                "run" => new Runmode(loggerFactory, baseConfig, () => ReadConfig<RunmodeConfig>(modeConfigFilename)),
                "match" => new Matchmode(loggerFactory, baseConfig, ReadConfig<MatchmodeConfig>(modeConfigFilename)),
                "dualcore" => new DualcoreMode(loggerFactory, baseConfig),
                "dummy" => new DummyMode(loggerFactory, baseConfig),
                _ => throw new NotSupportedException($"Invalid mode '{modeName}'")
            };
            TaskCompletionSource<bool> cleanupDone = new();
            try
            {
                Task modeTask = mode.Run();
                void Abort(object? sender, EventArgs args)
                {
                    logger.LogInformation("Aborting mode...");
                    mode.Cancel();
                    cleanupDone.Task.Wait();
                }
                AppDomain.CurrentDomain.ProcessExit += Abort; // SIGTERM
                Console.CancelKeyPress += Abort; // CTRL-C / SIGINT
                modeTask.Wait();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "uncaught exception! TPP is crashing now, goodbye");
            }
            loggerFactory.Dispose();
            cleanupDone.SetResult(true);
        }

        private static string MissingConfigErrorMessage(string? mode, string configFilename) =>
            $"missing {(mode == null ? "base" : "mode")} config file '{configFilename}'. " +
            $"Generate one from default values using 'gendefaultconfig{(mode == null ? "" : " --mode=" + mode)} " +
            $"--outfile={configFilename}'";

        private static void TestConfig(
            string configFilename,
            string? mode,
            string modeConfigFilename)
        {
            // just try to read the configs, don't do anything with them

            if (File.Exists(configFilename))
                ReadConfig<BaseConfig>(configFilename);
            else
                Console.Error.WriteLine(MissingConfigErrorMessage(null, configFilename));

            if (mode != null && ModeHasItsOwnConfig(mode))
            {
                if (File.Exists(modeConfigFilename))
                    ReadConfig(modeConfigFilename, DefaultConfigs[mode]!.GetType());
                else
                    Console.Error.WriteLine(MissingConfigErrorMessage(mode, modeConfigFilename));
            }
        }

        private static void OutputDefaultConfig(string? modeName, ValueObject? outfileArgument)
        {
            IRootConfig? config = modeName switch
            {
                null => new BaseConfig(),
                var name when DefaultConfigs.ContainsKey(name) => DefaultConfigs[name],
                _ => throw new NotSupportedException($"Invalid mode '{modeName}'")
            };
            if (config == null)
            {
                Console.Error.WriteLine($"mode '{modeName}' has no config file.");
                return;
            }
            string json = JsonConvert.SerializeObject(config, ConfigSerializerSettings);
            if (config is BaseConfig)
            {
                json = "/* DO NOT SHARE THIS FILE --- IT MAY CONTAIN SECRETS! */\n" +
                       "/* You can omit entries to set them to their default value. */\n" +
                       json + "\n";
            }
            if (outfileArgument == null || outfileArgument.IsNullOrEmpty) Console.Write(json);
            else File.WriteAllText(outfileArgument.ToString(), json, Encoding.UTF8);
        }

        private static void RegenerateJsonSchema()
        {
            var generator = new JSchemaGenerator();
            generator.GenerationProviders.Add(new StringEnumGenerationProvider());
            generator.DefaultRequired = Required.Default;
            generator.ContractResolver = new PrivateSetterContractResolver();
            IRootConfig baseConfig = new BaseConfig();
            string GetSchemaText(Type type) =>
                generator.Generate(type).ToString().Replace("\r\n", "\n") + '\n';
            File.WriteAllText(baseConfig.Schema, GetSchemaText(typeof(BaseConfig)), Encoding.UTF8);
            Console.Error.WriteLine($"Wrote base json schema to '{baseConfig.Schema}',");

            foreach ((string modeName, IRootConfig? defaultModeConfig) in DefaultConfigs)
            {
                if (defaultModeConfig == null) continue;
                File.WriteAllText(defaultModeConfig.Schema, GetSchemaText(defaultModeConfig.GetType()), Encoding.UTF8);
                Console.Error.WriteLine($"Wrote mode '{modeName}' json schema to '{defaultModeConfig.Schema}'");
            }
        }
    }
}
