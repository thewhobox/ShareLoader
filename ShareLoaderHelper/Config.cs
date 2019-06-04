using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ShareLoaderHelper
{
    public class Config
    {
        [JsonProperty("connectionString")]
        public string ConnectionString { get; set; }
    }
}
