using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using Codeplex.Data;

namespace Grabacr07.KanColleWrapper
{
	public class WebLogger : Logger
	{
	    private string _WebLoggerUrl;
	    public string WebLoggerUrl
	    {
	        get { return _WebLoggerUrl; }
	        set
	        {
	            _WebLoggerUrl = value;
                if (_WebLoggerUrl.IsEmpty())
                {
                    Debug.WriteLine("Failed to initialize WebLogger. No URL specified. WebLogger disabled until KCV restart.");
                    this.EnableLogging = false;
                    return;
                }
                try
                {
                    this.client = new HttpClient { BaseAddress = new Uri(_WebLoggerUrl) };
                    this.client.DefaultRequestHeaders.Accept.Clear();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    this.EnableLogging = false;
                }
	        }
	    }

	    public string BuildItemRoute { get; set; }
        public string BuildShipRoute { get; set; }
        public string ShipDropRoute { get; set; }

        private HttpClient client;

	    internal WebLogger(KanColleProxy proxy) : base(proxy)
	    {}

	    private async Task PostLog(string route, LogItem item)
	    {
	        var data = DynamicJson.Serialize(item);
            var resp = await this.client.PostAsync(route, new StringContent(data));
            resp.EnsureSuccessStatusCode();
        }

	    protected override void Log(LogItem item)
        {
            if (!this.EnableLogging) return;
	        if (this.client == null)
	            Debug.WriteLine("Attempted to WebLog before WebClient is initialized");

            try
            {
                switch (item.Type())
                {
                    case LogType.BuildItem:
                        this.PostLog(this.BuildItemRoute, item).Wait();
                        break;
                    case LogType.BuildShip:
                        this.PostLog(this.BuildShipRoute, item).Wait();
                        break;
                    case LogType.ShipDrop:
                        this.PostLog(this.ShipDropRoute, item).Wait();
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

