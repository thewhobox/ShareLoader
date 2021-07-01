using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Classes.Exceptions
{
    public class NoPremiumException : Exception
    {
        public NoPremiumException(string message = "No Premium Account") : base(message) { }
    }

    public class LoginException : Exception
    {
        public LoginException(string message = "Login did not work") : base(message) { }
    }

    public class LoopOverflowException : Exception
    {
        public LoopOverflowException(string message = "Too many loops") : base(message) { }
    }
    
    public class NoDownloadException : Exception
    {
        public NoDownloadException(string message = "No Downloadlink found") : base(message) { }
    }
}
