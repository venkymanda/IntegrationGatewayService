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
            public  void WriteToContextResponse(RelayedHttpListenerContext context, string message,
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
        }
    }


