using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Askaiser.UITesting.LibraryGenerator
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            try
            {
                await UnsafeMain(args).ConfigureAwait(false);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static async Task UnsafeMain(IEnumerable<string> args)
        {
            var nonEmptyArgs = args.Where(x => (x?.Trim() ?? string.Empty).Length > 0).ToArray();
            if (nonEmptyArgs.Length != 3)
            {
                Console.WriteLine("You must provide two arguments in this order:");
                Console.WriteLine(" 1. The directory path where your images are stored,");
                Console.WriteLine(" 2. The C# namespace of the generated C# code,");
                Console.WriteLine(" 3. The path of the generated C# file.");
                return;
            }

            var intputDirPath = nonEmptyArgs[0];
            var outputFilePath = nonEmptyArgs[2];

            var directory = new DirectoryInfo(intputDirPath);
            if (!directory.Exists)
            {
                Console.WriteLine($"The directory '{directory.FullName}' does not exists.");
                return;
            }

            var options = new LibraryCodeGeneratorOptions
            {
                ImageDirectoryPath = nonEmptyArgs[0],
                NamespaceName = nonEmptyArgs[1]
            };

            var outputFile = new FileInfo(outputFilePath);

            var result = await LibraryCodeGenerator.Generate(options).ConfigureAwait(false);

            foreach (var warning in result.Warnings)
                Console.WriteLine(warning);

            await File.WriteAllTextAsync(outputFile.FullName, result.Code).ConfigureAwait(false);
        }
    }
}