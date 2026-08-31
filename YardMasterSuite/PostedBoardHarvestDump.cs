using System;
using System.IO;
using YardMasterSuite.Core;

namespace YardMasterSuite
{
    /// <summary>
    /// One-shot boards+path harvest beside graph/corridor dumps (9.1.2 Win 2).
    /// </summary>
    internal static class PostedBoardHarvestDump
    {
        internal static Action<string>? EmitLog;

        internal static string? Write(
            string? origin,
            float noseX,
            float noseZ,
            float fwdX,
            float fwdZ,
            PathSegmentAlong[] segments,
            int segmentCount,
            ParsedPostedBoard[] boards,
            int boardCount)
        {
            var text = PostedBoardHarvestCodec.Format(
                origin,
                noseX,
                noseZ,
                fwdX,
                fwdZ,
                segments,
                segmentCount,
                boards,
                boardCount);
            var stamp = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var name = "boards-" + (string.IsNullOrEmpty(origin) ? "path" : origin!.ToLowerInvariant())
                + "-" + stamp + ".txt";
            return WriteFile(name, text, "T2 harvest: boards pathN=" + segmentCount
                + " boardN=" + boardCount);
        }

        private static string? WriteFile(string fileName, string text, string logLine)
        {
            try
            {
                var dir = RouteHarvestDump.DirectoryPath;
                Directory.CreateDirectory(dir);
                var path = Path.Combine(dir, fileName);
                RouteHarvestFiles.Replace(path, text);
                EmitLog?.Invoke(logLine + " file=" + path);
                return path;
            }
            catch (Exception ex)
            {
                EmitLog?.Invoke("T2 harvest: boards write " + ex.GetType().Name);
                return null;
            }
        }
    }
}
