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
using System.Security.Cryptography;

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
                    string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourceFilePath);
                    string newFolderPath = Path.Combine(destinationFolderPath, fileNameWithoutExtension);
                    Directory.CreateDirectory(newFolderPath);
                    
                    string destinationFilePath = Path.Combine(newFolderPath, Path.GetFileName(sourceFilePath));
                    File.Copy(sourceFilePath, destinationFilePath, true);
                    //Console.WriteLine($"File '{Path.GetFileName(sourceFilePath)}' copied successfully to '{newFolderPath}'.");
                    objLogger.LogItem($"File '{Path.GetFileName(sourceFilePath)}' copied successfully to '{newFolderPath}'." ,"FileManagement", "CopyFilesToNewFoldres");


                    //Convert type of the file if necessery
                    var targetFormat = new WaveFormat(16000, 16, 1); // Example: 16 kHz sample rate, 16-bit depth, mono
                    ConvertMp3ToWavIfNecessary(newFolderPath, targetFormat);

                    var wavFile = (Path.GetFileName(sourceFilePath)).Replace("mp3", "wav");

                    //Get the details of Organization and Case
                    Dictionary<string, string> components = audioTransRepository.GetFileNameComponents(clientId, wavFile);

                    // Using the function to safely get values from the dictionary
                    string agentId = GetValueFromDictionarySafely("AgentID", components);
                    string caseId = GetValueFromDictionarySafely("CaseID", components);
                    string discussionType = GetValueFromDictionarySafely("DiscussionType", components);
                    string date = GetValueFromDictionarySafely("Date", components); 
                    string uniqueKey = GetValueFromDictionarySafely("UniqueKey", components); 

                    //Insert the details in the database
                    audioTransRepository.InsertAudioTranscribe(clientId, (int)JobStatus.PreProcessing,(int)FileType.wav, wavFile, sourceFilePath, null, null, null, agentId,caseId,false,0,true,false);

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

                        audioTranscribeTrackerRepository.InsertAudioTranscribeTracker(clientId, parentFileId, (int)JobStatus.PreProcessing, (int)FileType.wav, wavFile, 1, null, destinationFilePath.Replace("mp3","wav"), null, null, null);
                        objLogger.LogItem("Audio duration is less than 5 minutes. No chunking needed for - " + sourceFilePath + " Destination Folder = "+FinalDirectory, "FileManagement", "CopyFilesToNewFoldres");
                    }

                    objLogger.LogItem("Inserting in table JobQueue for file = " + wavFile , "FileManagement", "CopyFilesToNewFolders");

                    jobQueueRepository.InsertJobQueue(wavFile, null, null, (int)JobSteps.PreStart, (int)JobStatus.PreProcessing);

                    objLogger.LogItem("CopyFileToNewFolderMethod sourceFilePath=" + sourceFilePath + " Destination Folder = "+FinalDirectory, "FileManagement", "CopyFilesToNewFoldres");

                    MoveFile(sourceFilePath,FinalDirectory);
                }
            }
            else
            {
                Console.WriteLine("Source directory does not exist.");
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

                        Console.WriteLine($"Chunk '{chunkFileName}' (Size: {new FileInfo(chunkFilePath).Length} bytes) created successfully.");

                        audioTranscribeTrackerRepository.InsertAudioTranscribeTracker(clientId, parentFileId, (int)JobStatus.PreProcessing, (int)FileType.wav, chunkFileName, sequence, null, chunkFilePath, null, null, null);

                        sequence++;
                    }
                }
                // No exceptions occurred, delete the original file
                File.Delete(filePath);

                Console.WriteLine("Original file deleted.");
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"Error occurred: {ex.Message}");
            }       }


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
                        Console.WriteLine($"{Path.GetFileName(audioFile)} is already in WAV format. Skipping conversion.");
                        continue;
                    }

                    string wavFilePath = Path.Combine(Path.GetDirectoryName(audioFile), Path.GetFileNameWithoutExtension(audioFile) + ".wav");

                    // Convert MP3 or other voice files to WAV
                    voiceFileOperatoins.ConvertMp3ToWav(audioFile, wavFilePath, targetFormat);                   
                    
                    File.Delete(audioFile);
                }
            }
            catch (Exception ex)
            {
                // Log any exceptions
                Console.WriteLine($"Error occurred: {ex.Message}");
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

    }
}

