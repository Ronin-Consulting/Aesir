using System.IO;
using Aesir.Common.Models;

namespace Aesir.Client.Models;

public class AesirDocument:AesirDocumentBase
{
    public string FileNameDisplay => Path.GetFileName(FileName);

    public string FileNameOnly => Path.GetFileName(FileName);

    public string FileSizeDisplay
    {
        get
        {
            return FormatFileSize(FileSize);
        }
    }

    public string CreatedAtDisplay=> CreatedAt.ToString("g");

    public string UpdatedAtDisplay=> UpdatedAt.ToString("g");

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        return $"{len:0.#} {sizes[order]}";
    }

}