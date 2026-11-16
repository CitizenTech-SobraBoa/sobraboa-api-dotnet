using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.API.Infrastructure;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> log, IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext ctx, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            BadHttpRequestException b => (b.StatusCode, "Requisição inválida"),
            AuthenticationFailedException => (401, "Não autenticado"),
            ForbiddenException => (403, "Acesso negado"),
            NotFoundException => (404, "Não encontrado"),
            ConflictException => (409, "Conflito"),
            DomainException => (422, "Regra de negócio violada"),
            ArgumentException => (400, "Requisição inválida"),
            _ => (500, "Erro interno")
        };

        if (status >= 500) log.LogError(ex, "Erro não tratado");
        else log.LogWarning("{Tipo}: {Msg}", ex.GetType().Name, ex.Message);

        var problem = new ProblemDetails
        {
            Status = status, Title = title,
            Detail = status == 500 && !env.IsDevelopment() ? "Ocorreu um erro inesperado." : ex.Message,
            Instance = ctx.Request.Path
        };
        problem.Extensions["traceId"] = ctx.TraceIdentifier;

        ctx.Response.StatusCode = status;
        await ctx.Response.WriteAsJsonAsync(problem, ct);
        return true;
    }
}