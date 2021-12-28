using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShareLoader.Classes;
using ShareLoader.Data;
using ShareLoader.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace ShareLoader
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        //Ich kann das viel besser als du!
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<DownloadContext>();

            services.AddSingleton(DownloadSettings.Load());
            services.AddSingleton<AccountHelper>();
            services.AddSingleton<IHostedService, DownloadManagerService>();
            services.AddSingleton<IHostedService, AccountManagerService>();
            services.AddSingleton<IHostedService, ExtractManagerService>();
            services.AddSingleton<IHostedService, MoveManagerService>();

            services.AddControllersWithViews();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();




            app.UseWebSockets();
            app.Use(async (http, next) =>
            {
                if (http.WebSockets.IsWebSocketRequest)
                {
                    WebSocket webSocket = await http.WebSockets.AcceptWebSocketAsync();
                    while (webSocket.State == WebSocketState.Open)
                    {
                        var token = new System.Threading.CancellationToken();
                        var buffer = new ArraySegment<Byte>(new Byte[4096]);
                        var received = await webSocket.ReceiveAsync(buffer, token);

                        if (received.MessageType == WebSocketMessageType.Text)
                        {
                            var request = System.Text.Encoding.UTF8.GetString(buffer.Array, buffer.Offset, buffer.Count).Trim().Replace("\0", "");
                            SocketHandler.Instance.Handle(webSocket, request);
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


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Downloads}/{action=Index}/{id?}");
            });
        }
    }
}
