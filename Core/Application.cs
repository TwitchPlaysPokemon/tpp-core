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
    internal static class Application
    {
        private const string Usage = @"
Usage:
  core run [--config=<file>]
  core testconfig [--config=<file>]
  core gendefaultconfig [--outfile=<file>]
  core regenjsonschema
  core -h | --help

Options:
  -h --help         Show this screen.
  --config=<file>   Specifies the config file to use. [default: config.json]
  --outfile=<file>  Specifies the file to output to. Default is printing to stdout.
";

        private static void Main(string[] args)
        {
            using var loggerFactory = LoggerFactory.Create(builder => builder
                .AddFile("logs/tpp-{Date}.log")
                .AddConsole());
            ILogger logger = loggerFactory.CreateLogger("main");
            IDictionary<string, ValueObject> arguments = new Docopt().Apply(Usage, args, exit: true);
            if (arguments["run"].IsTrue) Run(loggerFactory, logger, arguments["--config"].ToString());
            else if (arguments["testconfig"].IsTrue) TestConfig(logger, arguments["--config"].ToString());
            else if (arguments["gendefaultconfig"].IsTrue) OutputDefaultConfig(arguments["--outfile"]);
            else if (arguments["regenjsonschema"].IsTrue) RegenerateJsonSchema();
        }

        private static readonly JsonSerializerSettings ConfigSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new PrivateSetterContractResolver(),
            Converters = {new StringEnumConverter()},
            Formatting = Formatting.Indented,
            // if we don't force objects to be fully replaced, it might e.g. append to default collections,
            // making it impossible to remove elements from said collections via configuration.
            ObjectCreationHandling = ObjectCreationHandling.Replace,
        };

        private static RootConfig ReadConfig(ILogger logger, string filename)
        {
            string json = File.ReadAllText(filename);
            var config = JsonConvert.DeserializeObject<RootConfig>(json, ConfigSerializerSettings);
            if (config == null) throw new ArgumentException("config must not be null");
            ConfigUtils.LogUnrecognizedConfigs(logger, config);
            return config;
        }

        private static void Run(ILoggerFactory loggerFactory, ILogger logger, string configFilename)
        {
            RootConfig config = ReadConfig(logger, configFilename);
            var tpp = new Tpp(loggerFactory, config);
            try
            {
                tpp.Run().Wait();
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "uncaught exception! TPP is crashing now, goodbye.");
            }
        }

        private static void TestConfig(ILogger logger, string configFilename)
        {
            // just try to read the config, don't do anything with it
            ReadConfig(logger, configFilename);
        }

        private static void OutputDefaultConfig(ValueObject? outfileArgument)
        {
            string json = JsonConvert.SerializeObject(new RootConfig(), ConfigSerializerSettings);
            json = "/* DO NOT SHARE THIS FILE --- IT MAY CONTAIN SECRETS! */\n" +
                   "/* You can omit entries to set them to their default value. */\n" +
                   json + "\n";
            if (outfileArgument == null || outfileArgument.IsNullOrEmpty) Console.Write(json);
            else File.WriteAllText(outfileArgument.ToString(), json, Encoding.UTF8);
        }

        private static void RegenerateJsonSchema()
        {
            var generator = new JSchemaGenerator();
            generator.GenerationProviders.Add(new StringEnumGenerationProvider());
            generator.DefaultRequired = Required.Default;
            generator.ContractResolver = new PrivateSetterContractResolver();
            string schemaText = generator.Generate(typeof(RootConfig)).ToString();
            File.WriteAllText(RootConfig.Schema, schemaText, Encoding.UTF8);
            Console.Error.WriteLine($"Wrote json schema to '{RootConfig.Schema}'");
        }
    }
}
