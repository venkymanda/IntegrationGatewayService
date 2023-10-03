using SampleWorkerApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWorkerApp.Wrapper
{
    public class AzureRelayServiceWrapper : IHostedService
    {
        private readonly IAzureRelayService _azureRelayService;
        private readonly bool _isService;
        private const int MaxRetryAttempts = 3;
        private const int RetryDelayMilliseconds = 1000;

        public AzureRelayServiceWrapper(IAzureRelayService azureRelayService, bool isService)
        {
            _azureRelayService = azureRelayService;
            _isService = isService;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            int retryCount = 0;

            while (retryCount < MaxRetryAttempts)
            {
                try
                {
                    if (_isService)
                    {
                       var taskresult= await _azureRelayService.StartAsync(cancellationToken);
                    }
                    else
                    {
                        var taskresult=await Task.Run(() => _azureRelayService.StartAsync(cancellationToken));
                    }

                    return;
                }
                catch (Exception ex)
                {
                    // Log the exception here if needed
                    retryCount++;
                    await Task.Delay(RetryDelayMilliseconds);
                }
            }

            // If all retry attempts fail, throw the last exception
            throw new InvalidOperationException("Failed to start service after multiple retries.");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _azureRelayService.StopAsync(cancellationToken).Wait(cancellationToken);
            return Task.CompletedTask;
        }
    }


}
