using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper.Internal;
using Grabacr07.KanColleWrapper.Models;
using Grabacr07.KanColleWrapper.Models.Raw;
using Livet;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization;


namespace Grabacr07.KanColleWrapper
{
	public class WebLogger : NotificationObject
	{
		public bool EnableLogging { get; set; }

		private bool waitingForShip;
		private int dockid;
		private readonly int[] shipmats;

        public string buildItemRoute { get; set; }
        public string buildShipRoute { get; set; }
        public string shipDropRoute { get; set; }

        private HttpClient client;

        private enum LogType
        {
            BuildItem,
            BuildShip,
            ShipDrop
        };

		internal WebLogger(string uriString, KanColleProxy proxy)
		{
            client = new HttpClient();
            client.BaseAddress = new Uri(uriString);
            client.DefaultRequestHeaders.Accept.Clear();

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
				this.Log(LogType.BuildShip, "{0},{1},{2},{3},{4},{5},{6}", KanColleClient.Current.Master.Ships[dock.api_created_ship_id].SortId, this.shipmats[0], this.shipmats[1], this.shipmats[2], this.shipmats[3], this.shipmats[4], DateTime.Now.ToString("s"));
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

        private async Task postLog(string route, string format, params object[] args)
        {
            string data = String.Format(format, args);
            HttpResponseMessage resp = await client.PostAsync(route, new StringContent(data));
            resp.EnsureSuccessStatusCode();
        }

        private void Log(LogType type, string format, params object[] args)
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

