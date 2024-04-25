using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class SocketItem
    {
        public WebSocket Socket { get; set; }
        public int Id { get; set; }
        public int Gid { get; set; }
        public string SubscribeKeys { get; set; }

        public SocketItem(WebSocket socket, int id, int gid, string keys)
        {
            Socket = socket;
            Id = id;
            Gid = gid;
            SubscribeKeys = keys;
        }
    }
}
