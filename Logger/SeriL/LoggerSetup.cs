using Serilog;
using Serilog.Events;
using Serilog.Sinks;
using System;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Logger
{
    public static class LoggerSetup
    {
        private static bool _isConfigured = false;

        [Obsolete]
        public static void ConfigureLogger(string connectionString)
        {
            
            if (!_isConfigured)
            {
                Log.Logger = new LoggerConfiguration()
                    .MinimumLevel.Debug()
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                    .Enrich.FromLogContext()
                    .WriteTo.Console()
                    .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day)
                    .WriteTo.MSSqlServer(
                        connectionString: connectionString,
                        tableName: "LogEntries",
                        columnOptions: new Serilog.Sinks.MSSqlServer.ColumnOptions(),
                        autoCreateSqlTable: true)
                    .CreateLogger();
               
                _isConfigured = true;
            }
        }
    }
}
