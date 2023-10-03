using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Relay;
using System;
using System.IO;
using System.Net;

namespace SampleWorkerApp.Helper
{
   
        public interface IAzureRelayServiceHelper
    {

        public TokenProvider GetTokenProvider(string keyName = null, string key = null);


        public  string GetTokenAuthString();


        public HybridConnectionListener GetListener();

        public void WriteJsonToContextResponse(RelayedHttpListenerContext context, string json);

        public string ReadContextRequest(RelayedHttpListenerContext context);
           
        }
    }


