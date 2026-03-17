namespace TspApproximation.Core;

public sealed record TspInput(
    Graph Graph
);

public sealed record TspOutput(
    IReadOnlyList<int> Route, 
    int TotalDistance
);

public interface IAlgorithm
{
    TspOutput Solve(TspInput input);
}