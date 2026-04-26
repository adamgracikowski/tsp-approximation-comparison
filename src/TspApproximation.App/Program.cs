using System.Diagnostics;

using CommandLine;

using TspApproximation.Core;
using TspApproximation.Core.Algorithms;
using TspApproximation.Core.IO;

namespace TspApproximation.App;

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
            Console.WriteLine($"Loading graph from file: {options.FilePath}");
            var input = TspInputLoader.LoadFromFile(options.FilePath);

            IAlgorithm algorithm = options.UseDoubleTree
                ? new DoubleTree()
                : new Christofides();

            Console.WriteLine($"Number of vertices in the graph: {input.Graph.VertexCount}");
            Console.WriteLine($"Selected algorithm: {algorithm.GetType().Name}");
            Console.WriteLine("Solving...");

            var stopwatch = Stopwatch.StartNew();
            var output = algorithm.Solve(input);
            stopwatch.Stop();

            Console.WriteLine($"Algorithm finished in: {stopwatch.Elapsed.TotalMilliseconds:F2} ms");

            TspOutputWriter.WriteToConsole(output);

            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                Console.WriteLine($"\nSaving result to: {options.OutputPath}");
                TspOutputWriter.WriteToFile(output, options.OutputPath);
                Console.WriteLine("Result saved successfully.");
            }
        }
        catch (NotImplementedException ex)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(ex.Message);
            Console.ResetColor();
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