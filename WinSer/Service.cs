using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Logger;
using Utility.FileOps;
using MTS;
using Microsoft.IdentityModel.Tokens;
using Repo;
using ClientWrapper;
using System.Security.Cryptography;
using System.Globalization;
using Utility.MachineOps;

namespace WinSer
{
    partial class Service : ServiceBase
    {
        Logger.Logger objLogger = new Logger.Logger(ConfigurationManager.AppSettings["LogFile"].ToString());

        AudioTranscribeRepository audioTranscribeRepository = new AudioTranscribeRepository();

        readonly static int clientId = Convert.ToInt16(ConfigurationSettings.AppSettings["clientID"].ToString());
        string SATranscriptURL = ConfigurationSettings.AppSettings["SATranscriptURL"].ToString();

        int CpuUtilizationLimit = Convert.ToInt16(ConfigurationManager.AppSettings["CpuUtilizationLimit"]);


        MachineOps machineOps;

        public Service()
        {
            objLogger.LogItem("In Constructor before InitializeComponent()-", "Service", "Constructor");
            InitializeComponent();
            objLogger.LogItem("In Constructor after InitializeComponent()-", "Service", "Constructor");
        }

        protected override void OnStart(string[] args)
        {
            // TODO: Add code here to start your service.
            objLogger.LogItem("In Onstart-", "Service", "OnStart");
            tmrRunForPendingJobs.Enabled = true;
            tmrRunForChunk.Enabled = true;
            tmrRunForSentiment.Enabled = true;
            tmrRunForScoreCard.Enabled = true;

            try
            {
                audioTranscribeRepository.UpdateJobStatusToPreProcessingInBulk();
            }
            catch (Exception ex)
            {
                objLogger.LogItem("Exception in UpdateJobStatusToPreProcessingInBulk", "Service", "OnStart");
            }
        }

        protected override void OnStop()
        {
            // TODO: Add code here to perform any tear-down necessary to stop your service.
            tmrRunForPendingJobs.Enabled = false;
            tmrRunForChunk.Enabled = false;
            tmrRunForSentiment.Enabled = false;
            tmrRunForScoreCard.Enabled = false;
           
            try 
            {
                audioTranscribeRepository.UpdateJobStatusToPreProcessingInBulk();
            }
            catch (Exception ex) 
            {
                objLogger.LogItem("Exception in UpdateJobStatusToPreProcessingInBulk", "Service", "OnStop");
            }
            
            objLogger.LogItem("OnStop-", "Service", "OnStop");
        }

        private void tmrRunForPendingJobs_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                objLogger.LogItem("In the begining of tmrRunForPendingJobs_Elapsed-", "Service", "OnStart");

                tmrRunForPendingJobs.Enabled = false;
                string mp3FilePath = ConfigurationManager.AppSettings["SourceFolder"].ToString(); // Path to the input MP3 file
                string wavFilePath = ConfigurationManager.AppSettings["DestinationFolder"].ToString(); ; // Path to the output WAV file

                if (mp3FilePath.IsNullOrEmpty())
                {
                    objLogger.LogItem("mp3FilePath is null or empty", "Service", "Timer Elapse");

                }

                if (wavFilePath.IsNullOrEmpty())
                {
                    objLogger.LogItem("wavFilePath is null or empty", "Service", "Timer Elapse");

                }

                FileManagement fileOpx = new FileManagement();
                fileOpx.CopyFilesToNewFolders(mp3FilePath, wavFilePath);
                //RunJobs();
            }
            catch (Exception ex)
            {
                objLogger.LogItem("Error in Timer Elapse-" + ex.Message, "Service", "Timer Elapse");
            }
            finally
            {
                tmrRunForPendingJobs.Enabled = true;
            }

        }

        private void tmrRunForChunk_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                float currentUtilization;
                objLogger.LogItem("In the begining of tmrRunForChunk_Elapsed-", "Service", "OnStart");

                currentUtilization = MachineOps.getCPUUsage();

                if (currentUtilization > CpuUtilizationLimit)
                {
                    objLogger.LogItem("CPU Utilization exceeding the limit in mrRunForChunk job, current utilizatio is "+currentUtilization.ToString(), "Service", "tmrRunForSentiment");
                    Thread.Sleep(5000);
                    return;
                }

                tmrRunForChunk.Enabled = false;

                BaseThreadClass obj = new BaseThreadClass();
                obj.runThread();
            }
            catch (Exception ex)
            {
                objLogger.LogItem("Error in tmrRunForChunk_Elapsed Elapse-" + ex.Message, "Service", "tmrRunForChunk_Elapsed");
            }
            finally
            {
                tmrRunForChunk.Enabled = true;
            }

        }

        private void tmrRunForSentiment_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                float currentUtilization;

                objLogger.LogItem("In the begining of tmrRunForSentiment_Elapsed-", "Service", "tmrRunForSentiment");

                currentUtilization = MachineOps.getCPUUsage();

                if (currentUtilization > CpuUtilizationLimit) 
                {
                    objLogger.LogItem("CPU Utilization exceeding the limit in setiment analysis job, current utilizatio is "+currentUtilization.ToString(), "Service", "tmrRunForSentiment");
                    Thread.Sleep(5000);
                    return;
                }

                tmrRunForSentiment.Enabled = false;

                //Details of completed transcribe jobs
                List<(int Id, string AudioFileName)> results = audioTranscribeRepository.GetSAJobs(clientId, (int)JobStatus.Completed);

                // Check if the results list is null or empty
                if (results == null || results.Count == 0)
                {
                    objLogger.LogItem("No sentiment analysis job to run.", "Service", "tmrRunForSentiment");
                }
                else
                {
                    // Process each row in the results list
                    foreach (var result in results)
                    {
                        int id = result.Id;
                        string audioFileName = result.AudioFileName;

                        objLogger.LogItem($"Running Sentiment job for Id: {id}, AudioFileName: {audioFileName} and before retry count increase.", "Service", "tmrRunForSentiment");

                        audioTranscribeRepository.IncreaseRetryCountSAJob(id);

                        Task.Run(async () => await SentimentAnalysisAsync(id.ToString(), audioFileName));
                        Thread.Sleep(2000);

                        objLogger.LogItem($"After successfully Running Sentiment job for Id: {id}, AudioFileName: {audioFileName}.", "Service", "tmrRunForSentiment");

                    }
                }
            }
            catch (Exception ex)
            {
                objLogger.LogItem("Error in tmrRunForSentiment_Elapsed Elapse-" + ex.Message, "Service", "tmrRunForChunk_Elapsed");
            }
            finally
            {
                tmrRunForSentiment.Enabled = true;
            }

        }


        private async Task SentimentAnalysisAsync(string JobID, string FileName)
        {
            string response = "";
            try
            {
                
                using (ApiClient apiConsumer = new ApiClient(SATranscriptURL))
                {
                    response = await apiConsumer.GetAsync(SATranscriptURL, ("clientid", clientId.ToString()), ("audio_file", FileName));
                    objLogger.LogItem(" Reponse from SentimentAnalysisAsync is = "+response.ToString()+ " date and time is "+DateTime.Now.ToString(), "Services", "SentimentAnalysisAsync");
                }
            }
            catch (Exception ex)
            {
                objLogger.LogItem("Exception is caught in SentimentAnalysisAsync method for JobID = "+JobID+ " Exception details are "+ex.Message.ToString() +" date and time is "+DateTime.Now.ToString(), "services", "SentimentAnalysisAsync");
            }


            if (!((response.Contains("Request failed") || response.Contains("InternalServerError") || response.Contains("\"status\":\"failure\""))))
            {
                objLogger.LogItem("No error in SentimentAnalysisAsync method for JobID = "+JobID+ " date and time is "+DateTime.Now.ToString(), "services", "SentimentAnalysisAsync");
                audioTranscribeRepository.UpdateSADoneById(Convert.ToInt16(JobID));
            }
            else
            {
                objLogger.LogItem("API error is caught in SentimentAnalysisAsync method for JobID = "+JobID+ " date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "SentimentAnalysisAsync");
            }
        }


        private void tmrRunForScoreCard_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                objLogger.LogItem("In the begining of tmrRunForScoreCard_Elapsed-", "Service", "tmrRunForSentiment");

                if (MachineOps.getCPUUsage() > CpuUtilizationLimit)
                {
                    objLogger.LogItem("CPU Utilization exceeding the limit in setiment analysis job, current utilizatio is "+CpuUtilizationLimit.ToString(), "Service", "tmrRunForSentiment");
                    Thread.Sleep(5000);
                    return;
                }
            }
            catch (Exception ex)
            {
                objLogger.LogItem("Exception is caught in tmrRunForScoreCard_Elapsed. "+ DateTime.Now.ToString(), "BaseThreadClass", "SentimentAnalysisAsync");
            }        
        }

    } 
}
