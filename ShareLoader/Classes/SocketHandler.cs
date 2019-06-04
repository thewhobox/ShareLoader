using Microsoft.EntityFrameworkCore;
using ShareLoader.Data;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class SocketHandler
    {
        public List<SocketItem> Sockets { get; set; } = new List<SocketItem>();

        

        private static SocketHandler _instance;
        public static SocketHandler Instance
        {
            get
            {
                if (_instance == null) _instance = new SocketHandler();
                return _instance;
            }
        }

        public void Handle(WebSocket socket, string request)
        {
            var paras = request.Split(';');

            switch (paras[0])
            {
                case "register":
                    Sockets.Add(new SocketItem() { Socket = socket, Id = int.Parse(paras[1]), Gid = int.Parse(paras[2]), SubscribeKeys = paras[3] });
                    break;
            }
        }



        public async Task SendText(string text, DownloadItem item, string key)
        {
            await SendUpdate(text, item.ID, item.DownloadGroupID, key);
        }


        #region ItemDownload
        public async Task SendIDReset(DownloadItem item)
        {
            await SendUpdate("{ \"type\": \"reset\", \"id\": \"" + item.ID.ToString() + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }

        public async Task SendIDCheck(DownloadItem item)
        {
            await SendUpdate("{ \"type\": \"check\", \"id\": \"" + item.ID.ToString() + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }

        public async Task SendIDPercentage(DownloadItem item, double averageRead, string perc)
        {
            double averageRaw = averageRead / 1024 / 1024;
            string[] format = new string[] { "{0} bytes/s", "{0} KB/s", "{0} MB/s", "{0} GB/s", "{0} TB/s", "{0} PB/s", "{0} EB/s" };
            int i = 0;
            while (i < format.Length && averageRead >= 1024)
            {
                averageRead = (long)(100 * averageRead / 1024) / 100.0;
                i++;
            }

            await SendUpdate("{ \"type\": \"info\", \"id\": \"" + item.ID.ToString() + "\", \"perc\": \"" + perc + "\", \"average\": " + averageRead.ToString().Replace(',', '.') + ", \"speed\": \"" + string.Format(format[i], averageRead) + "\", \"stamp\": \"" + DateTime.Now.ToString() + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }

        public async Task SendIDError(DownloadItem item)
        {
            await SendUpdate("{ \"type\": \"error\", \"id\": \"" + item.ID.ToString() + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }

        public async Task SendIDDownloaded(DownloadItem item)
        {
            await SendUpdate("{ \"type\": \"downloaded\", \"id\": \"" + item.ID + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }

        public async Task SendIDExtract(DownloadItem item, string perc = "0")
        {
            await SendUpdate("{ \"type\": \"extract\", \"id\": \"" + item.ID + "\", \"perc\": \"" + perc + "\", \"group\": \"" + item.GroupID + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }

        public async Task SendIDExtracted(DownloadItem item)
        {
            await SendUpdate("{ \"type\": \"extracted\", \"id\": \"" + item.GroupID + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }

        public async Task SendIDFinish(DownloadItem item)
        {
            await SendUpdate("{ \"type\": \"fin\", \"id\": \"" + item.GroupID + "\" }", item.ID, item.DownloadGroupID, "downinfo");
        }
        #endregion

        #region Account
        public async Task SendAITrafficDay(AccountModel item)
        {
            await SendUpdate("{ \"type\": \"trafficday\", \"id\": \"" + item.ID + "\", \"value\": \"" + item.TrafficLeft + "\", \"stamp\": \"" + DateTime.Now + "\" }", item.ID, 0, "accountinfo");
        }
        public async Task SendAITrafficWeek(AccountModel item)
        {
            await SendUpdate("{ \"type\": \"trafficweek\", \"id\": \"" + item.ID + "\", \"value\": \"" + item.TrafficLeftWeek + "\", \"stamp\": \"" + DateTime.Now + "\" }", item.ID, 0, "accountinfo");
        }
        #endregion

        public async Task SendUpdate(string message, int id, int gid, string key)
        {
            List<SocketItem> sockets = Sockets.Where(s => s.SubscribeKeys.Split('.').Contains(key) && ((s.Id == id || s.Gid == gid) || (s.Id == -1 && s.Gid == -1))).ToList();
            List<SocketItem> toRemove = new List<SocketItem>();
            

            foreach (SocketItem item in sockets)
            {
                if(item.Socket.State != WebSocketState.Open)
                {
                    toRemove.Add(item);
                    continue;
                }
                var data = System.Text.Encoding.UTF8.GetBytes(message);
                var buffer = new ArraySegment<Byte>(data);
                await item.Socket.SendAsync(buffer, WebSocketMessageType.Text, true, new System.Threading.CancellationToken());
            }

            foreach (SocketItem item in toRemove)
            {
                Sockets.Remove(item);
            }
        }
    }
}
