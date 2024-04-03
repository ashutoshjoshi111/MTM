using MTS;
namespace Dx;

using NAudio.Wave;
using Utility.FileOps;
class Program
{
    
    static void Main(string[] args)
    {

        string mp3FilePath = "C:\\AICogent\\ICFiles"; // Path to the input MP3 file
        string wavFilePath = "C:\\AICogent\\ICFiles\\Chunk"; // Path to the output WAV file

        FileManagement fileOpx = new FileManagement();
        fileOpx.CopyFilesToNewFolders(mp3FilePath, wavFilePath);

        var targetFormat = new WaveFormat(16000, 16, 1); // Example: 16 kHz sample rate, 16-bit depth, mono
        var converter = new voiceFileOps();
        //converter.ConvertMp3ToWav(mp3FilePath, wavFilePath, targetFormat);

        BaseThreadClass obj = new BaseThreadClass();
        obj.runThread();

        Console.WriteLine("Hello, World!");
    }
}