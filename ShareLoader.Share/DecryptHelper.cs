namespace ShareLoader.Share;

using System.Xml.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

public class DecryptHelper
{
    public static string Decrypt(byte[] data, string key, int keySize = 256)
    {
        string rawLinks;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.KeySize = keySize;
            aesAlg.Key = System.Text.Encoding.UTF8.GetBytes(key);
            aesAlg.IV = System.Text.Encoding.UTF8.GetBytes(key);
            aesAlg.Padding = PaddingMode.None;
            aesAlg.Mode = CipherMode.CBC;
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(data))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        rawLinks = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        Regex rgx = new Regex("\u0000+$");
        rawLinks = rgx.Replace(rawLinks, "");
        return rawLinks;
    }

    public static string Decrypt(byte[] data, string key, string iv)
    {
        string rawLinks;
        using (Aes aesAlg = Aes.Create())
        {
            aesAlg.Padding = PaddingMode.Zeros;
            aesAlg.Mode = CipherMode.CBC;
            aesAlg.KeySize = 128;
            aesAlg.BlockSize = 128;
            aesAlg.Key = System.Text.Encoding.UTF8.GetBytes(key);
            aesAlg.IV = System.Text.Encoding.UTF8.GetBytes(iv);
            ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

            using (MemoryStream msDecrypt = new MemoryStream(data))
            {
                using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                    {
                        rawLinks = srDecrypt.ReadToEnd();
                    }
                }
            }
        }

        Regex rgx = new Regex("\u0000+$");
        rawLinks = rgx.Replace(rawLinks, "");
        return rawLinks;
    }

    private static string Decrypt(string input)
    {
        return Decrypt(Convert.FromBase64String(input), "cb99b5cbc24db398", "9bc24cb995cb8db3");
    }

    public static CheckViewModel DecryptContainer(string content)
    {
        string dlc_key = content.Substring(content.Length - 88);
        string dlc_data = content.Substring(0, content.Length - 88);

        HttpClient client = new HttpClient();
        var webRequest = new HttpRequestMessage(HttpMethod.Get, "http://service.jdownloader.org/dlcrypt/service.php?srcType=dlc&destType=pylo&data=" + dlc_key);
        var response = client.Send(webRequest);

        using var reader = new StreamReader(response.Content.ReadAsStream());
        string resp = reader.ReadToEnd();
        
        string dlc_enc_key = resp.Substring(4, resp.Length - 9);
        string dlc_dec_key = Decrypt(dlc_enc_key);

        string links_enc = Decrypt(Convert.FromBase64String(dlc_data), dlc_dec_key, 128).Replace("\0", "");
        string links_dec = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(links_enc));


        XDocument xml = XDocument.Parse(links_dec);
        XElement? package = xml.Element("dlc")?.Element("content")?.Element("package");
        if (package == null)
        {
            return new CheckViewModel();
        }

        List<string> urls = new List<string>();
        foreach (XElement file in package.Elements("file"))
        {
            XElement? urlEle = file.Element("url");
            if(urlEle == null)
                continue;
            string fileUrl = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(urlEle.Value));
            urls.Add(fileUrl);
        }

        
        CheckViewModel model = new CheckViewModel() 
        {
            Name = package.Attribute("name") != null ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(package.Attribute("name")?.Value ?? "Unknown")).Trim() : "",
            RawLinks = string.Join(',', urls)
        };
        SearchHelper.GetSearch(model);

        return model;
    }
}