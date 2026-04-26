using System.Text;

using CommandLine;

namespace TspApproximation.Generator;

public class Program
{
    static void Main(string[] args)
    {
        Parser.Default.ParseArguments<Options>(args)
            .WithParsed(RunOptions)
            .WithNotParsed(HandleParseError);
    }

    static void RunOptions(Options opts)
    {
        if (!Directory.Exists(opts.Dir))
        {
            Directory.CreateDirectory(opts.Dir);
        }

        Console.WriteLine($"Generating {opts.Count} instances starting from {opts.Start} vertices with a step of {opts.Step}...");

        for (int i = 0; i < opts.Count; i++)
        {
            int currentVertices = opts.Start + i * opts.Step;

            string filename = $"gen_n{currentVertices:D4}.txt";
            string filepath = Path.Combine(opts.Dir, filename);

            int[,] matrix = GenerateMetricTspMatrix(currentVertices, opts.Size);

            if (!VerifyTriangleInequality(matrix))
            {
                Console.WriteLine($"Skipping {filename} due to triangle inequality violation.");
                continue;
            }

            SaveToFile(matrix, filepath);
            Console.WriteLine($"Saved instance: {filepath}");
        }

        Console.WriteLine("File generation completed.");
    }

    static void HandleParseError(IEnumerable<Error> errs)
    {
        // CommandLineParser will automatically print the help screen here.
    }

    static int[,] GenerateMetricTspMatrix(int numVertices, int maxCoord)
    {
        var rnd = new Random();
        var points = new (int X, int Y)[numVertices];

        for (var i = 0; i < numVertices; i++)
        {
            points[i] = (rnd.Next(0, maxCoord + 1), rnd.Next(0, maxCoord + 1));
        }

        var matrix = new int[numVertices, numVertices];

        for (var i = 0; i < numVertices; i++)
        {
            for (var j = 0; j < numVertices; j++)
            {
                if (i != j)
                {
                    var dx = points[i].X - points[j].X;
                    var dy = points[i].Y - points[j].Y;
                    var dist = Math.Ceiling(Math.Sqrt(dx * dx + dy * dy));
                    matrix[i, j] = Math.Max(1, (int)dist);
                }
            }
        }

        for (int k = 0; k < numVertices; k++)
        {
            for (int i = 0; i < numVertices; i++)
            {
                for (int j = 0; j < numVertices; j++)
                {
                    if (i != j && j != k && i != k)
                    {
                        if (matrix[i, j] > matrix[i, k] + matrix[k, j])
                        {
                            matrix[i, j] = matrix[i, k] + matrix[k, j];
                        }
                    }
                }
            }
        }

        return matrix;
    }

    static bool VerifyTriangleInequality(int[,] matrix)
    {
        int n = matrix.GetLength(0);
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                for (int k = 0; k < n; k++)
                {
                    if (i != j && j != k && i != k)
                    {
                        if (matrix[i, j] > matrix[i, k] + matrix[k, j])
                        {
                            Console.WriteLine($"ERROR: Triangle inequality violated for vertices {i}, {j}, {k}!");
                            Console.WriteLine($"d({i},{j})={matrix[i, j]} > d({i},{k})={matrix[i, k]} + d({k},{j})={matrix[k, j]}");
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    static void SaveToFile(int[,] matrix, string filepath)
    {
        int n = matrix.GetLength(0);

        using var writer = new StreamWriter(filepath, false, Encoding.UTF8);
        writer.WriteLine(n);
        for (int i = 0; i < n; i++)
        {
            var row = Enumerable.Range(0, n).Select(j => matrix[i, j].ToString());
            writer.WriteLine(string.Join(" ", row));
        }
    }
}