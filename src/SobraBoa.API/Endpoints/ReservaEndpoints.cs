using SobraBoa.API.Infrastructure;
using SobraBoa.Application.Dtos;
using SobraBoa.Application.Services;

namespace SobraBoa.API.Endpoints;

public static class ReservaEndpoints
{
    public static IEndpointRouteBuilder MapReservas(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reservas").WithTags("Reservas").RequireAuthorization();

        g.MapPost("/{id:int}/retirada",
                async (int id, CodigoRequest req, ReservaService svc, HttpContext http, CancellationToken ct)
                    => Results.Ok(await svc.ConfirmarRetiradaAsync(http.User.GetUserId(), id, req.Codigo, ct)))
            .RequireAuthorization("Doador").Produces<ReservaResponse>(200).ProducesProblem(422);

        g.MapPost("/{id:int}/receber", async (int id, ReservaService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.ReceberAsync(http.User.GetUserId(), id, ct)))
            .RequireAuthorization("Receptor").AddEndpointFilter<IdempotencyFilter>()
            .WithSummary("Confirma o recebimento e emite o comprovante (idempotente)")
            .Produces<ComprovanteResponse>(200);

        g.MapPost("/{id:int}/cancelar",
                async (int id, CancelarRequest req, ReservaService svc, HttpContext http, CancellationToken ct) =>
                {
                    await svc.CancelarAsync(http.User.GetUserId(), id, req.Motivo, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization("Receptor").Produces(204);

        g.MapGet("/{id:int}/comprovante", async (int id, ReservaService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.ObterComprovanteAsync(http.User.GetUserId(), http.User.IsAdmin(), id, ct)))
            .Produces<ComprovanteResponse>(200).ProducesProblem(404);

        return app;
    }
}