using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleWorkerApp.Services
{
    public interface IAzureRelayService
    {
        Task<Task> StartAsync(CancellationToken cancellationToken);
        Task<Task> StopAsync(CancellationToken cancellationToken);
    }

}
