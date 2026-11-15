using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.API.Infrastructure;

public static class HealthChecksSetup
{
    public static IServiceCollection AddHealthChecksSetup(this IServiceCollection s, IConfiguration cfg)
    {
        s.AddHttpClient("health-externo",
            c => c.Timeout = TimeSpan.FromSeconds(cfg.GetValue("HealthChecks:ExternalService:TimeoutSeconds", 3)));

        s.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy("API no ar"), tags: ["live"])
            .AddDbContextCheck<SobraBoaContext>("oracle_database", failureStatus: HealthStatus.Unhealthy,
                tags: ["ready", "db"])
            .AddCheck<ExternalServiceHealthCheck>("external_service", failureStatus: HealthStatus.Degraded,
                tags: ["ready", "external"]);
        return s;
    }

    public static IEndpointRouteBuilder MapHealthChecksEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapHealthChecks("/health", Opcoes(_ => true));
        app.MapHealthChecks("/health/live", Opcoes(r => r.Tags.Contains("live")));
        app.MapHealthChecks("/health/ready", Opcoes(r => r.Tags.Contains("ready")));
        return app;
    }

    private static HealthCheckOptions Opcoes(Func<HealthCheckRegistration, bool> filtro) => new()
    {
        Predicate = filtro,
        // Healthy/Degraded -> 200 ; Unhealthy -> 503 (padrão do ASP.NET)
        ResponseWriter = async (ctx, report) =>
        {
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                status = report.Status.ToString(),
                duracaoMs = report.TotalDuration.TotalMilliseconds,
                checks = report.Entries.Select(e => new
                {
                    nome = e.Key, status = e.Value.Status.ToString(), descricao = e.Value.Description,
                    duracaoMs = e.Value.Duration.TotalMilliseconds
                })
            }));
        }
    };
}

public class ExternalServiceHealthCheck(IHttpClientFactory factory, IConfiguration cfg) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var url = cfg["HealthChecks:ExternalService:Url"]!;
            var resp = await factory.CreateClient("health-externo").GetAsync(url, ct);
            return resp.IsSuccessStatusCode
                ? HealthCheckResult.Healthy($"ViaCEP respondeu {(int)resp.StatusCode}")
                : HealthCheckResult.Degraded($"ViaCEP respondeu {(int)resp.StatusCode}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Degraded("ViaCEP indisponível", ex);
        }
    }
}