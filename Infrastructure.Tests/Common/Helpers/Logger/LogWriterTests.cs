using Infrastructure.Common.Helpers.Logger;

namespace Infrastructure.Tests.Common.Helpers.Logger;

public class LogWriterTests
{
    [Fact]
    public void Log_WhenPreferredRootNotWritable_FallsBackAndStillWrites()
    {
        var forbidden = Path.Combine(Path.GetTempPath(), "katm-forbidden-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(forbidden);
        try
        {
            // Make directory non-writable by removing write ACL is OS-specific;
            // instead pass a path under a file (invalid as directory parent target).
            var notADirectory = Path.Combine(forbidden, "not-a-dir");
            File.WriteAllText(notADirectory, "x");

            var sut = new LogWriter(notADirectory, working: true);
            sut.Log("probe.txt", "hello-from-service-logger");

            var expected = Path.Combine(sut.ResolvedRoot, "logs", DateTime.Now.ToString("yyyy-MM-dd"), "probe.txt");
            Assert.True(File.Exists(expected), $"Expected log at {expected}");
            Assert.Contains("hello-from-service-logger", File.ReadAllText(expected));
        }
        finally
        {
            try { Directory.Delete(forbidden, recursive: true); } catch { /* ignore */ }
        }
    }

    [Fact]
    public void Log_WhenWorkingFalse_DoesNotCreateFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), "katm-nolog-" + Guid.NewGuid().ToString("N"));
        var sut = new LogWriter(root, working: false);
        sut.Log("should-not-exist.txt", "nope");
        Assert.False(Directory.Exists(Path.Combine(root, "logs")));
    }
}
