using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Core.Configuration;
using DocoptNet;
using JsonNet.ContractResolvers;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema.Generation;

namespace Core
{
    internal static class Program
    {
        private const string Usage = @"
Usage:
  core runmode [--config=<file>] [--runmode-config=<file>]
  core matchmode [--config=<file>] [--matchmode-config=<file>]
  core testconfigs [--config=<file>] [--runmode-config=<file>] [--matchmode-config=<file>]
  core gendefaultconfig [--mode=(run|match)] [--outfile=<file>]
  core regenjsonschemas
  core -h | --help

Options:
  -h --help                  Show this screen.
  --config=<file>            Specifies the base config file for all modes. [default: config.json]
  --runmode-config=<file>    Specifies the runmode-specific config file. [default: config.runmode.json]
  --matchmode-config=<file>  Specifies the matchmode-specific config file. [default: config.matchmode.json]
  --outfile=<file>           Specifies the file to output to. Default is printing to stdout.
";

        private static void Main(string[] argv)
        {
            IDictionary<string, ValueObject> args = new Docopt().Apply(Usage, argv, exit: true);
            if (args["runmode"].IsTrue) Runmode(args["--config"].ToString(), args["--runmode-config"].ToString());
            if (args["matchmode"].IsTrue) Matchmode(args["--config"].ToString(), args["--matchmode-config"].ToString());
            else if (args["testconfigs"].IsTrue)
                TestConfigs(args["--config"].ToString(), args["--runmode-config"].ToString(),
                    args["--matchmode-config"].ToString());
            else if (args["gendefaultconfig"].IsTrue) OutputDefaultConfig(args["--mode"], args["--outfile"]);
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
            string json = File.ReadAllText(filename);
            var config = JsonConvert.DeserializeObject<T>(json, ConfigSerializerSettings);
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
            });

        private static void Runmode(string baseConfigFilename, string runmodeConfigFilename)
        {
            BaseConfig baseConfig = ReadConfig<BaseConfig>(baseConfigFilename);
            RunmodeConfig runmodeConfig = ReadConfig<RunmodeConfig>(runmodeConfigFilename);
            using var loggerFactory = BuildLoggerFactory(baseConfig);
            ILogger logger = loggerFactory.CreateLogger("main");
            using var runmode = new Runmode(loggerFactory, baseConfig, runmodeConfig);
            try
            {
                runmode.Run().Wait();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "uncaught exception! TPP is crashing now, goodbye.");
            }
        }

        private static void Matchmode(string baseConfigFilename, string matchmodeConfigFilename)
        {
            BaseConfig baseConfig = ReadConfig<BaseConfig>(baseConfigFilename);
            MatchmodeConfig matchmodeConfig = ReadConfig<MatchmodeConfig>(matchmodeConfigFilename);
            using var loggerFactory = BuildLoggerFactory(baseConfig);
            ILogger logger = loggerFactory.CreateLogger("main");
            var matchmode = new Matchmode(loggerFactory, baseConfig, matchmodeConfig);
            try
            {
                matchmode.Run().Wait();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "uncaught exception! TPP is crashing now, goodbye.");
            }
        }

        private static void TestConfigs(
            string configFilename,
            string runmodeConfigFilename,
            string matchmodeConfigFilename)
        {
            // just try to read the configs, don't do anything with them

            if (File.Exists(configFilename))
                ReadConfig<BaseConfig>(configFilename);
            else
                Console.Error.WriteLine(
                    $"missing base config file '{configFilename}', generate one from default values " +
                    $"using 'gendefaultconfig --outfile={configFilename}'");

            if (File.Exists(runmodeConfigFilename))
                ReadConfig<RunmodeConfig>(runmodeConfigFilename);
            else
                Console.Error.WriteLine(
                    $"missing runmode config file '{runmodeConfigFilename}', generate one from default values " +
                    $"using 'gendefaultconfig --mode=run --outfile={runmodeConfigFilename}'");

            if (File.Exists(matchmodeConfigFilename))
                ReadConfig<MatchmodeConfig>(matchmodeConfigFilename);
            else
                Console.Error.WriteLine(
                    $"missing matchmode config file '{matchmodeConfigFilename}', generate one from default values " +
                    $"using 'gendefaultconfig --mode=match --outfile={matchmodeConfigFilename}'");
        }

        private static void OutputDefaultConfig(ValueObject? mode, ValueObject? outfileArgument)
        {
            ConfigBase config = mode?.Value switch
            {
                null => new BaseConfig(),
                "run" => new RunmodeConfig(),
                "match" => new MatchmodeConfig(),
                _ => throw new NotSupportedException($"Invalid mode '{mode}'")
            };
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
            string baseSchemaText = generator.Generate(typeof(BaseConfig)).ToString();
            string runmodeSchemaText = generator.Generate(typeof(RunmodeConfig)).ToString();
            string matchmodeSchemaText = generator.Generate(typeof(MatchmodeConfig)).ToString();
            File.WriteAllText(BaseConfig.Schema, baseSchemaText, Encoding.UTF8);
            Console.Error.WriteLine($"Wrote base json schemas to '{BaseConfig.Schema}',");
            File.WriteAllText(RunmodeConfig.Schema, runmodeSchemaText, Encoding.UTF8);
            Console.Error.WriteLine($"Wrote runmode json schemas to '{RunmodeConfig.Schema}'");
            File.WriteAllText(MatchmodeConfig.Schema, matchmodeSchemaText, Encoding.UTF8);
            Console.Error.WriteLine($"Wrote matchmode json schemas to '{MatchmodeConfig.Schema}'");
        }
    }
}
