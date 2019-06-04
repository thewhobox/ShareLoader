using ShareLoader.Classes;
using ShareLoader.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ShareLoader.Downloader
{
    public interface IDownloader
    {
        bool IsFree { get; }
        string Identifier { get; }
        string UrlIdentifier { get; }

        string GetItemId(string url);

        Task<DownloadItem> GetItemInfo(DownloadItem item);

        Task<Stream> GetDownloadStream(DownloadItem item, AccountProfile profile);
        
        Task<AccountModel> GetAccountInfo(AccountProfile profile);

        Task<AccountProfile> DoLogin(AccountProfile profile);
    }
}
