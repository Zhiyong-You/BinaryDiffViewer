using System.IO;
using System.Text;

namespace BinaryDiffViewer.Services;

public class BinaryFileService
{
    private const int MaxReadBytes = 1024 * 1024; // 1MB
    private const int BytesPerLine = 16;

    public byte[] ReadBytes(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        var bytesToRead = (int)Math.Min(stream.Length, MaxReadBytes);
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

    public string GenerateHexView(byte[] data)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < data.Length; i += BytesPerLine)
        {
            sb.Append($"{i:X8}  ");
            int lineEnd = Math.Min(i + BytesPerLine, data.Length);
            for (int j = i; j < lineEnd; j++)
            {
                sb.Append($"{data[j]:X2}");
                if (j < lineEnd - 1) sb.Append(' ');
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    public static string FormatFileSize(long bytes)
    {
        if (bytes < 1024) return $"{bytes} bytes";
        if (bytes < 1024 * 1024) return $"{bytes / 1024.0:F1} KB";
        if (bytes < 1024L * 1024 * 1024) return $"{bytes / (1024.0 * 1024):F1} MB";
        return $"{bytes / (1024.0 * 1024 * 1024):F1} GB";
    }
}
