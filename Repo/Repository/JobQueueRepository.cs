using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Repo
{
    public class JobQueueRepository
    {
        private readonly DatabaseContext dbContext;

        public JobQueueRepository(string connectionString)
        {
            dbContext = new DatabaseContext(connectionString);
        }
        public JobQueueRepository()
        {
            dbContext = new DatabaseContext();
        }
        public void UpdateJobQueue(int jobId, string jobName, DateTime? startDateTime, DateTime? endDateTime, int? step, int? jobStatusId)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@JobId", jobId),
            new SqlParameter("@JobName", jobName),
            new SqlParameter("@StartDateTime", startDateTime ?? (object)DBNull.Value),
            new SqlParameter("@EndDateTime", endDateTime ?? (object)DBNull.Value),
            new SqlParameter("@Step", step ?? (object)DBNull.Value),
            new SqlParameter("@JobStatusId", jobStatusId)
        };

            dbContext.ExecuteScalarStoredProcedure("UpdateJobQueue", parameters.ToArray());
        }

        public void InsertJobQueue(string jobName, DateTime? startDateTime, DateTime? endDateTime, int? step, int? jobStatusId)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@JobName", jobName !=null ? (object)jobName : DBNull.Value),
                new SqlParameter("@StartDateTime", startDateTime ?? (object)DBNull.Value),
                new SqlParameter("@EndDateTime", endDateTime ?? (object)DBNull.Value),
                new SqlParameter("@Step", step !=null ? (object)step : DBNull.Value),
                new SqlParameter("@JobStatusId", jobStatusId !=null ? (object)jobStatusId : DBNull.Value)
            };

            dbContext.ExecuteScalarStoredProcedure("InsertJobQueue", parameters.ToArray());
        }


        public List<JobQueue> GetJobQueueByStatus(List<int> jobStatusIds)
        {
            List<JobQueue> jobQueues = new List<JobQueue>();

            try
            {
                // Create TVP (Table-Valued Parameter) for JobStatusIds
                DataTable tvp = new DataTable();
                tvp.Columns.Add(new DataColumn("JobStatusId", typeof(int)));
                foreach (int jobStatusId in jobStatusIds)
                {
                    tvp.Rows.Add(jobStatusId);
                }

                // Call the stored procedure using DatabaseContext
                SqlParameter parameter = new SqlParameter("@JobStatusIds", tvp);
                DataTable resultTable = dbContext.ExecuteStoredProcedure("GetJobQueueByStatus", new SqlParameter[] { parameter });

                // Convert DataTable result to List<JobQueue>
                foreach (DataRow row in resultTable.Rows)
                {
                    JobQueue jobQueue = new JobQueue
                    {
                        JobId = Convert.ToInt64(row["JobId"]),
                        JobName = row["JobName"].ToString(),
                        StartDateTime = row.IsNull("StartDateTime") ? null : (DateTime?)row["StartDateTime"],
                        EndDateTime = row.IsNull("EndDateTime") ? null : (DateTime?)row["EndDateTime"],
                        Step = row.IsNull("Step") ? null : (int?)row["Step"],
                        JobStatusId = row.IsNull("JobStatusId") ? null : (int?)row["JobStatusId"],
                        Retry = Convert.ToInt32(row["Retry"]),
                        CreatedDate = Convert.ToDateTime(row["CreatedDate"])
                    };
                    jobQueues.Add(jobQueue);
                }
            }
            catch (Exception ex)
            {
                // Handle exception or log error
                Console.WriteLine($"Error retrieving job queues: {ex.Message}");
            }

            return jobQueues;
        }


        public DataTable GetBackgroundJobsByClientAndStatus(int clientId, int jobStatusId)
        {
            DataTable resultTable = new DataTable();

            try
            {
                // Create parameters
                SqlParameter clientIdParam = new SqlParameter("@ClientID", clientId);
                SqlParameter jobStatusIdParam = new SqlParameter("@JobStatusID", jobStatusId);

                // Call the stored procedure using DatabaseContext
                resultTable = dbContext.ExecuteStoredProcedure("GetBackgroundJobsByClientAndStatus", new SqlParameter[] { clientIdParam, jobStatusIdParam });
            }
            catch (Exception ex)
            {
                // Handle exception or log error
                Console.WriteLine($"Error retrieving background jobs: {ex.Message}");
            }

            return resultTable.Copy();
        }

    }


}
