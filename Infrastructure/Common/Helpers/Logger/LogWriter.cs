namespace Infrastructure.Common.Helpers.Logger
{
    public class LogWriter
    {
        private readonly string _preferredRoot;
        private readonly bool _working;
        private string? _resolvedRoot;
        private readonly object _rootLock = new();

        public LogWriter(string exePath, bool working)
        {
            _preferredRoot = string.IsNullOrWhiteSpace(exePath)
                ? AppContext.BaseDirectory
                : exePath;
            _working = working;
        }

        /// <summary>Root folder that currently receives log files (exe dir or ProgramData fallback).</summary>
        public string ResolvedRoot => EnsureRoot();

        public void Log(string fileName, string text) => Write(fileName, text);

        public void EmergencyLog(string fileName, string text) => Write(fileName, text);

        private void Write(string fileName, string text)
        {
            if (!_working)
            {
                return;
            }

            try
            {
                var filePath = GetDailyFilePath(fileName);
                using var sw = File.AppendText(filePath);
                sw.Write("Log Entry: ");
                sw.WriteLine("[" + DateTime.Now + "] \n" + text);
                sw.WriteLine("----------------------------------------------");
            }
            catch
            {
                // Last resort: do not throw from logging.
                try
                {
                    _resolvedRoot = null;
                    var filePath = GetDailyFilePath(fileName);
                    using var sw = File.AppendText(filePath);
                    sw.WriteLine("[" + DateTime.Now + "] " + text);
                }
                catch
                {
                    // ignored
                }
            }
        }

        private string GetDailyFilePath(string fileName)
        {
            var folderPath = Path.Combine(EnsureRoot(), "logs", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(folderPath);
            return Path.Combine(folderPath, fileName);
        }

        private string EnsureRoot()
        {
            if (_resolvedRoot is not null)
            {
                return _resolvedRoot;
            }

            lock (_rootLock)
            {
                if (_resolvedRoot is not null)
                {
                    return _resolvedRoot;
                }

                foreach (var candidate in GetRootCandidates())
                {
                    try
                    {
                        var probe = Path.Combine(candidate, "logs");
                        Directory.CreateDirectory(probe);
                        // Verify write access (service account may not write under Program Files).
                        var probeFile = Path.Combine(probe, ".write-probe");
                        File.WriteAllText(probeFile, "ok");
                        File.Delete(probeFile);
                        _resolvedRoot = candidate;
                        return _resolvedRoot;
                    }
                    catch
                    {
                        // try next candidate
                    }
                }

                _resolvedRoot = Path.GetTempPath();
                return _resolvedRoot;
            }
        }

        private IEnumerable<string> GetRootCandidates()
        {
            yield return _preferredRoot;
            yield return AppContext.BaseDirectory;
            yield return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                "KATM_Online");
            yield return Path.Combine(Path.GetTempPath(), "KATM_Online");
        }
    }
}
