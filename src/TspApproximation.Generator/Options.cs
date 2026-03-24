using CommandLine;

namespace TspApproximation.Generator;

public class Options
{
    [Option("start", Required = true, HelpText = "Starting number of vertices")]
    public int Start
    {
        get; set;
    }

    [Option("step", Default = 1, HelpText = "Step size for increasing vertices (default: 1)")]
    public int Step
    {
        get; set;
    }

    [Option('c', "count", Default = 1, HelpText = "Number of files to generate (default: 1)")]
    public int Count
    {
        get; set;
    }

    [Option('s', "size", Default = 1000, HelpText = "Maximum coordinate value (default: 1000)")]
    public int Size
    {
        get; set;
    }

    [Option('d', "dir", Default = "data", HelpText = "Target directory for files (default: 'data')")]
    public string Dir { get; set; } = string.Empty;
}