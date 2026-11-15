using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace SobraBoa.API.Infrastructure;

public static class RateLimitingSetup
{
    public static IServiceCollection AddRateLimitingSetup(this IServiceCollection s, IConfiguration cfg)
    {
        var g = cfg.GetSection("RateLimiting:Global");
        var a = cfg.GetSection("RateLimiting:Auth");

        s.AddRateLimiter(o =>
        {
            o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // limite global: por usuário autenticado; se anônimo, por IP
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(http =>
            {
                var particao = http.User.Identity?.IsAuthenticated == true
                    ? $"user:{http.User.GetUserId()}"
                    : $"ip:{http.Connection.RemoteIpAddress}";
                return RateLimitPartition.GetFixedWindowLimiter(particao, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = g.GetValue("PermitLimit", 100),
                    Window = TimeSpan.FromSeconds(g.GetValue("WindowSeconds", 60)),
                    QueueLimit = 0
                });
            });

            // política nomeada para login/registro
            o.AddPolicy("auth", http => RateLimitPartition.GetFixedWindowLimiter(
                $"auth:{http.Connection.RemoteIpAddress}",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = a.GetValue("PermitLimit", 5),
                    Window = TimeSpan.FromSeconds(a.GetValue("WindowSeconds", 60)),
                    QueueLimit = 0
                }));

            // resposta 429 padronizada
            o.OnRejected = async (ctx, ct) =>
            {
                if (ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    ctx.HttpContext.Response.Headers.RetryAfter = ((int)retry.TotalSeconds).ToString();

                ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                await ctx.HttpContext.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = 429, Title = "Muitas requisições",
                    Detail = "Você excedeu o limite de requisições. Tente novamente em instantes.",
                    Instance = ctx.HttpContext.Request.Path
                }, ct);
            };
        });
        return s;
    }
}