using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Sego_and__Bux.Infrastructure
{
    public sealed class ServerTimeHeaderMiddleware
    {
        private const string HeaderName = "X-Server-UTC";
        private readonly RequestDelegate _next;

        public ServerTimeHeaderMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            context.Response.OnStarting(() =>
            {
                // ISO 8601 in UTC, e.g. 2025-09-03T18:42:23.411Z
                context.Response.Headers[HeaderName] = DateTime.UtcNow.ToString("o");
                return Task.CompletedTask;
            });

            await _next(context);
        }
    }

    public static class ServerTimeHeaderExtensions
    {
        public static IApplicationBuilder UseServerTimeHeader(this IApplicationBuilder app)
            => app.UseMiddleware<ServerTimeHeaderMiddleware>();
    }
}
