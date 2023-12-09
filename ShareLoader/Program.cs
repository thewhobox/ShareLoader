using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<ShareLoader.Data.DownloadContext>();
builder.Services.AddSingleton<IHostedService, ShareLoader.Background.BackgroundTasks>();

using(ShareLoader.Data.DownloadContext context = new ShareLoader.Data.DownloadContext())
{
    try{
        context.Database.Migrate();
    } catch{}
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseWebSockets();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.UseMiddleware<ShareLoader.Classes.AutoFileMiddleware>();

app.Use(async (http, next) =>
{
    if (http.WebSockets.IsWebSocketRequest)
    {
        System.Net.WebSockets.WebSocket webSocket = await http.WebSockets.AcceptWebSocketAsync();
        while (webSocket.State == System.Net.WebSockets.WebSocketState.Open)
        {
            var token = new System.Threading.CancellationToken();
            var buffer = new ArraySegment<Byte>(new Byte[4096]);
            var received = await webSocket.ReceiveAsync(buffer, token);

            if (received.MessageType == System.Net.WebSockets.WebSocketMessageType.Text)
            {
                var request = System.Text.Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count).Trim().Replace("\0", "");
                ShareLoader.Classes.SocketHelper.Instance.Handle(webSocket, request);
            }
            else
            {
                await next();
            }
        }
    }
    else
    {
        await next();
    }
});

app.Run();
