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
using IntegrationGatewayService.Services;

namespace SampleWorkerApp.Services
{
   
        public class AzureRelayService : IAzureRelayService
        {
            private readonly ILogger<AzureRelayService> _logger;
            private readonly IAzureRelayServiceHelper _helper;
            private readonly IAzureRelayServiceHandler   _handler;
            private CancellationTokenSource _cancellationTokenSource;
            private const int MaxRetryAttempts = 3;
            private const int RetryDelayMilliseconds = 1000;
            // Define a dictionary to store received chunks temporarily
            private Dictionary<Guid, Dictionary<long, byte[]>> partialChunks = new Dictionary<Guid, Dictionary<long, byte[]>>();

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
                    await ResumeAsync(cancellationToken);

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

                    // Subscribe to the status events.
                    listener.Connecting += (o, e) => { Console.WriteLine("Connecting"); };
                    listener.Offline += (o, e) => { Console.WriteLine("Offline"); };
                    listener.Online += (o, e) => { Console.WriteLine("Online"); };

                    //HTTP Request handler for handling each Message that comes through Relay 
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


       


        private void ProcessRequest(RelayedHttpListenerContext context)
        {
           _handler.HandleRequestBasedonType(context);
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
