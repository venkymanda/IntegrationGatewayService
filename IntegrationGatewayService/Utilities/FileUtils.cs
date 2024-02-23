using IntegrationGatewayService.Models;
using Microsoft.Azure.Relay;
using SampleWorkerApp.Helper;
using SampleWorkerApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationGatewayService.Utilities
{
    public class FileUtils : IFileManipulator
    {
        private readonly ILogger<AzureRelayService> _logger;
        private readonly IAzureRelayServiceHelper _helper;

        // Define a dictionary to store received chunks temporarily
        public Dictionary<Guid, Dictionary<long, byte[]>> partialChunks = new Dictionary<Guid, Dictionary<long, byte[]>>();

        public FileUtils(ILogger<AzureRelayService> logger, IAzureRelayServiceHelper helper)
        {
            _logger = logger;
            _helper = helper;
        }


        public async Task ManipulateFileAsync(RelayedHttpListenerContext context,FileUploadRequestDTO fileUploadRequest, FileUploadRequestHeadersDTO requestHeaders)
        {
            Console.WriteLine("FileUtils is manipulating the file.");
            try
            {
                _logger.LogInformation("Azure Relay listener Received a Message.");

                // Extract metadata from headers or content (same as before)

                long totalFileSize          = requestHeaders.TotalSize;
                string fileId               = requestHeaders.TransactionId; // Unique identifier for the file
                long currentChunkSequence   = requestHeaders.ChunkSequence;
                long chunksize              = requestHeaders.ChunkSize;
                
                string tempDirectory = Path.Combine(Path.GetTempPath(),"ChunkFiles",fileId);

                // Define a unique filename for the chunk based on the fileId and sequence
                string chunkFileName = Path.Combine(tempDirectory, $"{fileId}_{currentChunkSequence}.chunk");

                // Save the received chunk data to the temporary file
                using (FileStream chunkFileStream = File.Create(chunkFileName))
                {
                    byte[] buffer = new byte[chunksize]; // Adjust the buffer size as needed
                    int bytesRead;
                    while ((bytesRead = _helper.Decompress(context.Request.InputStream).Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Write the received chunk to the temporary file
                        chunkFileStream.Write(buffer, 0, bytesRead);
                    }
                }

                // Check if all expected chunks are received to assemble the complete file
                if (CheckIfAllChunksReceived(fileId, totalFileSize,chunksize, tempDirectory))
                {

                    string filePath = fileUploadRequest.DestinationPath; // Specify the file path

                    AssembleCompleteFile(fileId, totalFileSize, tempDirectory, chunksize,filePath);

                    // Cleanup temporary chunk files
                    CleanUpChunks(fileId,tempDirectory);

                    _logger.LogInformation("File received completely.");

                    // Respond with a success message
                    string successMessage = "File received and assembled successfully.";
                    await _helper.WriteToContextResponse(context, successMessage, HttpStatusCode.OK);

                    // All chunks received and processed successfully
                }

                // Respond with a success message for the received chunk
                string successChunkMessage = "File chunk received successfully.";
                await _helper.WriteToContextResponse(context, successChunkMessage, HttpStatusCode.OK);
                // Not all chunks received yet
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing file chunk.");
                // Respond with an error message
                string errorMessage = "Error processing file chunk.";
                await _helper.WriteToContextResponse(context, errorMessage, HttpStatusCode.InternalServerError);
            }
        }


        public void AssembleCompleteFile(string fileId, long totalFileSize,string tempDirectory,long ChunkSize,string outputPath)
        {
            // Create a FileStream to write the output file
            using (FileStream completeFileStream = File.Create(outputPath))
            {

                // Create a list to store temporary chunk files and their sequence numbers
                List<(string FileName, long Sequence)> chunkFiles = new List<(string, long)>();

                // Iterate through the temporary chunk files and add them to the list
                for (long currentChunkSequence = 0; currentChunkSequence < totalFileSize / ChunkSize; currentChunkSequence++)
                {
                    // Define the filename for the current chunk based on fileId and sequence
                    string chunkFileName = Path.Combine(tempDirectory, $"{fileId}_{currentChunkSequence}.chunk");

                    // Add the chunk file and its sequence number to the list
                    chunkFiles.Add((chunkFileName, currentChunkSequence));
                }

                // Sort the list of chunk files based on their sequence numbers
                chunkFiles.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

                // Optionally, decompress the complete file data if it was compressed before sending
                // Implement Decompress later
                // byte[] decompressedFileData = _helper.Decompress(completeFileData);
                // Iterate through the sorted list and write the chunks to the completeFileStream
                foreach (var (chunkFileName, _) in chunkFiles)
                {
                    // Read the chunk data from the file and write it to the completeFileStream
                    using (FileStream chunkFileStream = File.OpenRead(chunkFileName))
                    {
                        byte[] buffer = new byte[8192]; // Adjust the buffer size as needed
                        int bytesRead;
                        while ((bytesRead = chunkFileStream.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            // Write the received chunk to the completeFileStream after decompressing
                            //completeFileStream.Write(_helper.Decompress(buffer), 0, bytesRead);
                            completeFileStream.Write(buffer, 0, bytesRead);
                        }
                    }

                    // Delete the temporary chunk file
                    //File.Delete(chunkFileName);
                }
            }
           
           
        }


        public bool CheckIfAllChunksReceived(string fileId, long totalFileSize, long ChunkSize,string tempDirectory)
        {
            // Calculate the total number of expected chunks based on the total file size
            long totalChunks = (totalFileSize + ChunkSize - 1) / ChunkSize;

            // Path to the directory where chunk files are stored
            string directoryPath = tempDirectory;

            // Count the number of chunk files in the directory
            int receivedChunksCount = Directory.GetFiles(directoryPath, $"{fileId}_*.chunk").Length;

            // Check if the count of received chunks matches the total number of expected chunks
            return receivedChunksCount == totalChunks;
        }

        // Clean up chunks that are no longer needed
        public void CleanUpChunks(string fileId,string tempDirectory)
        {
            foreach (var filePath in Directory.GetFiles(tempDirectory, $"{fileId}_*.chunk"))
            {
                File.Delete(filePath);
            }
        }


    }
}
