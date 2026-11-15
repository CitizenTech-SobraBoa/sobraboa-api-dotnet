using SobraBoa.Application.Dtos;
using SobraBoa.Application.Services;

namespace SobraBoa.API.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdmin(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization("Admin");

        g.MapGet("/usuarios/pendentes", async (int? page, int? pageSize, AdminService svc, CancellationToken ct)
            => Results.Ok(await svc.PendentesAsync(page ?? 1, pageSize ?? 10, ct)));

        g.MapPatch("/usuarios/{id:int}/aprovacao",
            async (int id, AprovacaoRequest req, AdminService svc, CancellationToken ct) =>
            {
                await svc.AprovarAsync(id, req.Aprovado, ct);
                return Results.NoContent();
            }).Produces(204).ProducesProblem(404);

        g.MapGet("/metricas", async (AdminService svc, CancellationToken ct) => Results.Ok(await svc.MetricasAsync(ct)))
            .Produces<MetricasResponse>(200);

        return app;
    }
}