using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Askaiser.Puppets.LibraryGenerator
{
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                UnsafeMain(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return 1;
            }
        }

        private static void UnsafeMain(IEnumerable<string> args)
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
                NamespaceName = nonEmptyArgs[1],
                ClassName = "RootLibrary"
            };

            var outputFile = new FileInfo(outputFilePath);

            var result = LibraryCodeGenerator.Generate(options);

            foreach (var warning in result.Warnings)
                Console.WriteLine(warning);

            File.WriteAllText(outputFile.FullName, result.Code);
        }
    }
}
