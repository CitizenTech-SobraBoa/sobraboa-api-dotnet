using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace SobraBoa.API.Infrastructure;

public static class SwaggerSetup
{
    public static IServiceCollection AddSwaggerSetup(this IServiceCollection s)
    {
        s.AddEndpointsApiExplorer();
        s.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "SobraBoa API", Version = "v1",
                Description =
                    "CitizenTech · FIAP Global Solution 2026/2 — conecta comércios com sobra de alimentos a organizações que atendem pessoas em insegurança alimentar (ODS 2 e 12)."
            });

            // Esquema Http/bearer: o botão Authorize aceita só o token (sem digitar "Bearer ")
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT",
                Description = "Cole apenas o token retornado em /api/auth/login"
            });
            o.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
                { [new OpenApiSecuritySchemeReference("Bearer", doc)] = new List<string>() });

            o.OperationFilter<IdempotencyHeaderFilter>();
        });
        return s;
    }
}

// Mostra o header Idempotency-Key nos endpoints que usam IdempotencyFilter
public class IdempotencyHeaderFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var path = context.ApiDescription.RelativePath ?? "";
        var metodo = context.ApiDescription.HttpMethod;

        // rotas que exigem o header: POST /api/doacoes/{id}/reservas e POST /api/reservas/{id}/receber
        var usa = metodo == "POST" &&
                  ((path.StartsWith("api/doacoes") && path.EndsWith("/reservas")) || path.EndsWith("/receber"));
        if (!usa) return;

        operation.Parameters ??= [];
        operation.Parameters.Add(new OpenApiParameter
        {
            Name = "Idempotency-Key",
            In = ParameterLocation.Header,
            Required = true,
            Description = "Valor único gerado pelo cliente. Repetir a mesma chave devolve a resposta original sem executar de novo.",
            Schema = new OpenApiSchema { Type = JsonSchemaType.String }
        });
    }
}