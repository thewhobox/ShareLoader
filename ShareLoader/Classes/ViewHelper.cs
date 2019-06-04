using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace ShareLoader.Classes
{
    public class ViewHelper
    {

        public static string MenuCompact(HttpContext context)
        {
            return context.Request.Cookies["menuIsCompact"] == "true" ? "compacted" : "";
        }
    }
}
