using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Grabacr07.KanColleWrapper.Internal;
using Grabacr07.KanColleWrapper.Models;
using Grabacr07.KanColleWrapper.Models.Raw;
using Livet;

namespace Grabacr07.KanColleWrapper
{
    public class Logger : NotificationObject
    {
        public bool EnableLogging { get; set; }

        private bool waitingForShip;
        private int dockid;
        private readonly int[] shipmats;
        private int secretary;

        protected enum LogType
        {
            Invalid,
            BuildItem,
            BuildShip,
            ShipDrop
        };

        protected class LogItem
        {
            public LogItem()
            {
                Time = DateTime.Now;
            }

            public DateTime Time { get; set; }
            //TODO: Probably needs a better way of doing this. Reflection vs abstract properties?
            public virtual LogType Type()
            {
                return LogType.Invalid;
            }

            /// <summary>
            /// Create a CSV serialization of the current class.
            /// </summary>
            /// <returns>A csv string with serialized items. </returns>
            public string ToCsv()
            {
                PropertyInfo[] properties = this.GetType().GetProperties();
                var sb = new StringBuilder();
                foreach (var prp in properties)
                {
                    sb.Append(prp.GetValue(this, null)).Append(',');
                }
                sb.Length--;
                return sb.ToString();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <returns></returns>
            public string CsvTitle()
            {
                PropertyInfo[] properties = this.GetType().GetProperties();
                var sb = new StringBuilder();
                foreach (var prp in properties)
                {
                    if (prp.CanRead)
                    {
                        sb.Append(prp.Name).Append(',');
                    }
                }
                sb.Length--;
                return sb.ToString();
            }
        }

        protected class BuildItem : LogItem
        {
            public override LogType Type()
            {
                return LogType.BuildItem;
            }
            public string Result { get; set; }
            public string Secretary { get; set; }
            public string Fuel { get; set; }
            public string Ammo { get; set; }
            public string Steel { get; set; }
            public string Bauxite { get; set; }
        }

        protected class BuildShip : LogItem
        {
            public override LogType Type()
            {
                return LogType.BuildShip;
            }
            public string Result { get; set; }
            public int Secretary { get; set; }
            public int Fuel { get; set; }
            public int Ammo { get; set; }
            public int Steel { get; set; }
            public int Bauxite { get; set; }
            public int BuildMaterials { get; set; }
        }

        protected class ShipDrop : LogItem
        {
            public override LogType Type()
            {
                return LogType.ShipDrop;
            }
            public string Result { get; set; }
            public string Operation { get; set; }
            public string EnemyFleet { get; set; }
            public string Rank { get; set; }
        }

        internal Logger(KanColleProxy proxy)
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
            var logitem = new BuildItem
            {
                Result = item.api_create_flag == 1 ? KanColleClient.Current.Master.SlotItems[item.api_slot_item.api_slotitem_id].Name : "Penguin",
                Fuel = req["api_item1"],
                Ammo = req["api_item2"],
                Steel = req["api_item3"],
                Bauxite = req["api_item4"],
            };
            Log(logitem);
        }

        private void CreateShip(NameValueCollection req)
        {
            this.waitingForShip = true;
            this.secretary = KanColleClient.Current.Homeport.Organization.Fleets[0].Ships[0].Info.SortId;
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
                var logitem = new BuildShip
                {
                    Result = KanColleClient.Current.Master.Ships[dock.api_created_ship_id].Name,
                    Secretary = this.secretary;
                    Fuel = this.shipmats[0],
                    Ammo = this.shipmats[1],
                    Steel = this.shipmats[2],
                    Bauxite = this.shipmats[3],
                    BuildMaterials = this.shipmats[4]

                };
                Log(logitem);
                this.waitingForShip = false;
            }
        }

        private void BattleResult(kcsapi_battleresult br)
        {
            if (br.api_get_ship == null)
                return;
            var logitem = new ShipDrop
            {
                Result = KanColleClient.Current.Translations.GetTranslation(br.api_get_ship.api_ship_name, TranslationType.Ships, br),
                Operation = KanColleClient.Current.Translations.GetTranslation(br.api_quest_name, TranslationType.OperationMaps, br),
                EnemyFleet = KanColleClient.Current.Translations.GetTranslation(br.api_enemy_info.api_deck_name, TranslationType.OperationSortie, br),
                Rank = br.api_win_rank
            };
            Log(logitem);
        }

        protected virtual void Log(LogItem item)
        {
            if (!this.EnableLogging) return;

            try
            {
                string mainFolder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

                string logpath;
                switch (item.Type())
                {
                    case LogType.BuildItem:
                        logpath = mainFolder + "\\ItemBuildLog.csv";
                        break;
                    case LogType.BuildShip:
                        logpath = mainFolder + "\\ShipBuildLog.csv";
                        break;
                    case LogType.ShipDrop:
                        logpath = mainFolder + "\\DropLog.csv";
                        break;
                    default:
                        return;
                }
                if (!File.Exists(logpath))
                {
                    using (var w = File.AppendText(logpath))
                    {
                        w.WriteLine(item.CsvTitle());
                    }
                }
                using (var w = File.AppendText(logpath))
                {
                    w.WriteLine(item.ToCsv());
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}
