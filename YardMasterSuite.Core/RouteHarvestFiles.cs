using System.IO;
using System.Text;

namespace YardMasterSuite.Core;

/// <summary>
/// One-off harvest I/O: always truncate. Last Set dest wins; never append
/// a prior session or a multi-leg first pin onto corridor.txt.
/// </summary>
public static class RouteHarvestFiles
{
    public static void Replace(string path, string text)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (var writer = new StreamWriter(fs, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
        {
            writer.Write(text);
        }
    }
}
