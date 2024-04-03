#region Import namesapces
using System.Data;
using System.Configuration;
using System.Diagnostics;
using System.Management;
using System.Collections.Concurrent;
using Repo;
using ClientWrapper;
using System.Net.Http;

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
                            objLogger.LogItem(" System Putting job Id= " + ds.Tables[0].Rows[i]["Id"].ToString() + " into Thread Pool.", "BaseThreadClass", "runThread");

                            objJobid = ds.Tables[0].Rows[i]["Id"].ToString();

                            lock (lstObjLock) // Lock lstObjInQueue to prevent concurrent modifications
                            {
                                
                                if (objJobid != null && !lstObjInQueue.Contains(objJobid))
                                {
                                    //Checking CPU Utilization
                                    if (Convert.ToInt16(getCPUUsage()) > CpuUtilizationLimit)
                                    {
                                        objLogger.LogItem(" CPU utilization Alert!!!!!!! Current CPU Usages is exceeding " + CpuUtilizationLimit.ToString() + "%. New Threads could not start. ", "BaseThreadClass", "runThread");
                                        break;
                                    }

                                    ThreadPool.QueueUserWorkItem(callBack, (object)ds.Tables[0].Rows[i]["Id"].ToString());

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
        /// <returns>double</returns>
        private double getCPUUsage()
        {

            //ManagementObject processor = new ManagementObject("Win32_PerfFormattedData_PerfOS_Processor.Name='_Total'");
            //processor.Get();
            //return double.Parse(processor.Properties["PercentProcessorTime"].Value.ToString());
            return 50.77;
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
            try
            {  
                using (ApiClient apiConsumer = new ApiClient(transcriptionAudioURL))
                {
                    audioTranscribeTrackerRepository.IncreaseRetryCountAudioTranscribeTracker(Convert.ToInt32(JobID));
                    response = await apiConsumer.GetAsync(transcriptionAudioURL, ("clientid", clientId.ToString()), ("id", JobID));
                    objLogger.LogItem(" Reponse from transcript is = "+response.ToString()+ " date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
                }
            }
            catch (Exception ex)
            {
                objLogger.LogItem("Exception is caught in TranscriptionVoiceFileAsync method for JobID = "+JobID+ " Exception details are "+ex.Message.ToString() +" date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
                audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.PreProcessing, null, null, null, null, null, null, null, null);
            }


            if (!((response.Contains("Request failed") || response.Contains("InternalServerError") || response.Contains("\"status\":\"failure\""))))
            {
                audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.Completed, null, null, null, null, null, null, null, null);
                objLogger.LogItem("No error in TranscriptionVoiceFileAsync method for JobID = "+JobID+ " date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
            }
            else
            {
                objLogger.LogItem("API Error is caught in TranscriptionVoiceFileAsync method for JobID = "+JobID+ " date and time is "+DateTime.Now.ToString(), "BaseThreadClass", "TranscriptionVoiceFileAsync");
                audioTranscribeTrackerRepository.UpdateAudioTranscribeTracker(Convert.ToInt32(JobID), clientId, null, (int)JobStatus.PreProcessing, null, null, null, null, null, null, null, null);

            }

        }
    }
}
