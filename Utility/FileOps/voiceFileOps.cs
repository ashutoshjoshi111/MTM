using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NAudio.Wave;

namespace Utility.FileOps
{
    public class voiceFileOps
    {
        public void ConvertMp3ToWav(string mp3FilePath, string wavFilePath, WaveFormat targetFormat)
        {
            // Adjust encoding parameters
            using (var mp3Reader = new Mp3FileReader(mp3FilePath))
            {
                var pcmStream = new WaveFormatConversionStream(targetFormat, mp3Reader);
                WaveFileWriter.CreateWaveFile(wavFilePath, pcmStream);
            }

        }
    }
}
