
namespace Repo
{
    public class JobQueue
    {
        public long JobId { get; set; }
        public string? JobName { get; set; }
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; }
        public int? Step { get; set; }
        public int? JobStatusId { get; set; }
        public int Retry { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
