using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Core.Configuration;
using Core.Modes;
using DocoptNet;
using JsonNet.ContractResolvers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema.Generation;
using Serilog;
using Serilog.Sinks.Discord;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace Core
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

        private static void Main(string[] argv)
        {
            IDictionary<string, ValueObject> args = new Docopt().Apply(Usage, argv, exit: true);
            string? mode = null;
            string modeConfigFilename = args["--mode-config"].ToString();
            if (args["--mode"] != null && !args["--mode"].IsNullOrEmpty)
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
            if (config is BaseConfig baseConfig && baseConfig.LogPath == null)
            {
                Console.Error.WriteLine("no logging path is configured, logs will only be printed to console");
            }
            return config;
        }

        private static ILoggerFactory BuildLoggerFactory(BaseConfig baseConfig) =>
            LoggerFactory.Create(builder =>
            {
                builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
                if (baseConfig.LogPath != null) builder.AddFile(Path.Combine(baseConfig.LogPath, "tpp-{Date}.log"));
                if (baseConfig.DiscordLoggingConfig != null)
                {
                    builder.AddSerilog(new LoggerConfiguration()
                        .WriteTo.Discord(baseConfig.DiscordLoggingConfig.WebhookId, baseConfig.DiscordLoggingConfig.WebhookToken)
                        .MinimumLevel.Is(baseConfig.DiscordLoggingConfig.MinLogLevel)
                        .CreateLogger());
                }
            });

        private static void Mode(string modeName, string baseConfigFilename, string modeConfigFilename)
        {
            BaseConfig baseConfig = ReadConfig<BaseConfig>(baseConfigFilename);
            using ILoggerFactory loggerFactory = BuildLoggerFactory(baseConfig);
            ILogger logger = loggerFactory.CreateLogger("main");
            IMode mode = modeName switch
            {
                "run" => new Runmode(loggerFactory, baseConfig, ReadConfig<RunmodeConfig>(modeConfigFilename)),
                "match" => new Matchmode(loggerFactory, baseConfig, ReadConfig<MatchmodeConfig>(modeConfigFilename)),
                "dualcore" => new DualcoreMode(loggerFactory, baseConfig),
                "dummy" => new DummyMode(loggerFactory, baseConfig),
                _ => throw new NotSupportedException($"Invalid mode '{modeName}'")
            };
            try
            {
                Task modeTask = mode.Run();
                void Abort(object? sender, EventArgs args)
                {
                    logger.LogInformation("Aborting mode...");
                    mode.Cancel();
                    modeTask.Wait();
                }
                AppDomain.CurrentDomain.ProcessExit += Abort; // SIGTERM
                Console.CancelKeyPress += Abort; // CTRL-C / SIGINT
                modeTask.Wait();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "uncaught exception! TPP is crashing now, goodbye.");
            }
        }

        private static void TestConfig(
            string configFilename,
            string? mode,
            string modeConfigFilename)
        {
            // just try to read the configs, don't do anything with them

            if (File.Exists(configFilename))
                ReadConfig<BaseConfig>(configFilename);
            else
                Console.Error.WriteLine(
                    $"missing base config file '{configFilename}', generate one from default values " +
                    $"using 'gendefaultconfig --outfile={configFilename}'");

            if (mode != null && DefaultConfigs[mode] != null)
            {
                if (File.Exists(modeConfigFilename))
                    ReadConfig(modeConfigFilename, DefaultConfigs[mode]!.GetType());
                else
                    Console.Error.WriteLine(
                        $"missing mode config file '{modeConfigFilename}', generate one from default values " +
                        $"using 'gendefaultconfig {mode} --outfile={modeConfigFilename}'");
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
            string baseSchemaText = generator.Generate(typeof(BaseConfig))
                .ToString().Replace("\r\n", "\n");
            File.WriteAllText(baseConfig.Schema, baseSchemaText, Encoding.UTF8);
            Console.Error.WriteLine($"Wrote base json schema to '{baseConfig.Schema}',");

            foreach ((string modeName, IRootConfig? defaultModeConfig) in DefaultConfigs)
            {
                if (defaultModeConfig == null) continue;
                string modeSchemaText = generator.Generate(defaultModeConfig.GetType())
                    .ToString().Replace("\r\n", "\n");
                File.WriteAllText(defaultModeConfig.Schema, modeSchemaText, Encoding.UTF8);
                Console.Error.WriteLine($"Wrote mode '{modeName}' json schema to '{defaultModeConfig.Schema}'");
            }
        }
    }
}
