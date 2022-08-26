using ShareLoader.Models;

namespace ShareLoader.Manager;

public interface IDownloadManager
{
    string Identifier { get; }

    bool Check(string url);
    string GetItemId(string url);
    Task<ItemModel> GetItemInfo(string Id);
    Task<Stream> GetDownloadStream(object item);
    Task GetAccounInfo(AccountProfile acc);
    Task<bool> DoLogin(AccountProfile acc);
}