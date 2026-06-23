namespace BackendApi.NHibernate
{
    public static class AppLogger
    {
        private static readonly string LogFolder = "logs";

        private static readonly string InfoLogFile =
            Path.Combine(LogFolder, "Util.log");

        private static readonly string ErrorLogFile =
            Path.Combine(LogFolder, "nhibernate-error.log");

        static AppLogger()
        {
            Directory.CreateDirectory(LogFolder);
            
            // Clear old log files on startup
            if (File.Exists(InfoLogFile))
                File.Delete(InfoLogFile);
            if (File.Exists(ErrorLogFile))
                File.Delete(ErrorLogFile);
        }

        public static void Info(string message)
        {
            var logMessage =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO: {message}\n";

            File.AppendAllText(InfoLogFile, logMessage);
        }

        public static void Sql(string sql)
        {
            var logMessage =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] SQL:\n{sql}\n";

            File.AppendAllText(InfoLogFile, logMessage);
        }

        public static void Error(Exception ex)
        {
            var logMessage =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR:\n{ex}\n";

            File.AppendAllText(ErrorLogFile, logMessage);
        }
    }
}