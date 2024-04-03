using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Principal; 

namespace Logger
{
    public partial class Logger
    {
        #region Enums
        /// <summary>
        /// Defines the logging state(Enabled or Disable) for the current session
        /// </summary>
        internal enum LoggingState
        {
            /// <summary>
            /// Enable logging functionality
            /// </summary>
            Enabled,
            /// <summary>
            /// Disable logging functionality
            /// </summary>
            Disable
        }

        #endregion

        #region "Public Attributes"
        public ApplicationType Application = ApplicationType.Unassigned;
        public string ErrorDescription = "";
        #endregion

        #region "Private Attributes"

        /// <summary>
        /// Defines the default Append-to-file status
        /// </summary>
        private bool _AppendToFile = true;
        private string _className;

        /// <summary>
        /// Defines the default log-file-name if no one is provided
        /// </summary>
        private string _LogFileName = "VoiceProcessing.txt";

        public string LogFileName
        {
            get { return _LogFileName; }
            set
            {
                _LogFileName = value;
            }
        }

        /// <summary>
        /// Defines the default Logging state (Log Enabled)
        /// </summary>
        private LoggingState _LogFlag = LoggingState.Enabled;
        private LogType _logType;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes Log State, FileName
        /// </summary>
        public Logger()
        {
            try
            {
                _className = this.GetType().FullName;
                if (AppSettings.getItem("LOG_TYPE").Trim() == null || AppSettings.getItem("LOG_TYPE").Trim() == "")
                    _logType = LogType.DB;
                if (AppSettings.getItem("LOG_TYPE") == "DB")
                    _logType = LogType.DB;
                else if (AppSettings.getItem("LOG_TYPE") == "TEXTFILE")
                    _logType = LogType.TextFile;
                else if (AppSettings.getItem("LOG_TYPE") == "DBTEXTFILE")
                    _logType = LogType.DBTextFile;
                else
                    _logType = LogType.EventLog;
            }
            catch (Exception)
            {
                _logType = LogType.DB;
            }

            _AppendToFile = true;
            SetLogState();
            SetLogFileName();
        }

        /// <summary>
        /// Initializes Log State, FileName, requests AppendToFile parameter
        /// </summary>
        /// <param name="AppendToFile"></param>
        public Logger(bool AppendToFile)
        {
            try
            {
                _className = this.GetType().FullName;
                if (AppSettings.getItem("LOG_TYPE").Trim() == null || AppSettings.getItem("LOG_TYPE").Trim() == "")
                    _logType = LogType.DB;
                if (AppSettings.getItem("LOG_TYPE") == "DB")
                    _logType = LogType.DB;
                else if (AppSettings.getItem("LOG_TYPE") == "TEXTFILE")
                    _logType = LogType.TextFile;
                else if (AppSettings.getItem("LOG_TYPE") == "DBTEXTFILE")
                    _logType = LogType.DBTextFile;
                else
                    _logType = LogType.EventLog;
            }
            catch (Exception)
            {
                _logType = LogType.DB;
            }
            _AppendToFile = AppendToFile;
            SetLogState();
            SetLogFileName();
        }

        public Logger(string LogFileName)
        {
            try
            {
                _className = this.GetType().FullName;
                if (AppSettings.getItem("LOG_TYPE").Trim() == null || AppSettings.getItem("LOG_TYPE").Trim() == "")
                    _logType = LogType.DB;
                if (AppSettings.getItem("LOG_TYPE") == "DB")
                    _logType = LogType.DB;
                else if (AppSettings.getItem("LOG_TYPE") == "TEXTFILE")
                    _logType = LogType.TextFile;
                else if (AppSettings.getItem("LOG_TYPE") == "DBTEXTFILE")
                    _logType = LogType.DBTextFile;
                else
                    _logType = LogType.EventLog;
            }
            catch (Exception)
            {
                _logType = LogType.DB;
            }
            _LogFlag = LoggingState.Enabled;
            _LogFileName = LogFileName;
            _AppendToFile = true;
        }
        public Logger(string LogFileName, string className)
        {
            try
            {
                _className = this.GetType().FullName;

                if (AppSettings.getItem("LOG_TYPE").Trim() == null || AppSettings.getItem("LOG_TYPE").Trim() == "")
                    _logType = LogType.DB;
                if (AppSettings.getItem("LOG_TYPE") == "DB")
                    _logType = LogType.DB;
                else if (AppSettings.getItem("LOG_TYPE") == "TEXTFILE")
                    _logType = LogType.TextFile;
                else if (AppSettings.getItem("LOG_TYPE") == "DBTEXTFILE")
                    _logType = LogType.DBTextFile;
                else
                    _logType = LogType.EventLog;
            }
            catch (Exception)
            {
                _logType = LogType.DB;
            }
            _LogFlag = LoggingState.Enabled;
            _LogFileName = LogFileName;
            _AppendToFile = true;
            _className = className;
        }
        public Logger(bool AppendToFile, string className)
        {
            try
            {
                _className = this.GetType().FullName;

                if (AppSettings.getItem("LOG_TYPE").Trim() == null || AppSettings.getItem("LOG_TYPE").Trim() == "")
                    _logType = LogType.DB;
                if (AppSettings.getItem("LOG_TYPE") == "DB")
                    _logType = LogType.DB;
                else if (AppSettings.getItem("LOG_TYPE") == "TEXTFILE")
                    _logType = LogType.TextFile;
                else if (AppSettings.getItem("LOG_TYPE") == "DBTEXTFILE")
                    _logType = LogType.DBTextFile;
                else
                    _logType = LogType.EventLog;
            }
            catch (Exception)
            {
                _logType = LogType.DB;
            }
            _LogFlag = LoggingState.Enabled;
            _LogFileName = LogFileName;
            _AppendToFile = true;
            _className = className;
        }
        #endregion

        #region "Private Routines"

        /// <summary>
        /// Sets the log File Name from config file
        /// </summary>
        private void SetLogFileName()
        {
            System.IO.DirectoryInfo dirInfo;
            string logFileName = AppSettings.getItem("LogFileName");
            if (logFileName != null)
            {
                dirInfo = Directory.GetParent(this.GetType().Assembly.Location);
                _LogFileName = string.Concat(dirInfo.FullName, "\\", logFileName);
            }

            //Directory.GetDirectoryRoot()


            //this.GetType() 
        }

        /// <summary>
        /// Checks for Log Flag in configuration file
        /// </summary>
        private void SetLogState()
        {
            string LogFlag = AppSettings.getItem("LogFlag");
            if (LogFlag != null)
            {
                switch (LogFlag)
                {
                    case "0":
                        {
                            _LogFlag = LoggingState.Disable;
                            break;
                        }
                    case "1":
                        {
                            _LogFlag = LoggingState.Enabled;
                            break;
                        }
                }
            }
        }

        #endregion

        #region "Public Routines"

        /// <summary>
        /// Overload of method to	Log description into Logger output
        /// </summary>
        /// <param name="logString"></param>
        /// <param name="application"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        public bool LogItem(string logString, string className, string module)
        {
            //return LogItem(logString, application, module, LogType.TextFile);
            //JobTransactionType transType = JobTransactionType.Unassigned;
            string transUser = string.Empty;
            string transactionId = string.Empty;
            return LogItem(logString, className, module, transactionId, transUser, _logType);
        }


        /// <summary>
        /// Log description into Logger output
        /// </summary>
        /// <param name="logString"></param>
        /// <param name="logType"></param>
        /// <param name="application"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        public bool LogItem(string logString, string className, string module,
            LogType logType)
        {
            string transUser = string.Empty;
            string transactionid = string.Empty;
            //JobTransactionType transType = JobTransactionType.Unassigned;
            return LogItem(logString, className, module, transactionid, transUser, logType);
        }


        public bool LogItem(string logString, string className, string module,
            string transactionId)
        {
            string transUser = string.Empty;
            //JobTransactionType transType = JobTransactionType.Unassigned;
            return LogItem(logString, className, module, transactionId, transUser);
        }


        /// <summary>
        /// Log description into Logger output
        /// </summary>
        /// <param name="logString"></param>
        /// <param name="logType"></param>
        /// <param name="application"></param>
        /// <param name="module"></param>
        /// <returns></returns>
        public bool LogItem(string logString, string className, string module,
            string transactionId, LogType logType)
        {
            string transUser = string.Empty;
            //JobTransactionType transType = JobTransactionType.Unassigned;
            return LogItem(logString, className, module, transactionId, transUser, logType);
        }


        public bool LogItem(string logString, string className, string module,
            string transactionId, string transUser)
        {
            return LogItem(logString, className, module, transactionId, transUser, _logType);
        }


        public bool LogItem(string logString, string className, string module,
            string transactionId, string transUser, LogType logType)
        {
            string logRecord = "";
            string delimiter = ",";
            string currentDate = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");
            //string filesource, filedest;
            StreamWriter sw;
            object _QueueObject;
            WindowsIdentity user;

            try
            {
                if (transUser.Trim().Length == 0)
                {
                    user = WindowsIdentity.GetCurrent();
                    transUser = user.Name;
                }

                if (logType != LogType.Unassigned)
                    _logType = logType;

                if (className.Trim().Length == 0)
                {
                    className = AppDomain.CurrentDomain.FriendlyName;
                }

                if (_LogFlag == LoggingState.Enabled)
                {
                    switch (_logType)
                    {
                        case LogType.TextFile:
                            {
                                _QueueObject = this;
                                lock (_QueueObject)
                                {
                                    logRecord = string.Concat(currentDate, delimiter, className, delimiter, module,
                                        delimiter, logString);
                                    //sw = new StreamWriter(_LogFileName, _AppendToFile);
                                    sw = GetStreamWriter(_LogFileName, _AppendToFile);
                                    sw.WriteLine(logRecord);
                                    sw.Close();
                                    sw.Flush();
                                }
                                break;
                            }
                        case LogType.DB:
                            {
                                string dt = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");
                                /*HPMBuildSummary.UpdateLogTransactions(transactionId, logString,
                                    dt, (int)transType, transUser, (int)Application, className, module);*/
                                break;
                            }
                        case LogType.DBTextFile:
                            {
                                string dt = DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fff");
                                _QueueObject = this;
                                lock (_QueueObject)
                                {
                                    /*HPMBuildSummary.UpdateLogTransactions(transactionId, logString, dt,
                                        (int)transType, transUser, (int)Application, className, module);*/
                                    logRecord = string.Concat(currentDate, delimiter, className, delimiter, module,
                                        delimiter, logString);
                                    sw = GetStreamWriter(_LogFileName, _AppendToFile);
                                    sw.WriteLine(logRecord);
                                    sw.Close();
                                }
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorDescription = ex.Message;
                return false;
            }
            return true;
        }


        private StreamWriter GetStreamWriter(string logFileName, bool appendToFile)
        {
            string filedest = "";
            StreamWriter sw = null;
            sw = new StreamWriter(logFileName, appendToFile);
            if (sw.BaseStream.Length > 1048576)
            {
                sw.Close();
                filedest = logFileName.Replace(".log",
                    string.Concat("_", DateTime.Now.Month, "_", DateTime.Now.Day,
                                  "_", DateTime.Now.Year, "_", DateTime.Now.Hour, "_", DateTime.Now.Minute, ".log"));
                File.Copy(logFileName, filedest, true);
                sw = new StreamWriter(logFileName, false);
            }
            return sw;
        }
        #endregion

    }
}
