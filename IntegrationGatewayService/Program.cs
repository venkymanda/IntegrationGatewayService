using SampleWorkerApp.Services;
using SampleWorkerApp.Wrapper;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Linq;
using SampleWorkerApp.Helper;
using System.Reflection;
using IntegrationGatewayService.Extensions;
using IntegrationGatewayService.Utilities;
using IntegrationGatewayService.Services;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {

            //Check if its Service or Console
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            //Dependency Injection
            var hostBuilder = new HostBuilder()
                .ConfigureServices((hostContext, services) =>
                {
                    ConfigureServices(services);

                    services.AddHostedService(provider =>
                    {
                        var service = provider.GetRequiredService<IAzureRelayService>();
                        return new AzureRelayServiceWrapper(service, isService);
                    });
                });

            //Run as Windows Service
            if (isService)
            {
                var host = hostBuilder.UseWindowsService().Build();
                await host.RunAsync();

            }
            //Run as Console App
            else
            {
                var host = hostBuilder.Build();
                await host.RunAsync();


            }
        }

        catch (Exception ex)
        {
            //Console.WriteLine($"An exception occurred: {ex.Message}");
            // Optionally, perform any cleanup or logging here
        }
        finally
        {
            //Console.WriteLine("Press any key to exit...");
            //Console.ReadKey();
        }
    }


    public static void ConfigureServices(IServiceCollection services)
    {
        // Add configuration
        IConfiguration configuration = new ConfigurationBuilder()
            .SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)

            .Build();

        services.AddSingleton(configuration);

        // Add logging
        services.AddLogging(builder =>
        {
            builder.AddConsole();
        });

        services.AddFileManipulators();
        services.AddTransient<FileManipulatorTypeManager>();
        // Add services
        services.AddSingleton<IAzureRelayService, AzureRelayService>();
        services.AddScoped<IAzureRelayServiceHelper, AzureRelayServiceHelper>();
        services.AddScoped<IAzureRelayServiceHandler, AzureRelayServiceHandler>();
    }

}
