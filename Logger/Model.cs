using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Logger
{
    public partial class Logger
    {
        /// <summary>
        /// Defines the destination output type for logging the results
        /// </summary>
        public enum LogType
        {
            /// <summary>
            /// Log Output to a File
            /// </summary>
            TextFile,
            /// <summary>
            /// Log Output to Event Log(Not implemented)
            /// </summary>
            EventLog,
            /// <summary>
            /// Log output to DB(Not implemented)
            /// </summary>
            DB,
            DBTextFile,
            Unassigned
        }


        public enum ApplicationType
        {
            Unassigned,
            Transcript,
            Chunck,
            Summary
        }
    }
}
