using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;

namespace Repo
{
    public class AudioTranscribeRepository
    {
        private readonly DatabaseContext dbContext;

        public AudioTranscribeRepository(string connectionString)
        {
            dbContext = new DatabaseContext(connectionString);
        }

        public AudioTranscribeRepository()
        {
            dbContext = new DatabaseContext();
        }

        public void InsertAudioTranscribe(int clientId, int jobStatus, int fileType, string audioFileName = null,
            string transcribeFilePath = null, DateTime? transcribeStartTime = null, DateTime? transcribeEndTime = null, DateTime? transcribeDate = null)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@JobStatus", jobStatus),
            new SqlParameter("@FileType", fileType),
            new SqlParameter("@AudioFileName", audioFileName !=null ? (object)audioFileName : DBNull.Value),
            new SqlParameter("@TranscribeFilePath", transcribeFilePath != null ? (object)transcribeFilePath : DBNull.Value),
            new SqlParameter("@TranscribeStartTime", transcribeStartTime.HasValue ? (object)transcribeStartTime.Value : DBNull.Value),
            new SqlParameter("@TranscribeEndTime", transcribeEndTime.HasValue ? (object)transcribeEndTime.Value : DBNull.Value),
            new SqlParameter("@TranscribeDate", transcribeDate.HasValue ? (object)transcribeDate.Value : DBNull.Value)
        };

             dbContext.ExecuteScalarStoredProcedure("InsertAudioTranscribe", parameters.ToArray());
        }

        public void UpdateAudioTranscribeByFileName(int? clientId, int? jobStatus, int? fileType, string audioFileName,
    string transcribeFilePath = null, DateTime? transcribeStartTime = null, DateTime? transcribeEndTime = null, DateTime? transcribeDate = null)
        {
            // Build the list of parameters for the stored procedure call
            List<SqlParameter> parameters = new List<SqlParameter>
    {
        new SqlParameter("@ClientId", clientId),
        new SqlParameter("@JobStatus", jobStatus !=null ? (object)jobStatus : DBNull.Value ),
        new SqlParameter("@FileType", fileType !=null ? (object)fileType : DBNull.Value),
        new SqlParameter("@AudioFileName", audioFileName !=null ? (object)audioFileName : DBNull.Value),
        new SqlParameter("@TranscribeFilePath", transcribeFilePath != null ? (object)transcribeFilePath : DBNull.Value),
        new SqlParameter("@TranscribeStartTime", transcribeStartTime.HasValue ? (object)transcribeStartTime.Value : DBNull.Value),
        new SqlParameter("@TranscribeEndTime", transcribeEndTime.HasValue ? (object)transcribeEndTime.Value : DBNull.Value),
        new SqlParameter("@TranscribeDate", transcribeDate.HasValue ?(object) transcribeDate.Value : DBNull.Value)
    };

            // Call the stored procedure
            dbContext.ExecuteScalarStoredProcedure("UpdateAudioTranscribeByFileName", parameters.ToArray());
        }

        public AudioTranscribe GetAudioTranscribeDetails(int clientId, string audioFileName)
        {
            // Call the stored procedure using DatabaseContext
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                 new SqlParameter("@ClientId", clientId),
                 new SqlParameter("@AudioFileName", audioFileName)
             };

            DataTable resultTable = dbContext.ExecuteStoredProcedure("GetAudioTranscribeDetails", parameters.ToArray());

            // Check if any rows are returned
            if (resultTable.Rows.Count > 0)
            {
                DataRow row = resultTable.Rows[0];
                AudioTranscribe audioTranscribe = new AudioTranscribe
                {
                    // Map the DataRow to AudioTranscribe properties
                    Id = Convert.ToInt32(row["Id"]),
                    ClientId = Convert.ToInt32(row["ClientId"]),
                    // Map other properties as needed
                };
                return audioTranscribe;
            }
            else
            {
                return null; // Or throw an exception if desired
            }
        }

        public List<(int Id, string AudioFileName)> GetSAJobs(int clientId, int jobStatus)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@ClientId", clientId),
                new SqlParameter("@JobStatus", jobStatus)
            };

            DataTable resultTable = dbContext.ExecuteStoredProcedure("GetSentimentJob", parameters.ToArray());

            List<(int Id, string AudioFileName)> results = new List<(int Id, string AudioFileName)>();

            foreach (DataRow row in resultTable.Rows)
            {
                int id = Convert.ToInt32(row["Id"]);
                string audioFileName = Convert.ToString(row["AudioFileName"]);

                results.Add((id, audioFileName));
            }

            return results;
        }

        public void UpdateSADoneById(int id)
        {
            // Create list of parameters for the stored procedure call
            SqlParameter[] parameters =
            {
                new SqlParameter("@Id", id)
            };

            // Call the stored procedure
            dbContext.ExecuteStoredProcedure("UpdateSADoneById", parameters);
        }

    }
}
