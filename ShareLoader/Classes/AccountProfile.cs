using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class AccountProfile
    {
        public HttpClient Client { get; set; }
        public AccountModel Model { get; set; }
        public HttpClientHandler Handler { get; set; }

        public AccountProfile(AccountModel m)
        {
            Model = m;

            Handler = new HttpClientHandler();
            Handler.AllowAutoRedirect = false;

            Client = new HttpClient(Handler);
        }
    }
}
