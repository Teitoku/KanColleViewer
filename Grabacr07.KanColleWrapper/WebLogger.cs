using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Diagnostics;

namespace Grabacr07.KanColleWrapper
{
    public class WebLogger : Logger
    {
        public string buildItemRoute { get; set; }
        public string buildShipRoute { get; set; }
        public string shipDropRoute { get; set; }

        private HttpClient client;
        internal WebLogger(string uriString, KanColleProxy proxy) : base(proxy)
        {
            client = new HttpClient();
            client.BaseAddress = new Uri(uriString);

        }

        private async Task postLog(string route, string format, params object[] args)
        {
            string data = String.Format(format, args);
            HttpResponseMessage resp = await client.PostAsync(route, new StringContent(data));
            resp.EnsureSuccessStatusCode();
        }

        protected void Log(LogType type, string format, params object[] args)
        {
            if (!this.EnableLogging) return;

            try
            {
                switch (type)
                {
                    case LogType.BuildItem:
                        postLog(buildItemRoute, format, args).Wait();
                        break;
                    case LogType.BuildShip:
                        postLog(buildShipRoute, format, args).Wait();
                        break;
                    case LogType.ShipDrop:
                        postLog(shipDropRoute, format, args).Wait();
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            
        }
    }
}
