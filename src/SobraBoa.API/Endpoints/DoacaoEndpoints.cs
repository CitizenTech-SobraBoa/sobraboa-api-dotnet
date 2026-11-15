using SobraBoa.API.Infrastructure;
using SobraBoa.Application.Dtos;
using SobraBoa.Application.Services;

namespace SobraBoa.API.Endpoints;

public static class DoacaoEndpoints
{
    public static IEndpointRouteBuilder MapDoacoes(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/doacoes").WithTags("Doações").RequireAuthorization();

        g.MapPost("/", async (DoacaoRequest req, DoacaoService svc, HttpContext http, CancellationToken ct) =>
            {
                var r = await svc.CriarAsync(http.User.GetUserId(), req, ct);
                return Results.Created($"/api/doacoes/{r.Id}", r);
            })
            .RequireAuthorization("Doador")
            .WithSummary("Cria uma doação em RASCUNHO")
            .Produces<DoacaoResponse>(201).ProducesValidationProblem().ProducesProblem(403).ProducesProblem(422);

        g.MapGet("/minhas", async (int? page, int? pageSize, DoacaoService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.MinhasAsync(http.User.GetUserId(), page ?? 1, pageSize ?? 10, ct)))
            .RequireAuthorization("Doador").Produces<PagedResult<DoacaoResponse>>(200);

        g.MapGet("/proximas", async (double lat, double lng, double? raioKm, int? page, int? pageSize,
                    DoacaoService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.ProximasAsync(http.User.GetUserId(), lat, lng, raioKm ?? 10, page ?? 1,
                    pageSize ?? 10, ct)))
            .RequireAuthorization("Receptor")
            .WithSummary("Doações disponíveis num raio (km) ao redor de lat/lng")
            .Produces<PagedResult<DoacaoResponse>>(200).ProducesProblem(400);

        g.MapGet("/{id:int}", async (int id, DoacaoService svc, CancellationToken ct)
                => Results.Ok(await svc.ObterAsync(id, ct)))
            .Produces<DoacaoResponse>(200).ProducesProblem(404);

        g.MapPut("/{id:int}",
                async (int id, DoacaoRequest req, DoacaoService svc, HttpContext http, CancellationToken ct)
                    => Results.Ok(await svc.AtualizarAsync(http.User.GetUserId(), id, req, ct)))
            .RequireAuthorization("Doador").Produces<DoacaoResponse>(200);

        g.MapPatch("/{id:int}/publicar", async (int id, DoacaoService svc, HttpContext http, CancellationToken ct)
                => Results.Ok(await svc.PublicarAsync(http.User.GetUserId(), id, ct)))
            .RequireAuthorization("Doador").Produces<DoacaoResponse>(200).ProducesProblem(422);

        g.MapDelete("/{id:int}", async (int id, DoacaoService svc, HttpContext http, CancellationToken ct) =>
            {
                await svc.CancelarAsync(http.User.GetUserId(), id, ct);
                return Results.NoContent();
            })
            .RequireAuthorization("Doador").Produces(204);

        // Reservar é IDEMPOTENTE: exige Idempotency-Key (etapa 10)
        g.MapPost("/{id:int}/reservas", async (int id, ReservaService svc, HttpContext http, CancellationToken ct) =>
            {
                var r = await svc.ReservarAsync(http.User.GetUserId(), id, ct);
                return Results.Created($"/api/reservas/{r.Id}", r);
            })
            .RequireAuthorization("Receptor")
            .AddEndpointFilter<IdempotencyFilter>()
            .WithSummary("Reserva a doação (idempotente via header Idempotency-Key)")
            .Produces<ReservaResponse>(201).ProducesProblem(409).ProducesProblem(422);

        return app;
    }
}