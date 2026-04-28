using System.IO;
using BinaryDiffViewer.Models;

namespace BinaryDiffViewer.Services;

public class BinaryCompareService
{
    private const int BlockSize = 65536;       // 64KB per block
    private const int MaxDisplayDiffs = 10_000;

    public async Task<BinaryDiffResult> CompareAsync(
        string filePathA,
        string filePathB,
        IProgress<double>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var sizeA = new FileInfo(filePathA).Length;
        var sizeB = new FileInfo(filePathB).Length;
        var totalBytes = Math.Max(sizeA, sizeB);

        var displayDiffs = new List<BinaryDiffItem>(Math.Min(MaxDisplayDiffs, 1024));
        long totalDiffCount = 0;
        long? firstDiffOffset = null;
        long globalOffset = 0;

        var bufferA = new byte[BlockSize];
        var bufferB = new byte[BlockSize];

        await using var streamA = new FileStream(
            filePathA, FileMode.Open, FileAccess.Read, FileShare.Read,
            BlockSize, FileOptions.Asynchronous | FileOptions.SequentialScan);
        await using var streamB = new FileStream(
            filePathB, FileMode.Open, FileAccess.Read, FileShare.Read,
            BlockSize, FileOptions.Asynchronous | FileOptions.SequentialScan);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var readA = await ReadBlockAsync(streamA, bufferA, cancellationToken).ConfigureAwait(false);
            var readB = await ReadBlockAsync(streamB, bufferB, cancellationToken).ConfigureAwait(false);

            if (readA == 0 && readB == 0) break;

            var maxRead = Math.Max(readA, readB);
            for (int i = 0; i < maxRead; i++)
            {
                var inA = i < readA;
                var inB = i < readB;

                BinaryDiffItem? diff = null;
                if (inA && inB)
                {
                    if (bufferA[i] != bufferB[i])
                        diff = new BinaryDiffItem(globalOffset + i, DiffType.Modified, bufferA[i], bufferB[i]);
                }
                else if (inA)
                {
                    diff = new BinaryDiffItem(globalOffset + i, DiffType.Removed, bufferA[i], null);
                }
                else
                {
                    diff = new BinaryDiffItem(globalOffset + i, DiffType.Added, null, bufferB[i]);
                }

                if (diff is not null)
                {
                    totalDiffCount++;
                    firstDiffOffset ??= diff.Offset;
                    if (displayDiffs.Count < MaxDisplayDiffs)
                        displayDiffs.Add(diff);
                }
            }

            globalOffset += maxRead;

            if (totalBytes > 0)
                progress?.Report((double)globalOffset / totalBytes * 100.0);
        }

        return new BinaryDiffResult(
            IsSameSize: sizeA == sizeB,
            TotalDiffCount: totalDiffCount,
            FirstDiffOffset: firstDiffOffset,
            FileASize: sizeA,
            FileBSize: sizeB,
            DisplayDiffItems: displayDiffs.AsReadOnly()
        );
    }

    private static async ValueTask<int> ReadBlockAsync(
        FileStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        var totalRead = 0;
        while (totalRead < buffer.Length)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(totalRead), cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            totalRead += read;
        }
        return totalRead;
    }
}
