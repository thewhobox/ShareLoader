using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using System.Globalization;
using System.Threading.Tasks;

namespace ShareLoader.Classes;

public class AutoFileMiddleware
{
    private readonly RequestDelegate _next;

    public AutoFileMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext, IWebHostEnvironment environment)
    {
        string controller = httpContext.Request.RouteValues["controller"]?.ToString().ToLower();
        string action = httpContext.Request.RouteValues["action"]?.ToString().ToLower();

        if(!string.IsNullOrEmpty(controller) && !string.IsNullOrEmpty(action))
        {
            if(System.IO.File.Exists(System.IO.Path.Combine(environment.WebRootPath, "css", $"{controller}.{action}.css")))
                httpContext.Items.Add("style", $"/css/{controller}.{action}.css");
            if(System.IO.File.Exists(System.IO.Path.Combine(environment.WebRootPath, "js", $"{controller}.{action}.js")))
                httpContext.Items.Add("script", $"/js/{controller}.{action}.js");
        }

        await _next(httpContext); // calling next middleware
    }
}