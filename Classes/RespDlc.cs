using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class RespDlc
    {
        public RespDlcSuccess success { get; set; }
        public RespDlcError form_errors { get; set; }
    }

    public class RespDlcSuccess
    {
        public string message { get; set; }
        public List<string> links { get; set; }
    }

    public class RespDlcError
    {
        public List<string> content { get; set; }
        public List<string> __all__ { get; set; }
    }
}
