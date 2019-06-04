using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ShareLoader.Classes;
using ShareLoader.Data;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Http;
using ShareLoader.Manager;

namespace ShareLoader
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {

            //optionsBuilder.UseMySql("server=teamserver;userid=admin;password=Mein#pw#mysqladmin;database=shareloader;");
            services.AddDbContext<DownloadContext>(options =>
                options.UseMySql(Configuration.GetConnectionString("MySQLConnection")));


            services.AddSingleton(DownloadSettings.Load());
            services.AddSingleton<AccountHelper>();
            services.AddSingleton<IHostedService, DownloadManagerService>();
            services.AddSingleton<IHostedService, AccountManagerService>();
            services.AddSingleton<IHostedService, ExtractManagerService>();
            services.AddSingleton<IHostedService, MoveManagerService>();

            services.AddMvc();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            //if (env.IsDevelopment())
            //{
            //    app.UseDeveloperExceptionPage();
            //    app.UseBrowserLink();
            //}
            //else
            //{
            //    app.UseExceptionHandler("/Home/Error");
            //}

            app.UseDeveloperExceptionPage();

            app.UseStaticFiles();
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


            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Downloads}/{action=Index}/{id?}");
            });
        }
    }
}
