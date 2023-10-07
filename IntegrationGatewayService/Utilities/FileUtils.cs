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


        public void ManipulateFile(RelayedHttpListenerContext context)
        {
            Console.WriteLine("FileUtils is manipulating the file.");
            try
            {
                _logger.LogInformation("Azure Relay listener Received a Message.");

                // Extract metadata from headers or content (same as before)
                long chunkStart = _helper.GetChunkStart(context);
                long totalFileSize = _helper.GetTotalFileSize(context);
                Guid fileId = _helper.GetFileId(context); // Unique identifier for the file
                long currentChunkSequence = _helper.GetChunkSequence(context);
                long chunksize = 8192;

                string tempDirectory = Path.GetTempPath();

                // Define a unique filename for the chunk based on the fileId and sequence
                string chunkFileName = Path.Combine(tempDirectory, $"{fileId}_{currentChunkSequence}.chunk");

                // Save the received chunk data to the temporary file
                using (FileStream chunkFileStream = File.Create(chunkFileName))
                {
                    byte[] buffer = new byte[8192]; // Adjust the buffer size as needed
                    int bytesRead;
                    while ((bytesRead = context.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Write the received chunk to the temporary file
                        chunkFileStream.Write(buffer, 0, bytesRead);
                    }
                }

                // Check if all expected chunks are received to assemble the complete file
                if (CheckIfAllChunksReceived(fileId, totalFileSize,chunksize))
                {

                    string filePath = "path/to/your/output/file.ext"; // Specify the file path

                    AssembleCompleteFile(fileId, totalFileSize, tempDirectory, chunksize,filePath);

                    // Cleanup temporary chunk files
                    CleanUpChunks(fileId,tempDirectory);

                    _logger.LogInformation("File received completely.");

                    // Respond with a success message
                    string successMessage = "File received and assembled successfully.";
                    _helper.WriteToContextResponse(context, successMessage, HttpStatusCode.OK);

                    // All chunks received and processed successfully
                }

                // Respond with a success message for the received chunk
                string successChunkMessage = "File chunk received successfully.";
                _helper.WriteToContextResponse(context, successChunkMessage, HttpStatusCode.OK);
                // Not all chunks received yet
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing file chunk.");
                // Respond with an error message
                string errorMessage = "Error processing file chunk.";
                _helper.WriteToContextResponse(context, errorMessage, HttpStatusCode.InternalServerError);
            }
        }


        public void AssembleCompleteFile(Guid fileId, long totalFileSize,string tempDirectory,long ChunkSize,string outputPath)
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


        public bool CheckIfAllChunksReceived(Guid fileId, long totalFileSize,long ChunkSize)
        {
            // Calculate the total number of expected chunks based on the total file size
            long totalChunks = (totalFileSize + ChunkSize - 1) / ChunkSize;

            // Get the list of received chunk sequence numbers for the specified fileId
            var receivedChunkSequences = partialChunks.ContainsKey(fileId)
                ? partialChunks[fileId].Keys.ToList()
                : new List<long>();

            // Check if all expected chunks have been received
            for (long expectedSequence = 0; expectedSequence < totalChunks; expectedSequence++)
            {
                if (!receivedChunkSequences.Contains(expectedSequence))
                {
                    return false; // Not all chunks have been received
                }
            }

            return true; // All expected chunks have been received
        }


        // Clean up chunks that are no longer needed
        public void CleanUpChunks(Guid fileId,string tempDirectory)
        {
            foreach (var filePath in Directory.GetFiles(tempDirectory, $"{fileId}_*.chunk"))
            {
                File.Delete(filePath);
            }
        }


    }
}
