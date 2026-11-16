using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using SobraBoa.API.Endpoints;
using SobraBoa.API.Infrastructure;
using SobraBoa.Application;
using SobraBoa.Infrastructure;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ---- Logging estruturado ----
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext().Enrich.WithCorrelationId()
    .WriteTo.Console()
    .WriteTo.File("logs/sobraboa-api-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 14));

// ---- Camadas ----
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ---- Validação automática dos DTOs (DataAnnotations) -> 400 ProblemDetails ----
builder.Services.AddValidation();

// ---- Erros globais ----
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

// ---- JWT + autorização por papel ----
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // mantém "sub" e "role" exatamente como estão no token

        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidateAudience = true, ValidAudience = builder.Configuration["Jwt:Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)),
            ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name", RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Doador", p => p.RequireRole("Doador"))
    .AddPolicy("Receptor", p => p.RequireRole("Receptor"))
    .AddPolicy("Admin", p => p.RequireRole("Admin"));

// ---- Rate limiting, Health checks, Swagger ----
builder.Services.AddRateLimitingSetup(builder.Configuration);
builder.Services.AddHealthChecksSetup(builder.Configuration);
builder.Services.AddSwaggerSetup();

builder.Services.ConfigureHttpJsonOptions(o =>
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();


app.UseSerilogRequestLogging();
app.UseExceptionHandler();
app.UseStatusCodePages();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseRateLimiter(); // antes da autenticação: barra abuso cedo
app.UseAuthentication(); // 1º quem é você
app.UseAuthorization(); // 2º o que pode fazer

app.MapAuth();
app.MapDoacoes();
app.MapReservas();
app.MapAdmin();
app.MapHealthChecksEndpoints();

await app.SeedAdminAsync(); 
await app.SeedDadosDemoAsync();
app.Run();

public partial class Program; // necessário para WebApplicationFactory<Program> nos testes