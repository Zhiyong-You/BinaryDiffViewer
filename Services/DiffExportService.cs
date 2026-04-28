using System.IO;
using System.Text;
using BinaryDiffViewer.Models;

namespace BinaryDiffViewer.Services;

public class DiffExportService
{
    public async Task ExportToCsvAsync(string filePath, IReadOnlyList<BinaryDiffItem> diffs)
    {
        await using var writer = new StreamWriter(filePath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

        await writer.WriteLineAsync("Offset,FileA,FileB,DiffType");

        foreach (var diff in diffs)
        {
            var offset = $"0x{diff.Offset:X8}";
            var fileA = diff.ByteA.HasValue ? $"{diff.ByteA.Value:X2}" : string.Empty;
            var fileB = diff.ByteB.HasValue ? $"{diff.ByteB.Value:X2}" : string.Empty;
            await writer.WriteLineAsync($"{offset},{fileA},{fileB},{diff.Type}");
        }
    }
}
