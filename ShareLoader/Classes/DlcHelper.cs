using Microsoft.AspNetCore.Http;
using ShareLoader.Downloader;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ShareLoader.Classes
{
    public class DlcHelper
    {
        public async static Task<AddDlcModel> ParseDlc(Stream dlcStream)
        {
            StreamReader s = new StreamReader(dlcStream);
            string content = await s.ReadToEndAsync();
            s.Dispose();

            string dlc_key = content.Substring(content.Length - 88);
            string dlc_data = content.Substring(0, content.Length - 88);

            HttpClient client = new HttpClient();
            string resp = "";
            try
            {
                resp = await client.GetStringAsync("http://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&data=" + dlc_key);
            } catch
            {
                return null;
            }

            string dlc_enc_key = resp.Substring(4, resp.Length - 9);
            string dlc_dec_key = Decrypt(dlc_enc_key);

            string links_enc = Decrypt(dlc_data, dlc_dec_key).Replace("\0", "");
            string links_dec = UTF8Encoding.Default.GetString(Convert.FromBase64String(links_enc));

            XDocument xml = XDocument.Parse(links_dec);
            XElement package = xml.Element("dlc").Element("content").Element("package");
            UTF8Encoding encoding = new UTF8Encoding();
            DownloadGroup group = new DownloadGroup();
            IDownloader downloader;

            foreach (XElement file in package.Elements("file"))
            {
                string fileUrl = encoding.GetString(Convert.FromBase64String(file.Element("url").Value));
                downloader = DownloadHelper.GetDownloader(fileUrl);

                if(downloader == null)
                {
                    continue;
                }

                DownloadItem item = new DownloadItem();
                item.Name = encoding.GetString(Convert.FromBase64String(file.Element("filename").Value));
                item.ShareId = downloader.GetItemId(fileUrl);
                item.Hoster = downloader.Identifier;
                item.Url = fileUrl;

                item = await downloader.GetItemInfo(item);

                

                group.Items.Add(item);
            }

            group = DownloadHelper.SortGroups(group);

            AddDlcModel output = new AddDlcModel();
            output.Name = package.Attribute("name") != null ? encoding.GetString(Convert.FromBase64String(package.Attribute("name").Value)).Trim() : "";
            output.Password = "";
            
            foreach (DownloadItem item in group.Items)
            {
                if (!output.Items.ContainsKey(item.GroupID))
                    output.Items.Add(item.GroupID, new AddDlcGroupModel());

                output.Items[item.GroupID].Items.Add(item);
            }


            return output;
        }

        private static string Decrypt(string input)
        {
            var encoding = new UTF8Encoding();
            return Decrypt(input, encoding.GetBytes("cb99b5cbc24db398"), encoding.GetBytes("9bc24cb995cb8db3"));
        }

        private static string Decrypt(string input, string key)
        {
            var encoding = new UTF8Encoding();
            return Decrypt(input, encoding.GetBytes(key), encoding.GetBytes(key));
        }

        private static string Decrypt(string input, byte[] Key, byte[] IV)
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
                    rj.IV = IV;
                    var ms = new MemoryStream(cypher);

                    using (var cs = new CryptoStream(ms, rj.CreateDecryptor(Key, IV), CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            sRet = sr.ReadLine();
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
    }
}
