using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using System;
using System.IO;
using System.Net;
using SampleWorkerApp.Services;
using IntegrationGatewayService.Models;
using Azure.Core;
using Newtonsoft.Json;
using System.Transactions;

namespace SampleWorkerApp.Helper
{
   
        public class AzureRelayServiceHelper:IAzureRelayServiceHelper
        {
            private readonly ILogger<AzureRelayService> _logger;
            private readonly IConfiguration _configuration;

            public AzureRelayServiceHelper(ILogger<AzureRelayService> logger, IConfiguration configuration)
            {
                _logger = logger;
                _configuration = configuration.GetSection("AzureRelaySettings");
            }
            public  TokenProvider GetTokenProvider(string keyName = null, string key = null)
            {
                    string kn = keyName ?? _configuration["KeyName"];
                    string k = key ?? _configuration["Key"];
                    var result = TokenProvider.CreateSharedAccessSignatureTokenProvider(kn, k);
                    return result;
            }

            public  string GetTokenAuthString()
            {
               
                var uri = new Uri(string.Format("https://{0}/{1}", _configuration["RelayNamespace"], _configuration["ConnectionName"]));
                var result = (GetTokenProvider()
                            .GetTokenAsync(uri.AbsoluteUri, TimeSpan.FromHours(1))).Result.TokenString;
                return result;
            }

            public  HybridConnectionListener GetListener()
            {
                // Load settings from configuration
                  
                string keyName = _configuration["KeyName"];
                string key = _configuration["Key"];
           

                var listener = new HybridConnectionListener
                        (new Uri(string.Format("sb://{0}/{1}", _configuration["RelayNamespace"], _configuration["ConnectionName"])),
                        GetTokenProvider());

                return listener;

            }
            public  void WriteJsonToContextResponse(RelayedHttpListenerContext context, string json)
            {
                context.Response.Headers.Add("content-type", "application/json");
                using (var sw = new StreamWriter(context.Response.OutputStream))
                {
                    sw.WriteLine(json);
                }
            }
            public  void WriteToContextResponseV2(RelayedHttpListenerContext context, string message,
                HttpStatusCode statusCode = HttpStatusCode.OK)
            {
                context.Response.StatusCode = statusCode;
                using (var sw = new StreamWriter(context.Response.OutputStream))
                {
                    sw.WriteLine(message);
                }
            }
            public  string ReadContextRequest(RelayedHttpListenerContext context)
            {
                try
                {
                    StreamReader sr = new StreamReader(context.Request.InputStream);
                    var content = sr.ReadToEnd();
                    return content;
                }
                catch (Exception)
                {

                    return null;
                }
            }

            public long GetChunkStart(RelayedHttpListenerContext context)
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

        public long GetTotalFileSize(RelayedHttpListenerContext context)
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
        public Guid GetFileId(RelayedHttpListenerContext context)
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


        // Get the sequence number from headers or content (customize as needed)
        public long GetChunkSequence(RelayedHttpListenerContext context)
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

        // Decompression method (customize as needed)
        public byte[] Decompress(byte[] compressedData)
        {
            // Implement decompression logic here
            // Example: Use a compression library like GZipStream or deflate algorithm

            // Return the decompressed data
            return compressedData; // Placeholder for decompression logic
        }

        public (IRequestHeaders, IInputRequest) ExtractRequestFromContext(RelayedHttpListenerContext context)
        {
            string requestTypeHeader = context.Request.Headers["X-RequestType"] ?? string.Empty;

            if (!string.IsNullOrEmpty(requestTypeHeader))
            {
                var inputRequestJson = context.Request.Headers["X-InputRequest"];
                if (!string.IsNullOrEmpty(inputRequestJson))
                {
                    switch (requestTypeHeader)
                    {
                        case "UploadFile":
                            var uploadRequest = JsonConvert.DeserializeObject<FileUploadRequestDTO>(inputRequestJson);
                            var headersDTO = new FileUploadRequestHeadersDTO()
                            {
                                BlobName = context.Request.Headers["X-Filename"] ?? string.Empty,
                                ChunkSequence = int.TryParse(context.Request.Headers["X-ChunkSequence"], out int chunkSeq) ? chunkSeq : 0,
                                TotalChunks = int.TryParse(context.Request.Headers["X-TotalChunks"], out int totalChunks) ? totalChunks : 0,
                                TotalSize = long.TryParse(context.Request.Headers["X-TotalSize"], out long totalSize) ? totalSize : 0,
                                TransactionId = context.Request.Headers["X-TransactionID"] ?? string.Empty,
                                ChunkSize = int.TryParse(context.Request.Headers["X-ChunkSize"], out int chunkSize) ? chunkSize : 8192, // Default Value
                                RequestType=requestTypeHeader

                            };

                            return (headersDTO, uploadRequest);
                        // Handle other request types as needed
                        default:
                            // Handle unsupported request types
                            // You might consider logging or raising an exception here for unhandled types
                            break;
                    }
                }
                else
                {
                    // Handle the case where "X-InputRequest" is missing or invalid
                    // You might consider logging or raising an exception here
                }
            }
            else
            {
                // Handle the case where "X-RequestType" is missing or empty
                // You might consider logging or raising an exception here
            }

            // Handle invalid or missing data by returning default values
            return (null, null);
        }



    }
}


