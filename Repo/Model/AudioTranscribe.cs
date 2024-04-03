namespace Repo
{
    public class AudioTranscribe
    {
        public int Id { get; set; }
        public int ClientId { get; set; }
        public int JobStatus { get; set; }
        public int FileType { get; set; }
        public string AudioFileName { get; set; }
        public string TranscribeFilePath { get; set; }
        public DateTime? TranscribeStartTime { get; set; }
        public DateTime? TranscribeEndTime { get; set; }
        public DateTime? TranscribeDate { get; set; }
        public DateTime Created { get; set; }
        public DateTime Modified { get; set; }
        public bool IsActive { get; set; }
        public bool IsDeleted { get; set; }
    }
}
