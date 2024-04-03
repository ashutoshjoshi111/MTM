#region Import namesapces
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Text;
using System.IO;
using System.Configuration;
using System.Data.Sql;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Management;
using System.Diagnostics.PerformanceData;
using System.Reflection;
#endregion

namespace MTS
{
    public class Repo
    {
        public DataTable Poll()
        {
            try
            {
                string connectionStr = "Server=FLM-VM-COGAIDEV;Database=AudioTrans;User Id=sanjeev;password=AgreeYa!@#$";
                SqlConnection conn = new SqlConnection(connectionStr);
                SqlCommand cmd = new SqlCommand();
                //cmd.CommandType = CommandType.StoredProcedure;
                cmd.Connection = conn;
                cmd.CommandText = "Select * from [dbo].[JobQueue]";

                //SqlParameter param = new SqlParameter("@JobID", SqlDbType.VarChar);
                //param.Value = JobId;
                //cmd.Parameters.Add(param);
                //param = null;

                SqlDataAdapter da = new SqlDataAdapter(cmd);
                DataSet ds = new DataSet();
                da.Fill(ds);

                DataTable dt = ds.Tables[0];

                return dt.Copy();
                //return (DataTable)returnvar;
            }
            catch (Exception ex)
            {
                //Logger.Logger objLog = new Logger.Logger(ConfigurationManager.AppSettings["LogFile"].ToString());
                //objLog.LogItem("Error in Poll Reflection" + ex.Message, "Reflection", "Poll");
                return null;

            }
        }
    }
}
