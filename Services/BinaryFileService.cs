using System.IO;

namespace BinaryDiffViewer.Services;

public class BinaryFileService
{
    private const int MaxReadBytes = 1024 * 1024; // 1MB

    public byte[] ReadBytes(string filePath)
        => ReadBytes(filePath, 0, MaxReadBytes);

    public byte[] ReadBytes(string filePath, long startOffset, int maxBytes)
    {
        using var stream = File.OpenRead(filePath);

        if (stream.Length == 0 || maxBytes <= 0)
            return [];

        var clampedStartOffset = Math.Max(startOffset, 0);
        if (clampedStartOffset >= stream.Length)
            return [];

        stream.Seek(clampedStartOffset, SeekOrigin.Begin);

        var bytesToRead = (int)Math.Min(stream.Length - clampedStartOffset, maxBytes);
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

    public long GetFileSize(string filePath) => new FileInfo(filePath).Length;

    public string GenerateHexView(byte[] data) => HexFormatter.GeneratePlainHexView(data);

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} bytes";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}
