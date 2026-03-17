namespace TspApproximation.Core.IO;

public static class TspOutputWriter
{
    public static void WriteToConsole(TspOutput output)
    {
        Console.WriteLine(FormatOutput(output));
    }

    public static void WriteToFile(TspOutput output, string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        File.WriteAllText(
            filePath,
            FormatOutput(output)
        );
    }

    private static string FormatOutput(TspOutput output)
    {
        return $"Total Distance: {output.TotalDistance}{Environment.NewLine}Route: {string.Join("-", output.Route)}";
    }
}
