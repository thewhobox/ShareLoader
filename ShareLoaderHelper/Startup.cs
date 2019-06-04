using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.WindowsAPICodePack.Shell.PropertySystem;
using MS.WindowsAPICodePack.Internal;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Downloader;
using ShareLoader.Models;
using ShareLoaderHelper.ShellHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ShareLoaderHelper
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }
        private DownloadContext _context;
        private int groupId = 0;
        private static string AppId = "ShareLoaderHelper.App";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            string config = File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json"));
            Config conf = Newtonsoft.Json.JsonConvert.DeserializeObject<Config>(config);

            var optionsBuilder = new DbContextOptionsBuilder<DownloadContext>(); ;
            optionsBuilder.UseMySql(conf.ConnectionString);
            _context = new DownloadContext(optionsBuilder.Options);
        }

        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            ShortCutCreator.TryCreateShortcut(AppId, "ShareLoaderHelper");

            app.Run(async context =>
            {
                switch (context.Request.Path.Value)
                {
                    case "/jdcheck.js":
                        context.Response.StatusCode = 200;
                        context.Response.ContentType = "text/html";
                        StreamWriter wr = new StreamWriter(context.Response.Body);
                        wr.Write("var jdownloader = true; var version = '34065';");
                        wr.Flush();
                        break;

                    case "/flash/addcrypted2":
                        StreamReader sr = new StreamReader(context.Request.Body);

                        string[] paras = sr.ReadToEnd().Split('&');
                        Dictionary<string, string> parameter = new Dictionary<string, string>();
                        foreach (string para in paras)
                        {
                            string[] temp = para.Split('=');
                            parameter.Add(temp[0], System.Web.HttpUtility.UrlDecode(temp[1]));
                        }

                        string packageName;
                        parameter.TryGetValue("package", out packageName);
                        if (packageName == null)
                            packageName = "Undefined";
                        
                        ScriptEngine engine = new ScriptEngine("jscript");
                        ParsedScript parsed = engine.Parse(parameter["jk"]);
                        string hexkey = parsed.CallMethod("f").ToString();

                        byte[] dBytes = StringToByteArray(hexkey);
                        

                        string decrypted = Decrypt(parameter["crypted"], dBytes).Replace("\0", "");
                        if(decrypted.EndsWith(Environment.NewLine))
                            decrypted = decrypted.Substring(0, decrypted.Length -2);
                        
                        //ShowTextToast("ShareLoader", "Es wurde " + decrypted.Split(Environment.NewLine).Length + " Link(s) gefunden");
                        //Console.WriteLine(decrypted.Split(Environment.NewLine).Length + " Link(s) gefunden");

                        DownloadGroup group = new DownloadGroup();
                        group.Name = packageName;
                        group.IsTemp = true;
                        
                        foreach(string itemStr in decrypted.Split(Environment.NewLine))
                        {
                            IDownloader down = DownloadHelper.GetDownloader(itemStr);
                            DownloadItem item = new DownloadItem();
                            item.Url = itemStr;
                            item.ShareId = down.GetItemId(itemStr);
                            item = await down.GetItemInfo(item);

                            group.Items.Add(item);
                        }


                        group = DownloadHelper.SortGroups(group);
                        _context.Groups.Add(group);
                        _context.SaveChanges();
                        groupId = group.ID;

                        ShowTextToast("ShareLoader", "Die Gruppe wurde hinzugefügt.\r\nEs wurden " + group.Items.Count + " Links gefunden.");
                        context.Response.StatusCode = 200;
                        break;

                    default:
                        context.Response.StatusCode = 404;
                        break;
                }
            });
        }
        
        public static byte[] StringToByteArray(String hex)
        {
            int NumberChars = hex.Length / 2;
            byte[] bytes = new byte[NumberChars];
            using (var sr = new StringReader(hex))
            {
                for (int i = 0; i < NumberChars; i++)
                    bytes[i] =
                      Convert.ToByte(new string(new char[2] { (char)sr.Read(), (char)sr.Read() }), 16);
            }
            return bytes;
        }

        private static string Decrypt(string input, byte[] Key)
        {
            byte[] cypher = Convert.FromBase64String(input);

            var sRet = "";

            using (var rj = new RijndaelManaged())
            {
                try
                {
                    rj.Padding = PaddingMode.Zeros;
                    rj.Mode = CipherMode.CBC;
                    rj.KeySize = 128;
                    rj.BlockSize = 128;
                    rj.Key = Key;
                    rj.IV = Key;
                    var ms = new MemoryStream(cypher);

                    using (var cs = new CryptoStream(ms, rj.CreateDecryptor(Key, Key), CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            sRet = sr.ReadToEnd();
                        }
                    }
                }
                finally
                {
                    rj.Clear();
                }
            }

            return sRet;
        }
        
        private void ShowTextToast(string title, string message)
        {
            XmlDocument toastXml = ToastNotificationManager.GetTemplateContent(ToastTemplateType.ToastText02);

            // Fill in the text elements
            XmlNodeList stringElements = toastXml.GetElementsByTagName("text");
            stringElements[0].AppendChild(toastXml.CreateTextNode(title));
            stringElements[1].AppendChild(toastXml.CreateTextNode(message));

            // Create the toast and attach event listeners
            ToastNotification toast = new ToastNotification(toastXml);

            toast.Activated += Toast_Activated;

            // Show the toast. Be sure to specify the AppUserModelId
            // on your application's shortcut!
            ToastNotificationManager.CreateToastNotifier(AppId).Show(toast);
        }

        private void Toast_Activated(ToastNotification sender, object args)
        {
            string url = "https://shareloader.ipv10.de/Downloads/Edit/" + groupId;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}"));
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                Process.Start("xdg-open", url);
        }
    }
}
