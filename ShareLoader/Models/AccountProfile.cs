

namespace ShareLoader.Models;

public class AccountProfile
{
    public System.Net.CookieContainer cookieContainer = new System.Net.CookieContainer();
    public HttpClient Client { get; set; }
    public AccountModel Model { get; set; }
    public HttpClientHandler Handler { get; set; }
    public bool IsLoggedIn { get; set; } = false;

    public AccountProfile(AccountModel m)
    {
        Model = m;

        Handler = new HttpClientHandler() { CookieContainer = cookieContainer };
        Handler.AllowAutoRedirect = false; //TODO check if neccesarry

        Client = new HttpClient(Handler);
    }
}