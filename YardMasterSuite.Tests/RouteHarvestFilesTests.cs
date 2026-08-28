using System.IO;
using YardMasterSuite.Core;

namespace YardMasterSuite.Tests;

public class RouteHarvestFilesTests
{
    [Fact]
    public void Smoke_harvest_replace_overwrites_file_not_append()
    {
        var path = Path.Combine(Path.GetTempPath(), "yms-htp-harvest-replace.txt");
        try
        {
            File.WriteAllText(path, "stale-session-pin=990152\n");
            RouteHarvestFiles.Replace(path, "YMS-HARVEST 1\npin 990218\n");
            var text = File.ReadAllText(path);
            Assert.Equal("YMS-HARVEST 1\npin 990218\n", text);
            Assert.DoesNotContain("990152", text);
            Assert.DoesNotContain("stale-session", text);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
