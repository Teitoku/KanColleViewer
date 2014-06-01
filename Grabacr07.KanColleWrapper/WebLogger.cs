using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper.Internal;
using Grabacr07.KanColleWrapper.Models;
using Grabacr07.KanColleWrapper.Models.Raw;
using Livet;
using System.Net.Http;

namespace Grabacr07.KanColleWrapper
{
	public class WebLogger : NotificationObject
	{
		public bool EnableLogging { get; set; }

		private bool waitingForShip;
		private int dockid;
		private readonly int[] shipmats;

        public string WebLoggerUrl { get; set; }
        public string BuildItemRoute { get; set; }
        public string BuildShipRoute { get; set; }
        public string ShipDropRoute { get; set; }

        private HttpClient client;

        private enum LogType
        {
            BuildItem,
            BuildShip,
            ShipDrop
        };

		internal WebLogger(KanColleProxy proxy)
        {
			this.shipmats = new int[5];

			// ちょっと考えなおす
			proxy.api_req_kousyou_createitem.TryParse<kcsapi_createitem>().Subscribe(x => this.CreateItem(x.Data, x.Request));
			proxy.api_req_kousyou_createship.TryParse<kcsapi_createship>().Subscribe(x => this.CreateShip(x.Request));
			proxy.api_get_member_kdock.TryParse<kcsapi_kdock[]>().Subscribe(x => this.KDock(x.Data));
			proxy.api_req_sortie_battleresult.TryParse<kcsapi_battleresult>().Subscribe(x => this.BattleResult(x.Data));
		}
		
		private void CreateItem(kcsapi_createitem item, NameValueCollection req)
		{
			Log(LogType.BuildItem, "{0},{1},{2},{3},{4},{5}",
				item.api_create_flag == 1 ? item.api_slot_item.api_slotitem_id: -1,
				req["api_item1"], req["api_item2"], req["api_item3"], req["api_item4"], DateTime.Now.ToString("M/d/yyyy H:mm"));
		}

		private void CreateShip(NameValueCollection req)
		{
			this.waitingForShip = true;
			this.dockid = Int32.Parse(req["api_kdock_id"]);
			this.shipmats[0] = Int32.Parse(req["api_item1"]);
			this.shipmats[1] = Int32.Parse(req["api_item2"]);
			this.shipmats[2] = Int32.Parse(req["api_item3"]);
			this.shipmats[3] = Int32.Parse(req["api_item4"]);
			this.shipmats[4] = Int32.Parse(req["api_item5"]);
		}

		private void KDock(kcsapi_kdock[] docks)
		{
			foreach (var dock in docks.Where(dock => this.waitingForShip && dock.api_id == this.dockid))
			{
                this.Log(LogType.BuildShip, "{0},{1},{2},{3},{4},{5},{6},{7}", 
                    KanColleClient.Current.Master.Ships[dock.api_created_ship_id].SortId, 
                    this.shipmats[0], 
                    this.shipmats[1], 
                    this.shipmats[2], 
                    this.shipmats[3], 
                    this.shipmats[4], 
                    KanColleClient.Current.Homeport.Organization.Fleets[1].Ships[0].Info.SortId, 
                    DateTime.Now.ToString("s"));
				this.waitingForShip = false;
			}
		}

		private void BattleResult(kcsapi_battleresult br)
		{
			if (br.api_get_ship == null)
				return;

			Log(LogType.ShipDrop, "{0},{1},{2},{3},{4}", KanColleClient.Current.Translations.GetTranslation(br.api_get_ship.api_ship_name, TranslationType.Ships, br),
				KanColleClient.Current.Translations.GetTranslation(br.api_quest_name, TranslationType.OperationMaps, br),
				KanColleClient.Current.Translations.GetTranslation(br.api_enemy_info.api_deck_name, TranslationType.OperationSortie, br),
				br.api_win_rank, DateTime.Now.ToString("s"));
		}

        private async Task PostLog(string route, string format, params object[] args)
        {
            var data = String.Format(format, args);
            var resp = await this.client.PostAsync(route, new StringContent(data));
            resp.EnsureSuccessStatusCode();
        }

        private void Log(LogType type, string format, params object[] args)
        {
            if (!this.EnableLogging) return;

            if(this.client == null)
            {
                if (WebLoggerUrl.IsEmpty()) return;
                this.client = new HttpClient { BaseAddress = new Uri(WebLoggerUrl) };
                this.client.DefaultRequestHeaders.Accept.Clear();
            }

            try
            {
                switch (type)
                {
                    case LogType.BuildItem:
                        this.PostLog(this.BuildItemRoute, format, args).Wait();
                        break;
                    case LogType.BuildShip:
                        this.PostLog(this.BuildShipRoute, format, args).Wait();
                        break;
                    case LogType.ShipDrop:
                        this.PostLog(this.ShipDropRoute, format, args).Wait();
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

