using SobraBoa.Application.Dtos;
using SobraBoa.Application.Services;

namespace SobraBoa.API.Endpoints;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdmin(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin")
            .WithTags("Admin")
            .RequireAuthorization("Admin")
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        g.MapGet("/usuarios/pendentes", async (int? page, int? pageSize, AdminService svc, CancellationToken ct)
            => Results.Ok(await svc.PendentesAsync(page ?? 1, pageSize ?? 10, ct)))
            .WithSummary("Lista cadastros pendentes (paginado)").Produces<PagedResult<object>>(200);
        
        g.MapPatch("/usuarios/{id:int}/aprovacao",
            async (int id, AprovacaoRequest req, AdminService svc, CancellationToken ct) =>
            {
                await svc.AprovarAsync(id, req.Aprovado, ct);
                return Results.NoContent();
            }).Produces(204).ProducesProblem(404)
            .WithSummary("Aprova ou rejeita um cadastro");

        g.MapGet("/metricas", async (AdminService svc, CancellationToken ct) => Results.Ok(await svc.MetricasAsync(ct)))
            .Produces<MetricasResponse>(200)
            .WithSummary("Métricas de impacto: kg doados, doações por status e usuários aprovados");

        return app;
    }
}