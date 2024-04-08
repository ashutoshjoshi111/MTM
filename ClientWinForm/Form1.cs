using Microsoft.VisualBasic.Devices;
using MTS;
using Utility.FileOps;
using System;
using System.Threading.Tasks;
using ClientWrapper;
using System.Windows.Forms;
using Repo;
using System.Net.Http;

namespace ClientWinForm
{
    public partial class Form1 : Form
    {

        string baseUrl = "http://flm-vm-cogaidev:4091/transcribe_audio_text";

        //string baseUrl = "http://flm-vm-cogaidev:4091/merge_chunk_transcribe_text";

        //string baseUrl = "http://flm-vm-cogaidev:4091/get_data_from_sentiment_table";

        /*
         http://localhost:4091/dump_data_into_sentiment?audio_file=CallRecording111234.mp3&clientid=1 
        */
        public Form1()
        {
            InitializeComponent();
        }

        AudioTranscriber audioTranscriber = new AudioTranscriber("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");
        OpenAIWhisper openAIWhisper = new OpenAIWhisper("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");

        private async Task<string> TranscribeAudio(string audioFilePath)
        {
            try
            {
                string resultText = await audioTranscriber.TranscribeAudio(audioFilePath);
                return resultText;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<string> WhisperTranscribeAudio(string audioFilePath)
        {
            try
            {
                string resultText = await openAIWhisper.TranscribeWav(audioFilePath);
                return resultText;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task PerformHttpRequest()
        {
            try
            {
                //string result = await _httpClient.GetAsync(resource);

                using (ApiClient apiConsumer = new ApiClient(baseUrl))
                {
                    var firstResponse = await apiConsumer.GetAsync(baseUrl, ("clientid", "1"), ("id", "255"));
                    //apiConsumer.Get(baseUrl, ("audio_file", "DMV-85311-MU11.wav"), ("clientid", "1"));
                    //firstResponse = apiConsumer.Get(baseUrl, ("audio_file", "DMV-85311-MU11.wav"), ("clientid", "1"));
                }


                // Process the result as needed
            }
            catch (Exception ex)
            {
                // Handle exceptions
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            //string baseUrl = "http://flm-vm-cogaidev:4091/dump_data_into_sentiment";
            String firstResponse;

            string audioFilePath = "C:\\Ashutosh Joshi\\ChatGPT - bkp\\ChatGPT\\ChatGPT\\ChatGPT\\mthreadflask\\Recording\\DMV-85311-MU11_Chunk_6.wav"; // Provide the path to your audio file
                                                                                                                                                        //string transcript =  TranscribeAudio(audioFilePath).Result;

            string mp3FilePath = "C:\\AICogent\\ICFiles"; // Path to the input MP3 file
            string wavFilePath = "C:\\AICogent\\ICFiles\\Chunk"; // Path to the output WAV file

            FileManagement fileOpx = new FileManagement();
            fileOpx.CopyFilesToNewFolders(mp3FilePath, wavFilePath);



            //string transcript = TranscribeAudio(audioFilePath).Result;


            //Task.Run(async () => await PerformHttpRequest());

            /*
                        using (ApiClient apiConsumer = new ApiClient(baseUrl))
                        {
                            apiConsumer.Get(baseUrl, ("clientid", "1"), ("id", "222"));
                            //apiConsumer.Get(baseUrl, ("audio_file", "DMV-85311-MU11.wav"), ("clientid", "1"));
                            //firstResponse = apiConsumer.Get(baseUrl, ("audio_file", "DMV-85311-MU11.wav"), ("clientid", "1"));
                        }


                        using (ApiClient apiConsumer = new ApiClient(baseUrl))
                        {
                            //apiConsumer.Get(baseUrl, ("clientid", "1"), ("id", "222"));
                            //apiConsumer.Get(baseUrl, ("audio_file", "DMV-85311-MU11.wav"), ("clientid", "1"));
                            firstResponse = apiConsumer.Get(baseUrl, ("audio_file", "DMV-85311-MU11.wav"), ("clientid", "1"));
                        }

                       
                        ////var targetFormat = new NAudio.Wave.WaveFormat(16000, 16, 1); // Example: 16 kHz sample rate, 16-bit depth, mono
                        ////var converter = new voiceFileOps();


                        //converter.ConvertMp3ToWav(mp3FilePath, wavFilePath, targetFormat);


                        int numberOfThreads = 1; // You can configure the number of threads here
                        for (int i = 0; i < numberOfThreads; i++)
                        {
                            Thread thread = new Thread(() =>
                            {
                                BaseThreadClass obj = new BaseThreadClass();
                                obj.runThread();
                            });
                            thread.Start();
                        }
              */

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            BaseThreadClass obj = new BaseThreadClass();
            obj.runThread();
        }

        public void runThread()
        { 
        
        }
    }
}