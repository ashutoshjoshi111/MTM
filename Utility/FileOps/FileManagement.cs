using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;
using Repo;
using System.Configuration;
using Logger;
using Logger.NLog;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;

namespace Utility.FileOps
{
    public class FileManagement
    {
        public FileManagement() { }
        voiceFileOps voiceFileOperatoins = new voiceFileOps();
        JobQueueRepository jobQueueRepository = new JobQueueRepository();
        AudioTranscribeRepository audioTransRepository = new AudioTranscribeRepository();
        AudioTranscribeTrackerRepository audioTranscribeTrackerRepository = new AudioTranscribeTrackerRepository();

        Int16 clientId = Convert.ToInt16(ConfigurationManager.AppSettings["clientID"].ToString());
        string FinalDirectory = ConfigurationManager.AppSettings["Done"].ToString();
        Logger.Logger objLogger = new Logger.Logger(ConfigurationManager.AppSettings["LogFile"].ToString());
       

        private string agentId, caseId, discussionType, dateOfDiscussion, uniqueKey;

        public void CopyFilesToNewFolders(string sourceDirectoryPath, string destinationFolderPath)
        {

            if (Directory.Exists(sourceDirectoryPath))
            {
               
                string[] sourceFiles = Directory.GetFiles(sourceDirectoryPath);

                // Filter only .mp3 and .wav files
                string[] filteredFiles = sourceFiles.Where(file =>
                    Path.GetExtension(file).Equals(".mp3", StringComparison.OrdinalIgnoreCase) ||
                    Path.GetExtension(file).Equals(".wav", StringComparison.OrdinalIgnoreCase)).ToArray();


                foreach (string sourceFilePath in filteredFiles)
                {
                    if (!HandleAudioFileNameExistence(sourceFilePath))
                    {
                        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilePath);
                        string newFolderPath = Path.Combine(destinationFolderPath, fileNameWithoutExtension);
                        Directory.CreateDirectory(newFolderPath);

                        string destinationFilePath = Path.Combine(newFolderPath, Path.GetFileName(sourceFilePath));
                        File.Copy(sourceFilePath, destinationFilePath, true);
                        //Console.WriteLine($"File '{Path.GetFileName(sourceFilePath)}' copied successfully to '{newFolderPath}'.");
                        objLogger.LogItem($"File '{Path.GetFileName(sourceFilePath)}' copied successfully to '{newFolderPath}'.", "FileManagement", "CopyFilesToNewFoldres");
                       
                        LoggerService.nLoggerService.LogInfo($"File '{Path.GetFileName(sourceFilePath)}' copied successfully to '{newFolderPath}'.", Path.GetFileName(sourceFilePath), (int)JobSteps.PreStart);

                        //Convert type of the file if necessery
                        var targetFormat = new WaveFormat(16000, 16, 1); // Example: 16 kHz sample rate, 16-bit depth, mono
                        ConvertMp3ToWavIfNecessary(newFolderPath, targetFormat);

                        var wavFile = (Path.GetFileName(sourceFilePath)).Replace("mp3", "wav");

                        PopulateOrgDetails(clientId, wavFile);                        

                        //Insert the details in the database
                        audioTransRepository.InsertAudioTranscribe(clientId, (int)JobStatus.PreProcessing, (int)FileType.wav, wavFile, sourceFilePath, null, null, null, agentId, caseId,discussionType, dateOfDiscussion, uniqueKey, false, 0, true, false);

                        LoggerService.nLoggerService.LogInfo(wavFile +" Details are inserted into database.", wavFile, (int)JobSteps.PreStart);
                        
                        TimeSpan duration;
                        string WavDestFilePath = Path.Combine(newFolderPath, Path.GetFileName(fileNameWithoutExtension+".wav"));

                        // Check if file size exceeds 5 MB and chunk if necessary                   
                        try
                        {

                            using (var reader = new WaveFileReader(WavDestFilePath))
                            {
                                duration = reader.TotalTime;
                            }

                        }
                        catch (Exception ex)
                        {
                            throw ex;
                        }

                        // Check if the duration is greater than 5 minutes
                        if (duration.TotalMinutes > 5)
                        {
                            objLogger.LogItem("Audio duration is more than 5 minutes. Chunking needed for - " + sourceFilePath + " Destination Folder = "+FinalDirectory, "FileManagement", "CopyFilesToNewFoldres");
                            // Invoke chunking logic
                            ChunkWavFile(WavDestFilePath, 300000);
                        }
                        else
                        {
                            int parentFileId = 0;

                            AudioTranscribe transcription = audioTransRepository.GetAudioTranscribeDetails(clientId, wavFile);

                            if (transcription != null)
                            {
                                parentFileId = transcription.Id;
                                //Console.WriteLine($"Transcription ID: {transcription.Id}, Audio File Name: {transcription.AudioFileName}");
                            }
                            objLogger.LogItem("Inserting in table AudioTranscribeTracker for Parent FileID =" + parentFileId + ". The file name is  = "+Path.GetFileName(sourceFilePath), "FileManagement", "CopyFilesToNewFoldres");

                            audioTranscribeTrackerRepository.InsertAudioTranscribeTracker(clientId, parentFileId, (int)JobStatus.PreProcessing, (int)FileType.wav, wavFile, 1, null, destinationFilePath.Replace("mp3", "wav"), null, null, null);
                            objLogger.LogItem("Audio duration is less than 5 minutes. No chunking needed for - " + sourceFilePath + " Destination Folder = "+FinalDirectory, "FileManagement", "CopyFilesToNewFoldres");
                        }

                        //objLogger.LogItem("Inserting in table JobQueue for file = " + wavFile, "FileManagement", "CopyFilesToNewFolders");
                        //jobQueueRepository.InsertJobQueue(wavFile, null, null, (int)JobSteps.PreStart, (int)JobStatus.PreProcessing);
                        //objLogger.LogItem("CopyFileToNewFolderMethod sourceFilePath=" + sourceFilePath + " Destination Folder = "+FinalDirectory, "FileManagement", "CopyFilesToNewFoldres");

                        MoveFile(sourceFilePath, FinalDirectory);

                        LoggerService.nLoggerService.LogInfo(wavFile +" the orignal mp3/wav file is moved final directory.", wavFile, (int)JobSteps.PreStart);

                    }
                }
            }
            else
            {
                Console.WriteLine("Source directory does not exist.");
                objLogger.LogItem("Source directory does not exist." , "FileManagement", "CopyFilesToNewFoldres");

            }
        }


        public void ChunkWavFile(string filePath, int chunkSizeInMillisecond)
        {
            try
            {
                int parentFileId = 0;

                AudioTranscribe transcription = audioTransRepository.GetAudioTranscribeDetails(clientId, Path.GetFileName(filePath));

                if (transcription != null)
                {
                    parentFileId = transcription.Id;
                    //Console.WriteLine($"Transcription ID: {transcription.Id}, Audio File Name: {transcription.AudioFileName}");
                }

                Console.WriteLine("Starting chunking process...");

                // Create a directory for storing the chunks
                string outputDirectory = Path.GetDirectoryName(filePath);
                string outputDirectoryName = Path.GetFileNameWithoutExtension(filePath) + "_Chunks";
                string outputDirectoryPath = outputDirectory;
               

                // Open the input WAV file
                using (var reader = new WaveFileReader(filePath))
                {
                    int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;
                    int bytesPerChunk = chunkSizeInMillisecond * bytesPerMillisecond;

                    int sequence = 1;
                    byte[] buffer = new byte[bytesPerChunk];
                    while (reader.Position < reader.Length)
                    {
                        string chunkFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_Chunk_{sequence}.wav";
                        string chunkFilePath = Path.Combine(outputDirectoryPath, chunkFileName);

                        using (var writer = new WaveFileWriter(chunkFilePath, reader.WaveFormat))
                        {
                            int bytesRead = 0;
                            while (bytesRead < bytesPerChunk && reader.Position < reader.Length)
                            {
                                int bytesToRead = Math.Min(bytesPerChunk - bytesRead, (int)(reader.Length - reader.Position));
                                int read = reader.Read(buffer, 0, bytesToRead);
                                writer.Write(buffer, 0, read);
                                bytesRead += read;
                            }
                        }

                        objLogger.LogItem($"Chunk '{chunkFileName}' (Size: {new FileInfo(chunkFilePath).Length} bytes) created successfully.", "FileManagement", "ChunkWavFile");
                        LoggerService.nLoggerService.LogInfo($"Chunk '{chunkFileName}' (Size: {new FileInfo(chunkFilePath).Length} bytes) created successfully.", chunkFileName, (int)JobSteps.Chunk);

                        audioTranscribeTrackerRepository.InsertAudioTranscribeTracker(clientId, parentFileId, (int)JobStatus.PreProcessing, (int)FileType.wav, chunkFileName, sequence, null, chunkFilePath, null, null, null);
                        sequence++;
                    }
                }
                // No exceptions occurred, delete the original file
                File.Delete(filePath);
                LoggerService.nLoggerService.LogInfo($"After chunking, Orignal file is deleted successfully.", Path.GetFileName(filePath), (int)JobSteps.Chunk);
                
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"Error occurred: {ex.Message}");
                objLogger.LogItem($"Error occurred in file chunking: {ex.Message}", "FileManagement", "ChunkWavFile");
                LoggerService.nLoggerService.LogException("Error occurred in file chunking", ex, Path.GetFileName(filePath), (int)JobSteps.Chunk);
            }
        }


        public void ConvertMp3ToWavIfNecessary(string folderPath, WaveFormat targetFormat)
        {
            try
            {
                string[] audioFiles = Directory.GetFiles(folderPath, "*.mp3", SearchOption.TopDirectoryOnly);
                // Add other file extensions for different audio formats if needed (e.g., "*.wav", "*.aac", etc.)

                foreach (string audioFile in audioFiles)
                {
                    // Check if the file is already in WAV format
                    if (Path.GetExtension(audioFile).Equals(".wav", StringComparison.OrdinalIgnoreCase))
                    {                       
                        objLogger.LogItem($"{Path.GetFileName(audioFile)} is already in WAV format. Skipping conversion.", "FileManagement", "ConvertMp3ToWavIfNecessary");
                        continue;
                    }

                    string wavFilePath = Path.Combine(Path.GetDirectoryName(audioFile), Path.GetFileNameWithoutExtension(audioFile) + ".wav");

                    // Convert MP3 or other voice files to WAV

                    try
                    {
                        voiceFileOperatoins.ConvertMp3ToWav(audioFile, wavFilePath, targetFormat);                        
                    }
                    catch (Exception ex)
                    {
                        objLogger.LogItem("Alert!!! Error in file conversion for "+audioFile, "FileManagement", "ConvertMp3ToWavIfNecessary");
                        
                        var wavFile = (Path.GetFileName(audioFile)).Replace("mp3", "wav");                       
                        PopulateOrgDetails(clientId, wavFile);
                        audioTransRepository.InsertAudioTranscribe(clientId, (int)JobStatus.InvalidJob, (int)FileType.wav, wavFile, audioFile, null, null, null, agentId, caseId,discussionType,dateOfDiscussion,uniqueKey, false, 0, true, false);
                        
                        LoggerService.nLoggerService.LogException("Error occurred in file convesion", ex, audioFile, (int)JobSteps.FileConversion);
                    }


                    try
                    {
                        File.Delete(audioFile);
                        LoggerService.nLoggerService.LogInfo($"After Conversion, Orignal file is deleted successfully.", audioFile, (int)JobSteps.Chunk);

                    }
                    catch (Exception ex)
                    {
                        objLogger.LogItem("Alert!!! Error in file delete for "+audioFile, "FileManagement", "ConvertMp3ToWavIfNecessary");
                        LoggerService.nLoggerService.LogException("Error occurred in file chunking", ex, Path.GetFileName(audioFile).Replace("mp3", "wav"), (int)JobSteps.FileConversion);

                    }
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions                
                objLogger.LogItem("High level Alert!!! Error in file format conversion."+ex.Message.ToString(), "FileManagement", "ConvertMp3ToWavIfNecessary");
                LoggerService.nLoggerService.LogException("High level Alert!!! Error in file format conversion."+ex.Message.ToString(), ex, "FILE-TYPE-CONVERSION", (int)JobSteps.FileConversion);
            }
        }

        public void MoveFile(string sourceFilePath, string destinationFolderPath)
        {
            try
            {
                // Check if the source file exists
                if (!File.Exists(sourceFilePath))
                {
                    objLogger.LogItem($"Source file '{sourceFilePath}' does not exist.", "FileManagement", "MoveFile");

                    Console.WriteLine($"Source file '{sourceFilePath}' does not exist.");
                    return;
                }

                // Create the destination folder if it doesn't exist
                if (!Directory.Exists(destinationFolderPath))
                {
                    objLogger.LogItem($"destination folder '{destinationFolderPath}' does not exist.", "FileManagement", "MoveFile");

                    Directory.CreateDirectory(destinationFolderPath);
                }

                // Get the file name from the source file path
                string fileName = Path.GetFileName(sourceFilePath);

                // Construct the destination file path
                string destinationFilePath = Path.Combine(destinationFolderPath, fileName);

                // Move the file

                if (File.Exists(destinationFilePath))
                {
                    objLogger.LogItem("File alrady exists deleting the same name file.....", "FileManagement", "MoveFile");                    
                    File.Delete(destinationFilePath);

                    objLogger.LogItem("Deleted the same name file.....", "FileManagement", "MoveFile");

                }

                File.Move(sourceFilePath,destinationFilePath);
                Console.WriteLine($"File '{fileName}' moved successfully to '{destinationFolderPath}'.");
                objLogger.LogItem($"File '{fileName}' moved successfully to '{destinationFolderPath}'.", "FileManagement", "MoveFile");

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error moving file: {ex.Message}");
                objLogger.LogItem($"Error moving file: {ex.Message}", "FileManagement", "MoveFile");

            }
        }

        public static string GetValueFromDictionarySafely(string key, Dictionary<string, string> dictionary)
        {
            if (dictionary.TryGetValue(key, out var value))
            {
                return value ?? ""; // If value is found but is null, return an empty string instead
            }
            return ""; // Return an empty string if the key is not found in the dictionary
        }

        public void PopulateOrgDetails(int clientId, string wavFile)
        {
            //Get the details of Organization and Case
            Dictionary<string, string> components = audioTransRepository.GetFileNameComponents(clientId, wavFile);

            // Using the function to safely get values from the dictionary
             agentId = GetValueFromDictionarySafely("AgentID", components);
             caseId = GetValueFromDictionarySafely("CaseID", components);
             discussionType = GetValueFromDictionarySafely("DiscussionType", components);
             dateOfDiscussion = GetValueFromDictionarySafely("Date", components);
             uniqueKey = GetValueFromDictionarySafely("UniqueKey", components);
        }

        public bool HandleAudioFileNameExistence(string sourceFilePath)
        {
            try
            {
                string fileNameWithExtension = Path.GetFileName(sourceFilePath);
                string invalidDirectoryPath = ConfigurationManager.AppSettings["Invalid"].ToString();

                var wavFile = (Path.GetFileName(sourceFilePath)).Replace("mp3", "wav");

                PopulateOrgDetails(clientId, wavFile);                

                //objLogger.LogItem($"Verifying uniqueness for file: "+fileNameWithExtension.Replace("mp3", "wav"), "FileManagement", "HandleAudioFileNameExistence");
                objLogger.LogItem($"Verifying uniqueness for file: "+wavFile, "FileManagement", "HandleAudioFileNameExistence");
                bool exists = audioTransRepository.CheckAudioFileNameExists(fileNameWithExtension.Replace("mp3","wav"));

                if (exists)
                {
                    MoveFile(sourceFilePath, invalidDirectoryPath);
                    objLogger.LogItem($"ALERT!!!!!!Duplicate file: "+fileNameWithExtension.Replace("mp3", "wav"), "FileManagement", "HandleAudioFileNameExistence");
                    wavFile = wavFile.Replace(".", "_0_0_DUP.");
                    audioTransRepository.InsertAudioTranscribe(clientId, (int)JobStatus.DuplicateFileName, (int)FileType.wav, wavFile, sourceFilePath, null, null, null, agentId, caseId, discussionType, dateOfDiscussion, uniqueKey, false, 0, false, false);

                    return true;
                }
            }
            catch (Exception ex)
            {
                objLogger.LogItem($"Error in method HandleAudioFileNameExistence for moving file: {ex.Message}", "FileManagement", "HandleAudioFileNameExistence");
            }

            return false;
        }

    }
}

