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

        // 2) já existe registro? (respondido ou em processamento)
        var existente = await store.ObterAsync(chave, escopo, ct);
        if (existente is not null) return Responder(http, existente);

        // 3) reserva a chave ANTES de executar: se perder a corrida, devolve o que o vencedor gravou
        var agora = clock.GetUtcNow().UtcDateTime;
        if (!await store.TentarReservarAsync(chave, escopo, agora, ct))
        {
            existente = await store.ObterAsync(chave, escopo, ct);
            return existente is null ? EmProcessamento(http) : Responder(http, existente);
        }

        // 4) executa de verdade; se falhar, libera a chave para o cliente tentar de novo
        object? resultado;
        try
        {
            resultado = await next(ctx);
        }
        catch
        {
            await store.LiberarAsync(chave, escopo, CancellationToken.None);
            throw;
        }

        // 5) grava a resposta definitiva (< 500); erro de servidor libera a chave
        if (resultado is IStatusCodeHttpResult sc && (sc.StatusCode ?? 200) < 500)
        {
            var corpo = resultado is IValueHttpResult v && v.Value is not null
                ? JsonSerializer.Serialize(v.Value, Json)
                : null;
            await store.CompletarAsync(chave, escopo, sc.StatusCode ?? 200, corpo, ct);
        }
        else
        {
            await store.LiberarAsync(chave, escopo, CancellationToken.None);
        }

        return resultado;
    }

    private const int EmProcessamentoStatus = 0;

    private static IResult Responder(HttpContext http, IdempotencyRecord r)
    {
        if (r.StatusCode == EmProcessamentoStatus) return EmProcessamento(http);

        http.Response.Headers["Idempotent-Replayed"] = "true";
        return r.CorpoResposta is null
            ? Results.StatusCode(r.StatusCode)
            : Results.Content(r.CorpoResposta, "application/json", statusCode: r.StatusCode);
    }

    private static IResult EmProcessamento(HttpContext http)
    {
        http.Response.Headers.RetryAfter = "1";
        return Results.Problem(title: "Requisição em processamento",
            detail: "Outra requisição com a mesma chave está em andamento. Tente novamente em instantes.",
            statusCode: 409);
    }
}

public static class IdempotencyExtensions
{
    public static RouteHandlerBuilder RequireIdempotencyKey(this RouteHandlerBuilder builder)
        => builder.AddEndpointFilter<IdempotencyFilter>()
            .WithMetadata(new IdempotentMarker())
            .ProducesProblem(StatusCodes.Status400BadRequest);
}