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
            private CancellationTokenSource? _cancellationTokenSource;
            private const int MaxRetryAttempts = 3;
            private const int RetryDelayMilliseconds = 1000;
           
        public AzureRelayService(ILogger<AzureRelayService> logger, IAzureRelayServiceHelper helper,IAzureRelayServiceHandler handler)
            {
                _logger = logger;
                _helper = helper;
                _handler = handler;
            // Subscribe to process exit and unhandled exception events
            AppDomain.CurrentDomain.ProcessExit += StopOrShutdownService;
            AppDomain.CurrentDomain.UnhandledException += StopOrShutdownService;


        }

        public async Task<Task> StartAsync(CancellationToken cancellationToken)
        {
            try
            {
                //ResumeAsync(cancellationToken);

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

            if (_cancellationTokenSource is not null)
            {
                _cancellationTokenSource.Cancel();
            }

            // Add any cleanup logic here

            return Task.CompletedTask;
        }
        private void StopOrShutdownService(object? sender, EventArgs? e)
        {
            // Handle abrupt process exit here
            // Perform cleanup and logging as needed
            try
            {
                if (_cancellationTokenSource is not null)
                {
                    _cancellationTokenSource.Cancel();

                   
                }
                // Call StopAsync to perform cleanup and additional stop logic
                StopAsync(CancellationToken.None).Wait(); // Consider async if StopAsync is async
            }
            catch (Exception ex)
            {
                // Handle exceptions and log appropriately
               
                _logger.LogError(ex, "An error occurred during service stop.");
            }
        }


        private async Task BackgroundWorkAsync(CancellationToken cancellationToken)
        {
            int retryCount = 0;
            // Use a semaphore to control concurrency
            int maxConcurrentRequests = 100; // Adjust as needed
            SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrentRequests);

            _cancellationTokenSource = new CancellationTokenSource();
            while (!cancellationToken.IsCancellationRequested && retryCount < MaxRetryAttempts && !_cancellationTokenSource.IsCancellationRequested)
            {
                try
                {
                    var listener = _helper.GetListener();

                    // Subscribe to the status events.
                    listener.Connecting += (o, e) => { Console.WriteLine("Connecting"); };
                    listener.Offline += (o, e) => { Console.WriteLine("Offline"); };
                    listener.Online += (o, e) => { Console.WriteLine("Online"); };

                    // Set up cancellation token for ProcessRequestAsync
                    var cancellationTokenForProcessRequest = _cancellationTokenSource.Token;
                    //HTTP Request handler for handling each Message that comes through Relay 
                    listener.RequestHandler = async (context) =>
                    {
                        // Process each request concurrently using async/await and a semaphore
                        await semaphore.WaitAsync();

                        try
                        {
                            await ProcessRequestAsync(context, cancellationTokenForProcessRequest);
                        }
                        finally
                        {
                            semaphore.Release();
                        }

                    };
                    ;

                    await listener.OpenAsync(cancellationToken);

                    _logger.LogInformation("Azure Relay listener started.");

                   

                   

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

                    // Increment retry count and wait with exponential backoff before retrying
                    retryCount++;
                    int delayMilliseconds = (int)Math.Pow(2, retryCount) * RetryDelayMilliseconds;
                    await Task.Delay(delayMilliseconds, cancellationToken);

                    if (retryCount >= MaxRetryAttempts)
                    {
                        throw new InvalidOperationException("Failed to perform background work after multiple retries.");
                    }
                }
            }

            // If all retry attempts fail, throw the last exception
           
        }


       


        private async Task ProcessRequestAsync(RelayedHttpListenerContext context,CancellationToken cancellationToken)
        {
            try
            {
                await _handler.HandleRequestBasedonTypeAsync(context);

                cancellationToken.ThrowIfCancellationRequested();
            }
            catch (OperationCanceledException)
            {
                // Cancellation requested, gracefully exit
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }

        // Implement the ResumeAsync method to handle resumption logic
        // public  void ResumeAsync(CancellationToken cancellationToken){}

        // Implement the RequestMissingChunksAsync method to request missing chunks from the sender

        // Implement the GetLastReceivedChunkSequence method to retrieve the last successfully received chunk's sequence number from logs or a database

    }


}
