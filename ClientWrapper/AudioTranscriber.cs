using System;
using System.IO;
using System.Threading.Tasks;
using OpenAI_API;

namespace ClientWrapper
{
    public class AudioTranscriber
    {
        private readonly OpenAIAPI _openAiApi;

        public AudioTranscriber(string apiKey)
        {
            APIAuthentication apiAuthentication = new APIAuthentication(apiKey);
            _openAiApi = new OpenAIAPI(apiAuthentication);
        }

        public async Task<string> TranscribeAudio(string audioFilePath)
        {
            try
            {
                string resultText = await _openAiApi.Transcriptions.GetTextAsync(audioFilePath);
                
                return resultText;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private string ConvertAudioToBase64(string audioFilePath)
        {
            byte[] audioBytes = File.ReadAllBytes(audioFilePath);
            return Convert.ToBase64String(audioBytes);
        }
    }
}
