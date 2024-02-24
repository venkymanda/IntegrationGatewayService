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
using System.IO.Compression;

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

        
        public async Task WriteToContextResponse(RelayedHttpListenerContext context, string message, HttpStatusCode statusCode = HttpStatusCode.OK)
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
                await context.Response.CloseAsync().ConfigureAwait(false); // Item 1, Item 5
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while writing to the response.");
                // Handle the error as needed
            }
        }

        // Decompression method (customize as needed)
        /*public byte[] Decompress(Stream compressedData)
        {
            GZipStream decompressionStream = null;
            try
            {
                using (Stream compressedStream = new MemoryStream())
                {
                    byte[] buffer = new byte[4096];
                    int bytesRead;

                    while ((bytesRead = compressedData.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        compressedStream.Write(buffer, 0, bytesRead);
                    }

                    using (MemoryStream decompressedStream = new MemoryStream())
                    {
                        using (decompressionStream = new GZipStream(compressedStream, CompressionMode.Decompress))
                        {
                            decompressionStream.CopyTo(decompressedStream);
                        }

                        // Reset the position of the decompressed stream to the beginning
                        decompressedStream.Position = 0;
                       
                        return decompressedStream.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                // Handle decompression error
                Console.WriteLine("Error during decompression: " + ex.Message);
                throw; // Re-throw the exception to propagate it to the caller
            }
            finally
            {
                decompressionStream?.Dispose(); // Ensure the stream is disposed
            }
        }*/

        public  byte[] Decompress(Stream gzipStream)
        {
            using (MemoryStream decompressedStream = new MemoryStream())
            {
                using (GZipStream decompressionStream = new GZipStream(gzipStream, CompressionMode.Decompress))
                {
                    decompressionStream.CopyTo(decompressedStream);
                }
                return TrimNullBytes(decompressedStream.ToArray());
            }
        }

        private static byte[] TrimNullBytes(byte[] bytes)
        {
            int length = bytes.Length;
            while (length > 0 && bytes[length - 1] == 0)
            {
                length--;
            }
            byte[] trimmedBytes = new byte[length];
            Array.Copy(bytes, trimmedBytes, length);
            return trimmedBytes;
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
                            if (uploadRequest != null)
                            {
                                return (headersDTO, uploadRequest);
                            }
                            else { return (headersDTO, new FileUploadRequestDTO()); }
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


