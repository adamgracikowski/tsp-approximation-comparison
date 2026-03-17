using CommandLine;

namespace TspApproximation.App;

public class Options
{
    [Value(0, MetaName = "FilePath", Required = true, HelpText = "Path to the text file containing the graph's adjacency matrix.")]
    public string FilePath { get; set; } = string.Empty;

    [Option('o', "output", Required = false, HelpText = "Path to the output file where the result will be saved.")]
    public string? OutputPath { get; set; }

    [Option('d', "double-tree", Required = false, HelpText = "Forces the use of the Double Tree algorithm instead of the default Christofides.")]
    public bool UseDoubleTree { get; set; }
}
