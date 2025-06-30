using Azure.Bicep.Types.Concrete;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Generator;
using Gabrielngbtuc.Azure.Bicep.Keyvault.Shared.Models;
using CommandLine;

namespace Gabrielngbtuc.Azure.Bicep.Keyvault.CLI
{
    public static class Program
    {
        internal class Options
        {
            [Option("outdir", Required = true, HelpText = "The path to output types to")]
            public string? Outdir { get; set; }
        }

        public static void Main(string[] args)
            => Parser.Default.ParseArguments<Options>(args)
                .WithParsed<Options>(Main);

        private static void Main(Options options)
        {
            var types = TypeGenerator.GenerateTypes("az-keyvault-tuc-ext", "1.0.0", typeof(Configuration), typeof(Configuration));
            // DiscriminatedType example
            //var types = TypeGenerator.GenerateTypes("test", "1.0.0", typeof(ConfigurationBase),typeof(Class1));


            var outdir = Path.GetFullPath(options.Outdir!);

            Directory.CreateDirectory(outdir);
            foreach (var kvp in types)
            {
                var filePath = Path.Combine(outdir, kvp.Key);
                Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

                File.WriteAllText(filePath, kvp.Value);
            }
        }
    }
}