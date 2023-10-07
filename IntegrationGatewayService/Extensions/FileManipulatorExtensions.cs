using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using IntegrationGatewayService.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace IntegrationGatewayService.Extensions
{
   

    public static class FileManipulatorExtensions
    {
        public static IServiceCollection AddFileManipulators(this IServiceCollection services)
        {
            services.AddTransient<IFileManipulator, FileUtils>();
           
            // Add other implementations as needed.

            return services;
        }
    }

}
