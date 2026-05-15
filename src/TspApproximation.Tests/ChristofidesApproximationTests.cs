using FluentAssertions;

using TspApproximation.Core.Algorithms;
using TspApproximation.Core.IO;

namespace TspApproximation.Tests;

public sealed class ChristofidesApproximationTests
{
    private static readonly string TsplibPath = FindTsplibDirectory();

    public static TheoryData<string, int> Instances
    {
        get
        {
            var data = new TheoryData<string, int>();
            foreach (var path in Directory.EnumerateFiles(TsplibPath, "n*.txt").OrderBy(p => p))
            {
                var name = Path.GetFileNameWithoutExtension(path);
                var opt = int.Parse(name[(name.IndexOf('o') + 1)..]);
                data.Add(Path.GetFileName(path), opt);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(Instances))]
    public void Christofides_IsWithin1_5xOptimal(string fileName, int opt)
    {
        var filePath = Path.Combine(TsplibPath, fileName);
        var input = TspInputLoader.LoadFromFile(filePath);

        var result = new Christofides().Solve(input);

        result.TotalDistance.Should().BeLessThanOrEqualTo(
            (int)Math.Ceiling(1.5 * opt),
            because: $"{fileName}: expected at most 1.5x{opt}={1.5 * opt:F0}, got {result.TotalDistance}");
    }

    [Theory]
    [MemberData(nameof(Instances))]
    public void Christofides_RouteVisitsEveryVertexExactlyOnce(string fileName, int _)
    {
        var filePath = Path.Combine(TsplibPath, fileName);
        var input = TspInputLoader.LoadFromFile(filePath);
        var n = input.Graph.VertexCount;

        var result = new Christofides().Solve(input);

        result.Route.Should()
            .HaveCount(n + 1, because: "route must contain n vertices plus the closing return to start");
        result.Route[0].Should().Be(result.Route[^1], because: "route must be a cycle (first == last)");
        result.Route.Take(n).Should().OnlyHaveUniqueItems(because: "every vertex must be visited exactly once");
        result.Route.Take(n).Should()
            .BeEquivalentTo(Enumerable.Range(0, n), because: "every vertex index must appear in the route");
    }

    [Theory]
    [MemberData(nameof(Instances))]
    public void Christofides_ReportedDistanceMatchesActualRouteWeight(string fileName, int _)
    {
        var filePath = Path.Combine(TsplibPath, fileName);
        var input = TspInputLoader.LoadFromFile(filePath);

        var result = new Christofides().Solve(input);

        var actual = 0;
        for (int i = 0; i < result.Route.Count - 1; i++)
        {
            actual += input.Graph[result.Route[i], result.Route[i + 1]];
        }

        result.TotalDistance.Should().Be(actual,
            because: "reported distance must equal the sum of edge weights along the route");
    }

    private const string ExamplesDirectoryName = "examples";
    private const string TsplibExamplesSubdirectoryName = "tsplib";

    private static string FindTsplibDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, ExamplesDirectoryName)))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException(
                $"Could not locate the '{ExamplesDirectoryName}' directory by walking up from the test binary.");
        }

        return Path.Combine(dir.FullName, ExamplesDirectoryName, TsplibExamplesSubdirectoryName);
    }
}