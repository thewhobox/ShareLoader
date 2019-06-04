using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using Microsoft.Net.Http.Server;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ShareLoaderHelper
{
    class Program
    {
        public static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));

            string confPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");
            string config = File.ReadAllText(confPath);
            Config conf = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(config);

            if (conf.ConnectionString == null || conf.ConnectionString == "")
            {
                Console.WriteLine("Es wurde noch kein ConnectionString angegeben.");
                Console.WriteLine("Bitte gib ihn jetzt ein:");
                conf.ConnectionString = Console.ReadLine();
                System.IO.File.WriteAllText(confPath, Newtonsoft.Json.JsonConvert.SerializeObject(conf));

                Main(args);
            }
            else
            {
                if (isService)
                {
                    var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
                    var pathToContentRoot = Path.GetDirectoryName(pathToExe);
                    Directory.SetCurrentDirectory(pathToContentRoot);
                }


                var builder = BuildWebHost(args.Where(arg => arg != "--console").ToArray());
                var host = builder.Build();

                if (isService)
                {
                    // To run the app without the CustomWebHostService change the
                    // next line to host.RunAsService();
                    host.RunAsService();
                }
                else
                {
                    host.Run();
                }

            }
        }

        public static IWebHostBuilder BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseUrls("http://localhost:9666/");
    }
}
