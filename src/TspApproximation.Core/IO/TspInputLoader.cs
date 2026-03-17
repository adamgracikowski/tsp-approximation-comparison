namespace TspApproximation.Core.IO;

public static class TspInputLoader
{
    public static TspInput LoadFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"File not found: {filePath}");

        var lines = File.ReadLines(filePath)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();

        if (lines.Length == 0)
            throw new InvalidDataException("Input file is empty.");

        if (!int.TryParse(lines[0].Trim(), out var vertexCount))
            throw new InvalidDataException("First line must contain the number of vertices.");

        if (lines.Length - 1 < vertexCount)
            throw new InvalidDataException($"Expected {vertexCount} lines of adjacency matrix, but found {lines.Length - 1}.");

        var matrix = new int[vertexCount, vertexCount];

        for (var i = 0; i < vertexCount; i++)
        {
            var rowValues = lines[i + 1].Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

            if (rowValues.Length < vertexCount)
                throw new InvalidDataException($"Line {i + 2} must contain at least {vertexCount} values.");

            for (var j = 0; j < vertexCount; j++)
            {
                if (!int.TryParse(rowValues[j], out var distance))
                    throw new InvalidDataException($"Invalid integer value at line {i + 2}, column {j + 1}.");

                matrix[i, j] = distance;
            }
        }

        var graph = new Graph(matrix);

        return new TspInput(graph);
    }
}