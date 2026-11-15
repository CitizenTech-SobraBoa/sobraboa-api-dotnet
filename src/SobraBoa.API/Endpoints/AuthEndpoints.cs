using SobraBoa.Application.Dtos;
using SobraBoa.Application.Services;

namespace SobraBoa.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuth(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Autenticação").RequireRateLimiting("auth");

        g.MapPost("/registro", async (RegistroRequest req, AuthService svc, CancellationToken ct) =>
            {
                var id = await svc.RegistrarAsync(req, ct);
                return Results.Created($"/api/usuarios/{id}",
                    new { usuarioId = id, mensagem = "Cadastro enviado. Aguarde a aprovação do administrador." });
            })
            .WithSummary("Cadastra doador ou receptor (nasce PENDENTE)")
            .Produces(201).ProducesValidationProblem().ProducesProblem(409);

        g.MapPost("/login", async (LoginRequest req, AuthService svc, CancellationToken ct)
                => Results.Ok(await svc.LoginAsync(req, ct)))
            .WithSummary("Autentica e devolve o JWT")
            .Produces<LoginResponse>(200).ProducesProblem(401).ProducesProblem(429);

        return app;
    }
}