
namespace Repo
{
    public enum JobSteps
    {
        PreStart = 1,
        FileValidation=2,
        FileConversion=3,
        Chunk =4,
        FileMovement=5,
        FileDeletion=6,
        Transcript=7,
        Sentiment = 8,
        Compliance = 9,        
        DuplicateFile=10,
        Error=11,
        WindowServiceRunning=12,
        WindowServiceStop=13,
        Complete=14
    }
}
