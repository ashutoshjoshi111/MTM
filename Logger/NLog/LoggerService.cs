using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace Logger.NLog
{
    public class LoggerService
    {

        private static readonly LoggerService Nlogger = new LoggerService();
        public static LoggerService nLoggerService => Nlogger;
        private readonly ILogger _logger;

        public LoggerService()
        {
            LogManager.LoadConfiguration("nlog.config");
            _logger = LogManager.GetCurrentClassLogger();
        }

        public static LoggerService GetInstance()
        {
            return Nlogger;
        }


        public void LogInfo(string message, string KeyId = null, int Step = 0)
        {
            SetCustomColumns(KeyId, Step);
            _logger.Info(message);
        }

        public void LogWarn(string message, string KeyId = null, int Step = 0)
        {
            SetCustomColumns(KeyId, Step);
            _logger.Warn(message);
        }

        public void LogError(string message, string KeyId = null, int Step = 0)
        {
            SetCustomColumns(KeyId, Step);
            _logger.Error(message);
        }

        public void LogException(string message, System.Exception ex, string KeyId = null, int Step = 0)
        {
            SetCustomColumns(KeyId, Step);
            _logger.Error(ex, message);
        }

        [Obsolete]
        private void SetCustomColumns(string KeyId, int Step)
        {
            if (!string.IsNullOrEmpty(KeyId))
                MappedDiagnosticsLogicalContext.Set("KeyId", KeyId);
            if (Step != 0)
                MappedDiagnosticsLogicalContext.Set("Step", Step);
        }


    }
}
