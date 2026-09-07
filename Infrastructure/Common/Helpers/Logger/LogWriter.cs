namespace Infrastructure.Common.Helpers.Logger
{
    public class LogWriter(string exePath, bool working)
    {
        private readonly string _exePath = exePath;
        private readonly bool _working = working;
        /// <summary>
        /// Writer to file logs.
        /// </summary>
        /// <param name="fileName">Name File Example log.txt</param>
        /// <param name="text">Text logs.</param>
        /// <param name="deleteExistFile">If true deleten file and create file write log.</param>
        public void Log(string fileName, string text)
        {
            if (_working)
            {
                try
                {
                    string filePath = GetDailyFilePath(fileName);
                    using StreamWriter sw = File.AppendText(filePath);
                    sw.Write("Log Entry: ");
                    sw.WriteLine("[" + DateTime.Now.ToString() + "] \n" + text);
                    sw.WriteLine("----------------------------------------------");
                }
                catch
                {
                }
            }
        }
        public void EmergencyLog(string fileName, string text)
        {
            if (_working)
            {
                try
                {
                    string filePath = GetDailyFilePath(fileName);
                    using StreamWriter sw = File.AppendText(filePath);
                    sw.Write("Log Entry: ");
                    sw.WriteLine("[" + DateTime.Now.ToString() + "] \n" + text);
                    sw.WriteLine("----------------------------------------------");
                }
                catch
                {
                }
            }
        }

        private string GetDailyFilePath(string fileName)
        {
            string folderPath = Path.Combine(_exePath, "logs", DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(folderPath);
            return Path.Combine(folderPath, fileName);
        }
    }
}
