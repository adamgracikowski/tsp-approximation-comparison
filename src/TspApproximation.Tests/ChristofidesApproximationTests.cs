using FluentAssertions;

using TspApproximation.Core;
using TspApproximation.Core.Algorithms;
using TspApproximation.Core.IO;

namespace TspApproximation.Tests;

public class ChristofidesApproximationTests
{
    private static readonly string TsplibPath = FindTsplibDirectory();

    // All tsplib instances with n <= 200 — large enough to exercise blossoms, fast enough for CI.
    public static IEnumerable<object[]> Instances =>
    [
        ["n51o426.tsp", 426],
        ["n52o7542.tsp", 7542],
        ["n70o675.tsp", 675],
        ["n76o108159.tsp", 108159],
        ["n76o538.tsp", 538],
        ["n99o1211.tsp", 1211],
        ["n100o7910.tsp", 7910],
        ["n100o20749.tsp", 20749],
        ["n100o21282.tsp", 21282],
        ["n100o21294.tsp", 21294],
        ["n100o22068.tsp", 22068],
        ["n100o22141.tsp", 22141],
        ["n101o629.tsp", 629],
        ["n105o14379.tsp", 14379],
        ["n107o44303.tsp", 44303],
        ["n124o59030.tsp", 59030],
        ["n130o6110.tsp", 6110],
        ["n136o96772.tsp", 96772],
        ["n144o58537.tsp", 58537],
        ["n150o6528.tsp", 6528],
        ["n150o26130.tsp", 26130],
        ["n150o26524.tsp", 26524],
        ["n152o73682.tsp", 73682],
        ["n159o42080.tsp", 42080],
        ["n195o2323.tsp", 2323],
        ["n198o15780.tsp", 15780],
        ["n200o29368.tsp", 29368],
        ["n200o29437.tsp", 29437],
    ];

    [Theory]
    [MemberData(nameof(Instances))]
    public void Christofides_IsWithin1_5xOptimal(string fileName, int opt)
    {
        var filePath = Path.Combine(TsplibPath, fileName);
        var input = TspInputLoader.LoadFromFile(filePath);

        var result = new Christofides().Solve(input);

        result.TotalDistance.Should().BeLessThanOrEqualTo(
            (int)Math.Ceiling(1.5 * opt),
            because: $"{fileName}: expected at most 1.5×{opt}={1.5 * opt:F0}, got {result.TotalDistance}");
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

    private static string FindTsplibDirectory()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "examples")))
        {
            dir = dir.Parent;
        }

        if (dir == null)
        {
            throw new DirectoryNotFoundException(
                "Could not locate the 'examples' directory by walking up from the test binary.");
        }

        return Path.Combine(dir.FullName, "examples", "tsplib");
    }
}