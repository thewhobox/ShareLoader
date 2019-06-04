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

        public AccountProfile(AccountModel m)
        {
            Model = m;
            
            Client = new HttpClient();
        }
    }
}
