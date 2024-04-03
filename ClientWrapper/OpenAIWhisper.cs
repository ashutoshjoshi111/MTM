using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ClientWrapper
{
   
    public class OpenAIWhisper
    {
        private readonly string _apiKey;

        public OpenAIWhisper(string apiKey)
        {
            _apiKey = apiKey;
        }

        public async Task<string> TranscribeWav(string filePath)
        {
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var content = new ByteArrayContent(File.ReadAllBytes(filePath));
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("audio/wav");

                // Define the model name in the request payload
                var requestData = new
                {
                    model = "whisper-1", // Specify the model name here
                    language = "en",
                    file = content
                };

                // Serialize the request data to JSON
                var jsonRequestData = JsonConvert.SerializeObject(requestData);

                // Create the HTTP request
                var response = await client.PostAsync("https://api.openai.com/v1/audio/transcriptions", new StringContent(jsonRequestData, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                // Read the response content
                var responseString = await response.Content.ReadAsStringAsync();
                var responseObject = JObject.Parse(responseString);

                // Extract the transcribed text from the response
                var transcribedText = responseObject["text"].Value<string>();

                return transcribedText;
            }
        }
    }
}
