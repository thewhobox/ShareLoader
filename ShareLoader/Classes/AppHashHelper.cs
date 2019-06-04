using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShareLoader.Classes
{
    public class AppHashHelper
    {
        public static AppHash GenerateNewSecret()
        {
            char[] chars = { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H',
            'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P',
            'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X',
            'Y', 'Z', '2', '3', '4', '5', '6', '7' };
            
            string output = "";
            AppHash code = new AppHash();
            Random rnd = new Random();

            for(int i = 0; i < 16; i++)
                output += chars[rnd.Next(0, chars.Count() - 1)];

            code.Secret = output;
            return code;
        }
    }
}
