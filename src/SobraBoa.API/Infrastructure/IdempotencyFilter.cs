using System.Text.Json;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;

namespace SobraBoa.API.Infrastructure;

public sealed class IdempotentMarker; // usado só pelo Swagger

public class IdempotencyFilter(IIdempotencyStore store, TimeProvider clock) : IEndpointFilter
{
    private const string Header = "Idempotency-Key";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var http = ctx.HttpContext;
        var ct = http.RequestAborted;

        // 1) header obrigatório
        if (!http.Request.Headers.TryGetValue(Header, out var valores) || string.IsNullOrWhiteSpace(valores))
            return Results.Problem(title: "Header ausente",
                detail: $"O header '{Header}' é obrigatório neste endpoint.", statusCode: 400);

        var chave = valores.ToString().Trim();
        if (chave.Length > 100)
            return Results.Problem(title: "Header inválido", detail: $"'{Header}' deve ter no máximo 100 caracteres.",
                statusCode: 400);

        var escopo = $"{http.User.GetUserId()}:{http.Request.Method}:{http.Request.Path}";

        // 2) já processada? devolve a resposta original
        var existente = await store.ObterAsync(chave, escopo, ct);
        if (existente is not null)
        {
            http.Response.Headers["Idempotent-Replayed"] = "true";
            return existente.CorpoResposta is null
                ? Results.StatusCode(existente.StatusCode)
                : Results.Content(existente.CorpoResposta, "application/json", statusCode: existente.StatusCode);
        }

        // 3) executa de verdade
        var resultado = await next(ctx);

        // 4) grava a resposta (somente respostas "definitivas": < 500)
        if (resultado is IStatusCodeHttpResult sc && (sc.StatusCode ?? 200) < 500)
        {
            var corpo = resultado is IValueHttpResult v && v.Value is not null
                ? JsonSerializer.Serialize(v.Value, Json)
                : null;
            var gravou = await store.SalvarAsync(new IdempotencyRecord
            {
                Chave = chave, Escopo = escopo, StatusCode = sc.StatusCode ?? 200,
                CorpoResposta = corpo, CriadoEm = clock.GetUtcNow().UtcDateTime
            }, ct);

            // 5) corrida: duas requisições com a mesma chave ao mesmo tempo
            if (!gravou)
                return Results.Problem(title: "Requisição em processamento",
                    detail: "Outra requisição com a mesma chave está em andamento.", statusCode: 409);
        }

        return resultado;
    }
}

public static class IdempotencyExtensions
{
    public static RouteHandlerBuilder RequireIdempotencyKey(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<IdempotencyFilter>()
            .WithMetadata(new IdempotentMarker())
            .ProducesProblem(StatusCodes.Status400BadRequest);
}