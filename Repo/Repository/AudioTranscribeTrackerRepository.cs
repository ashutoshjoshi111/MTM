using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Repo
{
    public class AudioTranscribeTrackerRepository
    {
        private readonly DatabaseContext dbContext;

        public AudioTranscribeTrackerRepository(string connectionString)
        {
            dbContext = new DatabaseContext(connectionString);
        }

        public AudioTranscribeTrackerRepository()
        {
            dbContext = new DatabaseContext();
        }

        public void InsertAudioTranscribeTracker(int clientId, int audioId, int chunkStatus, int chunkFileType, string chunkFileName, int? chunkSequence, string chunkText, string chunkFilePath, DateTime? chunkTranscribeStart, DateTime? chunkTranscribeEnd, DateTime? chunkCreatedDate)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
        {
            new SqlParameter("@ClientId", clientId),
            new SqlParameter("@AudioId", audioId),
            new SqlParameter("@ChunkStatus", chunkStatus),
            new SqlParameter("@ChunkFileType", chunkFileType),
            new SqlParameter("@ChunkFileName", chunkFileName ?? (object)DBNull.Value),
            new SqlParameter("@ChunkSequence", chunkSequence ?? (object)DBNull.Value),
            new SqlParameter("@ChunkText", chunkText ?? (object)DBNull.Value),
            new SqlParameter("@ChunkFilePath", chunkFilePath ?? (object)DBNull.Value),
            new SqlParameter("@ChunkTranscribeStart", chunkTranscribeStart ?? (object)DBNull.Value),
            new SqlParameter("@ChunkTranscribeEnd", chunkTranscribeEnd ?? (object)DBNull.Value),
            new SqlParameter("@ChunkCreatedDate", chunkCreatedDate ?? (object)DBNull.Value)
        };

            dbContext.ExecuteScalarStoredProcedure("InsertAudioTranscribeTracker", parameters.ToArray());
        }

        public void UpdateAudioTranscribeTracker(int id, int? clientId, int? audioId, int? chunkStatus, int? chunkFileType, string chunkFileName, int? chunkSequence, string chunkText, string chunkFilePath, DateTime? chunkTranscribeStart, DateTime? chunkTranscribeEnd, DateTime? chunkCreatedDate)
        {
            // Build list of non-null parameters
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@Id", id),
                new SqlParameter("@ClientId", clientId ?? (object)DBNull.Value),
                new SqlParameter("@AudioId", audioId ?? (object)DBNull.Value),
                new SqlParameter("@ChunkStatus", chunkStatus ?? (object)DBNull.Value),
                new SqlParameter("@ChunkFileType", chunkFileType ?? (object)DBNull.Value),
                new SqlParameter("@ChunkFileName", string.IsNullOrEmpty(chunkFileName) ? (object)DBNull.Value : chunkFileName),
                new SqlParameter("@ChunkSequence", chunkSequence ?? (object)DBNull.Value),
                new SqlParameter("@ChunkText", string.IsNullOrEmpty(chunkText) ? (object)DBNull.Value : chunkText),
                new SqlParameter("@ChunkFilePath", string.IsNullOrEmpty(chunkFilePath) ? (object)DBNull.Value : chunkFilePath),
                new SqlParameter("@ChunkTranscribeStart", chunkTranscribeStart ?? (object)DBNull.Value),
                new SqlParameter("@ChunkTranscribeEnd", chunkTranscribeEnd ?? (object)DBNull.Value),
                new SqlParameter("@ChunkCreatedDate", chunkCreatedDate ?? (object)DBNull.Value)
            };

            dbContext.ExecuteScalarStoredProcedure("UpdateAudioTranscribeTracker", parameters.ToArray());
        }

        public void IncreaseRetryCountAudioTranscribeTracker(int id)
        {
            List<SqlParameter> parameters = new List<SqlParameter>
            {
                new SqlParameter("@id", id)
            };

            dbContext.ExecuteScalarStoredProcedure("IncreaseRetryCountAudioTranscribeTracker", parameters.ToArray());
        }

    }
    
}
