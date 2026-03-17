using CommandLine;

namespace TspApproximation.Runner;

public sealed class Options
{
    [Value(0, MetaName = "Directory", Required = true, HelpText = "Path to the input directory.")]
    public string Directory { get; set; } = string.Empty;

    [Option('o', "output", Required = false, HelpText = "Path to the output CSV file. If omitted, the result will be saved in the application directory.")]
    public string? OutputPath
    {
        get; set;
    }

    [Option('d', "double-tree", Required = false, HelpText = "Forces the use of the Double Tree algorithm instead of the default Christofides.")]
    public bool UseDoubleTree
    {
        get; set;
    }
}