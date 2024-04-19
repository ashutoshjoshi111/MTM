#region Import namesapces
using System.Data;
using System.Configuration;
using System.Diagnostics;
using System.Management;
using System.Collections.Concurrent;
using Repo;
using ClientWrapper;
using System.Net.Http;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.Globalization;

#endregion

namespace MTS
{
    public class BaseThreadClass
    {

        #region Declare Global Varibles
        Thread[] workerThreads;
        object objLockingObject;
        static Boolean flagThreads = true;
        readonly static int maxNumberOfThreads = Convert.ToInt16(ConfigurationSettings.AppSettings["maxthreadcount"].ToString());
        private static Int32 numberOfThreads;
        private static Int32 currentThreadCount;
        readonly static int clientId = Convert.ToInt16(ConfigurationSettings.AppSettings["clientID"].ToString());
        string transcriptionAudioURL = ConfigurationSettings.AppSettings["URLTransAudio"].ToString();

        WaitCallback callBack;// = new WaitCallback(BaseThreadClass.callTheConsumer);
        Boolean poolThread = ThreadPool.SetMinThreads(4, 4);

        //PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        //PerformanceCounter ramCounter;

        object lockObject;
        Logger.Logger objLogger = new Logger.Logger(ConfigurationManager.AppSettings["LogFile"].ToString());
        //static List<string> lstObjInQueue = new List<string>();
       
        static ConcurrentHashSet<string> lstObjInQueue = new ConcurrentHashSet<string>();
       
        string objJobid = "";
        private readonly object lstObjLock = new object(); // Lock object for lstObjInQueue

        ConcurrentDictionary<string, int> jobCounts = new ConcurrentDictionary<string, int>();

        JobQueueRepository jobQueueRepository = new JobQueueRepository();

        AudioTranscribeTrackerRepository audioTranscribeTrackerRepository = new AudioTranscribeTrackerRepository();

        #endregion

        /// <summary>
        /// Makes threads and executes.
        /// </summary>
        public void runThread()
        {
            try
            {
                
                objLogger.LogItem("Trying to check  Thread Pool.", "BaseThreadClass", "runThread");

                int CpuUtilizationLimit = 0;
                CpuUtilizationLimit = Convert.ToInt16(ConfigurationManager.AppSettings["CpuUtilizationLimit"]);

                //Checking CPU Utilization
                if (Convert.ToInt16(getCPUUsage()) > CpuUtilizationLimit)
                {
                    objLogger.LogItem(" CPU utilization Alert!!!!!!!!!!!!!!!!!!!!! Current CPU Usages is exceeding " + CpuUtilizationLimit.ToString() + "%. New Threads could not start. ", "BaseThreadClass", "runThread");
                    Thread.Sleep(5);
                    return;
                }


                if ((currentThreadCount >= maxNumberOfThreads) & (flagThreads))
                {
                    return;
                }

                DataSet ds = new DataSet();
                DataTable DataTbl;                
               
                DataTbl = jobQueueRepository.GetBackgroundJobsByClientAndStatus(clientId, (int)JobStatus.PreProcessing);

                if (DataTbl == null)
                {
                    objLogger.LogItem("Table is Null", "BaseThreadClass", "runThread");
                    return;
                }

                ds.Tables.Add(DataTbl);
               
                callBack = new WaitCallback(this.callTheConsumer);
                
                flagThreads = false;

                if (ds.Tables.Count > 0)
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        numberOfThreads = ds.Tables[0].Rows.Count;
                        objLockingObject = this;

                        for (int i = 0; (i <= numberOfThreads - 1) & (i <= maxNumberOfThreads) & (currentThreadCount < maxNumberOfThreads); i++)
                        {

                            string jobid = ds.Tables[0].Rows[i]["Id"].ToString();

                            objLogger.LogItem(" System Putting job Id= " + jobid + " into Thread Pool.", "BaseThreadClass", "runThread");

                            objJobid = jobid;

                            lock (lstObjLock) // Lock lstObjInQueue to prevent concurrent modifications
                            {
                                
                                if (objJobid != null && !lstObjInQueue.Contains(objJobid))
                                {
                                   
                                    ThreadPool.QueueUserWorkItem(callBack, (object)jobid);

                                     lstObjInQueue.Add(objJobid);                                    

                                    // Increment the count for this jobid
                                    jobCounts.AddOrUpdate(objJobid, 1, (_, count) => count + 1);                                    

                                    // Check for duplicates
                                    if (jobCounts[objJobid] > 1)
                                    {
                                        objLogger.LogItem("Alert!!!! Duplicate found " + objJobid, "BaseThreadClass", "runThread");
                                    }                                        

                                        if (currentThreadCount == maxNumberOfThreads)
                                        {
                                            break;
                                        }
                                        Interlocked.Increment(ref currentThreadCount);
                                 }
                                else
                                {
                                    objLogger.LogItem("Alert!!!! Conflict Match found for Job Id =" + objJobid + "..........!, this job will only run after completion of first instance. ", "BaseThreadClass", "runThread");
                                }
                            }
                        }

                        flagThreads = true;
                        ds = null;
                        DataTbl = null;
                    }
                }
            }
            catch (Exception ex)
            {
                objLogger.LogItem(" Error in Running thread " + ex.Message, "BaseThreadClass", "runThread");
            }
        }

        /// <summary>
        /// Makes objects for created threads and calls the run method on the created Objects.
        /// </summary>
        /// <param name="id">object</param>
        public void callTheConsumer(object id)
        {
            
            string jobid = (string)id;
            objLockingObject = this;

            try
            {
                objLogger.LogItem("Before running Run() for the jobid =" + jobid + ". ", "BaseThreadClass", "CallTheConsumer");

                Run(jobid);

                objLogger.LogItem("Job Id =" + jobid + " is completed successfully.", "BaseThreadClass", "CallTheConsumer");

                lock (objLockingObject)
                {
                    Interlocked.Decrement(ref currentThreadCount);

                    if (lstObjInQueue.Contains(jobid))
                    {
                        lock (objLockingObject)
                        {
                            lstObjInQueue.Remove(jobid);
                        }

                        objLogger.LogItem("Removing Job Id =" + jobid + " from the system work Queue.", "BaseThreadClass", "runThread");

                    }
                }
            }
            catch (Exception ex)
            {
                if ((lstObjInQueue.Contains(jobid)))
                {
                    lock (objLockingObject)
                    {
                        lstObjInQueue.Remove(jobid);
                    }

                    objLogger.LogItem("Alert! Error(s) has been reported in a running Thread ID= " + jobid + ". Detail error message is as follows " + ex.Message, "BaseThreadClass", "runThread");
                   
                }
            }

        }

        /// <summary>
        /// Gives details of current CPU utilization.
        /// </summary>
        /// <returns>float</returns>
        private float getCPUUsage()
        {

            string command = @"typeperf ""\Processor(_Total)\% Processor Time"" -sc 1";
            string output = ExecuteCommand(command);
            float cpuUsage = ParseCpuUsage(output);

            objLogger.LogItem("CPU % count is =" + cpuUsage.ToString() + ".", "BaseThreadClass", "runThread");
            Console.WriteLine($"CPU Usage: {cpuUsage}%");

            return cpuUsage;
        }

        private static string ExecuteCommand(string command)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c {command}",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            return output;
        }

        private static float ParseCpuUsage(string output)
        {
            // Split the output into lines.
            string[] lines = output.Split('\n');

            // The CPU usage value is expected on the second line (index 1) after the header.
            if (lines.Length > 1)
            {
                string dataLine = lines[2]; // Get the second line where the data resides.
                string[] parts = dataLine.Split(',');

                if (parts.Length > 1) // Ensure there's at least two elements (date and value)
                {
                    string cpuUsageString = parts[1].Trim('"'); // Trim quotes if present around the CPU usage value.
                    cpuUsageString = cpuUsageString.Replace(@"""", "");

                    if (float.TryParse(cpuUsageString, NumberStyles.Any, CultureInfo.InvariantCulture, out float cpuUsage))
                    {
                        return cpuUsage;
                    }
                }
            }

            return -1; // Return -1 if parsing fails.
        }


        public void Run(string JobID)
        {
            try
            { 
                objLogger.LogItem("Begining of Run job with id is ="+JobID+"  date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "Run");
                

                Task.Run(async () => await TranscriptionVoiceFileAsync(JobID));               

                //jobQueueRepository.UpdateJobQueue(Convert.ToInt32(JobID), null, null, null, (int)JobSteps.Sentiment, null);
                
                objLogger.LogItem("End of Run method with id is ="+JobID+"  date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "Run");

            }
            catch (Exception ex)
            {
                objLogger.LogItem("Exception is caught in Run method for JobID ="+JobID+"  date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "Run");
            }

        }

        public void TranscriptionVoiceFile(string JobID)
        {
            using (ApiClient apiConsumer = new ApiClient(transcriptionAudioURL))
            {
                apiConsumer.Get(transcriptionAudioURL, ("clientid", clientId.ToString()), ("id", JobID));                
            }
        }

        private async Task TranscriptionVoiceFileAsync(string JobID)
        {
            string response="";
            //try
            //{  
            //    using (ApiClient apiConsumer = new ApiClient(transcriptionAudioURL))
            //    {
            //        audioTranscribeTrackerRepository.IncreaseRetryCountAudioTranscribeTracker(Convert.ToInt32(JobID));
            //        response = await apiConsumer.GetAsync(transcriptionAudioURL, ("clientid", clientId.ToString()), ("id", JobID));
            //        objLogger.LogItem(" Reponse from transcript is = "+response.ToString()+ " date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
            //    }
            //}
            //catch (Exception ex)
            //{
            //    objLogger.LogItem("Exception is caught in TranscriptionVoiceFileAsync method for JobID = "+JobID+ " Exception details are "+ex.Message.ToString() +" date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
            //    audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.PreProcessing, null, null, null, null, null, null, null, null);
            //}


            try
            {
                using (ApiClient apiConsumer = new ApiClient(transcriptionAudioURL))
                {
                    audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.Processing, null, null, null, null, null, null, null, null);

                    audioTranscribeTrackerRepository.IncreaseRetryCountAudioTranscribeTracker(Convert.ToInt32(JobID));

                    HttpResponseMessage httpResponse = await apiConsumer.GetAsyncHttpResponse(transcriptionAudioURL, ("clientid", clientId.ToString()), ("id", JobID));

                     response = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        // Handle successful response
                        // For example, update tracker and log success
                        audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.Completed, null, null, null, null, null, null, null, null);
                        objLogger.LogItem("Success response from transcript. Response: " + response + ". Date and time: " + DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
                    }
                    else
                    {
                        // Handle error response
                        // For example, log error and update tracker
                        objLogger.LogItem("Error response from transcript. Status code: " + httpResponse.StatusCode + ". Date and time: " + DateTime.Now.ToString() + ". Complete response is ="+httpResponse.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
                        audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.PreProcessing, null, null, null, null, null, null, null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions
                objLogger.LogItem("Exception is caught in TranscriptionVoiceFileAsync method for JobID = "+JobID+ " Exception details are "+ex.Message.ToString() +" date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
                audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.PreProcessing, null, null, null, null, null, null, null, null);
            }

            //if (!((response.Contains("Request failed") || response.Contains("InternalServerError") || response.Contains("\"status\":\"failure\""))))
            //{
            //    audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.Completed, null, null, null, null, null, null, null, null);
            //    objLogger.LogItem("No error in TranscriptionVoiceFileAsync method for JobID = "+JobID+ " date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
            //}
            //else
            //{
            //    objLogger.LogItem("API Error is caught in TranscriptionVoiceFileAsync method for JobID = "+JobID+ " date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
            //    audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.PreProcessing, null, null, null, null, null, null, null, null);
            //}
        }
    }
}
