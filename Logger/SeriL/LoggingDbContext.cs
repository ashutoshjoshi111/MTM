
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using System;
using Serilog.Sinks.MSSqlServer;

namespace Logger
{
    // Define your database context
    public class LoggingDbContext : DbContext
    {
        public DbSet<LogEntry> LogEntries { get; set; }

        public LoggingDbContext(DbContextOptions<LoggingDbContext> options) : base(options)
        {

        }
    }
}
