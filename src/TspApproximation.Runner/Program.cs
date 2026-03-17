using System.Diagnostics;
using System.Globalization;

using CommandLine;

using CsvHelper;

using TspApproximation.Core;
using TspApproximation.Core.Algorithms;
using TspApproximation.Core.IO;

namespace TspApproximation.Runner;

internal class Program
{
    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
    }

    static void RunOptions(Options options)
    {
        try
        {
            IAlgorithm algorithm = options.UseDoubleTree
                ? new DoubleTree()
                : new Christofides();

            var algorithmType = options.UseDoubleTree
                ? nameof(DoubleTree)
                : nameof(Christofides);

            Console.WriteLine($"Directory: {options.Directory}");
            Console.WriteLine($"Algorithm: {algorithmType}");
            Console.WriteLine();

            if (!Directory.Exists(options.Directory))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Error: Input directory '{options.Directory}' does not exist.");
                Console.ResetColor();
                return;
            }

            var files = Directory.GetFiles(options.Directory, "*.txt", SearchOption.TopDirectoryOnly);

            var now = DateTime.Now;
            var defaultFileName = $"result_{now:yyyy-MM-dd_HH-mm-ss}.csv";
            var outputPath = options.OutputPath ?? Path.Combine(Directory.GetCurrentDirectory(), defaultFileName);

            Console.WriteLine($"Result will be saved in: {outputPath}");
            Console.WriteLine();

            using var writer = new StreamWriter(outputPath)
            {
                AutoFlush = true
            };
            using var csv = new CsvWriter(writer, culture: CultureInfo.InvariantCulture);

            csv.WriteHeader<CsvResult>();
            csv.NextRecord();

            foreach (var file in files)
            {
                Console.WriteLine($"Loading {file}...");

                var input = TspInputLoader.LoadFromFile(file);

                Console.WriteLine($"Running {file}...");

                var stopwatch = Stopwatch.StartNew();
                var output = algorithm.Solve(input);
                stopwatch.Stop();

                var csvResult = new CsvResult
                {
                    FileName = Path.GetFileName(file),
                    Algorithm = algorithmType,
                    VertexCount = input.Graph.VertexCount,
                    TotalDistance = output.TotalDistance,
                    RouteLength = output.Route.Count,
                    Route = string.Join("-", output.Route),
                    TotalElapsedMiliseconds = stopwatch.Elapsed.TotalMilliseconds
                };

                csv.WriteRecord(csvResult);
                csv.NextRecord();

                Console.WriteLine($"Saved {file} (Time: {stopwatch.Elapsed.TotalMilliseconds:F2} ms)");
                Console.WriteLine();
            }
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nAn error occurred: {ex.Message}");
            Console.ResetColor();
        }
    }
    static void HandleParseError(IEnumerable<Error> errs)
    {
        // CommandLineParser will automatically print the help screen here.
    }
}