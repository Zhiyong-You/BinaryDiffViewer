using System.IO;
using BinaryDiffViewer.Models;

namespace BinaryDiffViewer.Services;

public class BinaryCompareService
{
    private const int MaxCompareBytes = 10 * 1024 * 1024; // 10MB

    public BinaryDiffResult Compare(string filePathA, string filePathB)
    {
        var bytesA = ReadBytes(filePathA);
        var bytesB = ReadBytes(filePathB);
        var sameSize = new FileInfo(filePathA).Length == new FileInfo(filePathB).Length;

        var diffs = new List<BinaryDiffItem>();
        var maxLen = Math.Max(bytesA.Length, bytesB.Length);

        for (var i = 0; i < maxLen; i++)
        {
            var inA = i < bytesA.Length;
            var inB = i < bytesB.Length;

            if (inA && inB)
            {
                if (bytesA[i] != bytesB[i])
                    diffs.Add(new BinaryDiffItem(i, DiffType.Modified, bytesA[i], bytesB[i]));
            }
            else if (inA)
            {
                diffs.Add(new BinaryDiffItem(i, DiffType.Removed, bytesA[i], null));
            }
            else
            {
                diffs.Add(new BinaryDiffItem(i, DiffType.Added, null, bytesB[i]));
            }
        }

        return new BinaryDiffResult(
            SameSize: sameSize,
            TotalDiffCount: diffs.Count,
            FirstDiffOffset: diffs.Count > 0 ? diffs[0].Offset : null,
            Diffs: diffs.AsReadOnly()
        );
    }

    private static byte[] ReadBytes(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var bytesToRead = (int)Math.Min(stream.Length, MaxCompareBytes);
        var buffer = new byte[bytesToRead];
        var totalRead = 0;
        while (totalRead < bytesToRead)
        {
            var read = stream.Read(buffer, totalRead, bytesToRead - totalRead);
            if (read == 0) break;
            totalRead += read;
        }
        return buffer;
    }
}
