using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using Microsoft.Extensions.Configuration;
using SampleWorkerApp.Helper;
using System.Net;
using Newtonsoft.Json;

namespace SampleWorkerApp.Services
{
   
        public class AzureRelayService : IAzureRelayService
        {
            private readonly ILogger<AzureRelayService> _logger;
            private readonly IAzureRelayServiceHelper _helper;
            private CancellationTokenSource _cancellationTokenSource;
            private const int MaxRetryAttempts = 3;
            private const int RetryDelayMilliseconds = 1000;

        public AzureRelayService(ILogger<AzureRelayService> logger, IAzureRelayServiceHelper helper)
            {
                _logger = logger;
                _helper = helper;
            // Subscribe to process exit and unhandled exception events
            AppDomain.CurrentDomain.ProcessExit += StopOrShutdownService;
            AppDomain.CurrentDomain.UnhandledException += StopOrShutdownService;


        }

            public async Task<Task> StartAsync(CancellationToken cancellationToken)
            {
                try
                {
                    ResumeAsync(cancellationToken);

                    _cancellationTokenSource = new CancellationTokenSource();

                    await Task.Run(async () =>
                    {
                        await BackgroundWorkAsync(_cancellationTokenSource.Token);
                    });

                    return Task.CompletedTask;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException(ex.Message);
                    //return Task.FromException(ex);
                }
            }

            public Task StopAsync(CancellationToken cancellationToken)
            {
                _cancellationTokenSource.Cancel();

                // Add any cleanup logic here

                return Task.CompletedTask;
            }
        private void StopOrShutdownService(object sender, EventArgs e)
        {
            // Handle abrupt process exit here
            // Perform cleanup and logging as needed
            try
            {
                // Save partialChunks data to a file
                SavePartialChunksToFile();
            }
            catch (Exception ex)
            {
                // Handle exceptions and log appropriately
            }
        }

        private async Task BackgroundWorkAsync(CancellationToken cancellationToken)
        {
            int retryCount = 0;

            while (retryCount < MaxRetryAttempts)
            {
                try
                {
                    var listener = _helper.GetListener();
                    listener.RequestHandler = ProcessRequest;

                    await listener.OpenAsync(cancellationToken);

                    _logger.LogInformation("Azure Relay listener started.");

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        // Perform background processing here

                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                    }

                    await listener.CloseAsync(cancellationToken);

                    // If everything succeeded, break out of the loop
                    break;
                }
                catch (OperationCanceledException)
                {
                    // Cancellation requested, gracefully exit
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while processing requests.");

                    // Increment retry count and wait before retrying
                    retryCount++;
                    await Task.Delay(RetryDelayMilliseconds);
                    if (retryCount >= MaxRetryAttempts)
                    {
                        throw new InvalidOperationException("Failed to perform background work after multiple retries.");
                    }
                }
            }

            // If all retry attempts fail, throw the last exception
           
        }


        // Define a dictionary to store received chunks temporarily
        public Dictionary<Guid, Dictionary<long, byte[]>> partialChunks = new Dictionary<Guid, Dictionary<long, byte[]>>();


        private void ProcessRequest(RelayedHttpListenerContext context)
        {
            try
            {
                _logger.LogInformation("Azure Relay listener Received a Message.");

                // Extract metadata from headers or content (same as before)
                long chunkStart = GetChunkStart(context);
                long totalFileSize = GetTotalFileSize(context);
                Guid fileId = GetFileId(context); // Unique identifier for the file
                long currentChunkSequence = GetChunkSequence(context);
                

                // Define a buffer to read chunks of the file
                byte[] buffer = new byte[8192]; // You can adjust the buffer size as needed

                // Read the chunk data into a byte array
                using (MemoryStream chunkData = new MemoryStream())
                {
                    int bytesRead;
                    while ((bytesRead = context.Request.InputStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        // Write the received chunk to the in-memory buffer
                        chunkData.Write(buffer, 0, bytesRead);
                    }

                    // Check if the dictionary entry for the file exists
                    if (!partialChunks.ContainsKey(fileId))
                    {
                        partialChunks[fileId] = new Dictionary<long, byte[]>();
                    }

                    // Store the chunk data using the sequence number as the key
                    partialChunks[fileId][currentChunkSequence] = chunkData.ToArray();
                }

                // Check if all expected chunks are received to assemble the complete file
                if (partialChunks.ContainsKey(fileId) && partialChunks[fileId].Count >= totalFileSize)
                {
                    // Assemble the complete file from the received chunks
                   
                    byte[] completeFileData = new byte[totalFileSize];
                    foreach (var entry in partialChunks[fileId].OrderBy(kvp => kvp.Key))
                    {
                        // fileStream.Write(entry.Value, 0, entry.Value.Length);
                        byte[] chunkData = entry.Value;
                        long chunkStartSequence = entry.Key;
                        Array.Copy(chunkData, 0, completeFileData, chunkStartSequence, chunkData.Length);
                    }

                    // Optionally, decompress the complete file data if it was compressed before sending
                    byte[] decompressedFileData = Decompress(completeFileData);

                    // Save the decompressed data to a file
                    string filePath = "path/to/your/output/file.ext"; // Specify the file path
                    File.WriteAllBytes(filePath, decompressedFileData);

                    // Remove the assembled chunks and the expected sequence number
                    partialChunks.Remove(fileId);

                    

                    // The entire file has been received and assembled
                    _logger.LogInformation("File received completely.");
                }

                // Respond with a success message
                string successMessage = "File chunk received successfully.";
                WriteToContextResponse(context, successMessage, HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while processing file chunk.");
                // Respond with an error message
                string errorMessage = "Error processing file chunk.";
                WriteToContextResponse(context, errorMessage, HttpStatusCode.InternalServerError);
            }
        }

        // Decompression method (customize as needed)
        private byte[] Decompress(byte[] compressedData)
        {
            // Implement decompression logic here
            // Example: Use a compression library like GZipStream or deflate algorithm

            // Return the decompressed data
            return compressedData; // Placeholder for decompression logic
        }

        // Get the sequence number from headers or content (customize as needed)
        private long GetChunkSequence(RelayedHttpListenerContext context)
        {
            // Extract and return the sequence number from headers or content
            // Example: Get from "X-Chunk-Sequence" header
            string sequenceHeader = context.Request.Headers.Get("X-Chunk-Sequence");
            if (!string.IsNullOrEmpty(sequenceHeader) && long.TryParse(sequenceHeader, out long sequence))
            {
                return sequence;
            }
            return 0;
        }
        private long GetChunkStart(RelayedHttpListenerContext context)
        {
            // Extract and return the chunk start position from headers or content
            // Example: Get from "Content-Range" header
            string contentRangeHeader = context.Request.Headers.Get("Content-Range");
            if (!string.IsNullOrEmpty(contentRangeHeader))
            {
                // Parse the content range header to get the start position
                // Example: "bytes 0-999/5000"
                var parts = contentRangeHeader.Split(' ');
                if (parts.Length > 1)
                {
                    var rangeParts = parts[1].Split('-');
                    if (rangeParts.Length > 0)
                    {
                        if (long.TryParse(rangeParts[0], out long start))
                        {
                            return start;
                        }
                    }
                }
            }
            return 0;
        }

        private long GetTotalFileSize(RelayedHttpListenerContext context)
        {
            // Extract and return the total file size from headers or content
            // Example: Get from "Content-Range" header
            string contentRangeHeader = context.Request.Headers.Get("Content-Range");
            if (!string.IsNullOrEmpty(contentRangeHeader))
            {
                // Parse the content range header to get the total file size
                // Example: "bytes 0-999/5000"
                var parts = contentRangeHeader.Split('/');
                if (parts.Length > 1)
                {
                    if (long.TryParse(parts[1], out long totalSize))
                    {
                        return totalSize;
                    }
                }
            }
            return 0;
        }

        // Get the fileId from headers or content (customize as needed)
        private Guid GetFileId(RelayedHttpListenerContext context)
        {
            // Extract and return the fileId from headers or content
            // Example: Get from "X-File-Id" header
            string fileIdHeader = context.Request.Headers.Get("X-File-Id");
            if (!string.IsNullOrEmpty(fileIdHeader) && Guid.TryParse(fileIdHeader, out Guid fileId))
            {
                return fileId;
            }

            // If fileId is not found in headers, you may need to extract it from content or other sources.
            // Example: Extract from JSON content
            // string content = ReadContextRequest(context);
            // Implement fileId extraction logic from content.

            // If fileId cannot be determined, you can return a default Guid or throw an exception.
            // For demonstration, we return Guid.Empty as a default.
            return Guid.Empty;
        }


        public void WriteToContextResponse(RelayedHttpListenerContext context, string message, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            try
            {
                // Set the HTTP status code
                context.Response.StatusCode = (HttpStatusCode)(int)statusCode;

                // Write the message to the response stream
                using (var sw = new StreamWriter(context.Response.OutputStream))
                {
                    sw.WriteLine(message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while writing to the response.");
                // Handle the error as needed
            }
        }

        // A method to handle the resumption logic
        //public async Task ResumeAsync(CancellationToken cancellationToken)
        //{
        //    try
        //    {
        //        // Read logs or database records to determine the last successfully received chunk for each file

        //        //foreach (var fileIdentifier in filesToResume)
        //        //{
        //        //    long lastReceivedChunkSequence = GetLastReceivedChunkSequence(fileIdentifier);
        //        //    long expectedSequence = lastReceivedChunkSequence + 1;

        //        //    // Request missing chunks from the sender starting from expectedSequence
        //        //    await RequestMissingChunksAsync(fileIdentifier, expectedSequence, cancellationToken);
        //        //}
        //    }
        //    catch (Exception ex)
        //    {
        //        // Handle exceptions and log appropriately
        //    }
        //}

        // A method to save the partialChunks data to a file
        private void SavePartialChunksToFile()
        {
            try
            {
                // Serialize the partialChunks dictionary to a file (e.g., using JSON serialization)
                string filePath = "path/to/partialChunks.json"; // Specify the file path
                string jsonData = JsonConvert.SerializeObject(partialChunks);
                File.WriteAllText(filePath, jsonData);
            }
            catch (Exception ex)
            {
                // Handle exceptions and log appropriately
            }
        }

        // Implement the ResumeAsync method to handle resumption logic
        public async Task ResumeAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Read the saved partialChunks data from the file (if it exists)
                string filePath = "path/to/partialChunks.json"; // Specify the file path
                if (File.Exists(filePath))
                {
                    string jsonData = File.ReadAllText(filePath);
                    partialChunks = JsonConvert.DeserializeObject<Dictionary<Guid, Dictionary<long, byte[]>>>(jsonData);
                }

                // Continue with resumption logic as before
            }
            catch (Exception ex)
            {
                // Handle exceptions and log appropriately
            }
        }

        // Implement the RequestMissingChunksAsync method to request missing chunks from the sender

        // Implement the GetLastReceivedChunkSequence method to retrieve the last successfully received chunk's sequence number from logs or a database

    }


}
