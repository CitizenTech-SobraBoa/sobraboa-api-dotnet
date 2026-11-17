using SobraBoa.API.Infrastructure;
using SobraBoa.Application.Dtos;
using SobraBoa.Application.Services;

namespace SobraBoa.API.Endpoints;

public static class ReservaEndpoints
{
    public static IEndpointRouteBuilder MapReservas(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/reservas")
            .WithTags("Reservas")
            .RequireAuthorization()
            .ProducesProblem(StatusCodes.Status429TooManyRequests);

        g.MapPost("/{id:int}/retirada",
                async (int id, CodigoRequest req, ReservaService svc, HttpContext http, CancellationToken ct)
                    => Results.Ok(await svc.ConfirmarRetiradaAsync(http.User.GetUserId(), id, req.Codigo, ct)))
            .RequireAuthorization("Doador").Produces<ReservaResponse>(200).ProducesProblem(422)
            .WithSummary("Doador confirma a retirada informando o código")
            .ProducesProblem(403).ProducesProblem(404);

        g.MapPost("/{id:int}/receber", async (int id, ReservaService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.ReceberAsync(http.User.GetUserId(), id, ct)))
            .RequireAuthorization("Receptor").RequireIdempotencyKey()
            .WithSummary("Confirma o recebimento e emite o comprovante (idempotente)")
            .Produces<ComprovanteResponse>(200);

        g.MapPost("/{id:int}/cancelar",
                async (int id, CancelarRequest req, ReservaService svc, HttpContext http, CancellationToken ct) =>
                {
                    await svc.CancelarAsync(http.User.GetUserId(), id, req.Motivo, ct);
                    return Results.NoContent();
                })
            .RequireAuthorization("Receptor").Produces(204)
            .WithSummary("Receptor cancela a própria reserva")
            .ProducesProblem(403).ProducesProblem(404).ProducesProblem(422);

        g.MapGet("/{id:int}/comprovante", async (int id, ReservaService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.ObterComprovanteAsync(http.User.GetUserId(), http.User.IsAdmin(), id, ct)))
            .Produces<ComprovanteResponse>(200).ProducesProblem(404)
            .WithSummary("Consulta o comprovante (dono da reserva, dono da doação ou admin)")
            .ProducesProblem(403);
        
        g.MapGet("/minhas", async (ReservaService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.MinhasAsync(http.User.GetUserId(), ct)))
            .RequireAuthorization("Receptor")
            .WithSummary("Lista as reservas do receptor autenticado")
            .Produces<IReadOnlyList<ReservaResponse>>(200).ProducesProblem(403);

        return app;
    }
}