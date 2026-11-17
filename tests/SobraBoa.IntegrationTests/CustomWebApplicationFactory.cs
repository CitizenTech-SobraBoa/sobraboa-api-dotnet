using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = "sobraboa-" + Guid.NewGuid();
    public const string Key = "TROQUE-ESTA-CHAVE-SUPER-SECRETA-COM-NO-MINIMO-32-CARACTERES"; // mesma do appsettings
    public Dictionary<string, string?> Extra { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, cfg) => cfg.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:OracleConnection"] = "Data Source=fake",
            ["RateLimiting:Global:PermitLimit"] = "100000",     // não atrapalha os outros testes
            ["RateLimiting:Auth:PermitLimit"] = "100000",
            ["HealthChecks:ExternalService:Url"] = "http://localhost:1/inexistente",
            ["HealthChecks:ExternalService:TimeoutSeconds"] = "1",
            ["Jobs:Expiracao:Habilitado"] = "false",
            ["Admin:Email"] = "admin@sobraboa.com", ["Admin:Senha"] = "Admin@12345"
        }.Concat(Extra).ToDictionary(k => k.Key, k => k.Value)));

        builder.ConfigureServices(s =>
        {
            // remove TODAS as configurações do DbContext (Oracle) e coloca InMemory
            var remover = s.Where(d => d.ServiceType == typeof(DbContextOptions<SobraBoaContext>)
                                    || d.ServiceType == typeof(DbContextOptions)
                                    || d.ServiceType.FullName!.Contains("IDbContextOptionsConfiguration")).ToList();
            foreach (var d in remover) s.Remove(d);
            s.AddDbContext<SobraBoaContext>(o => o.UseInMemoryDatabase(_dbName));
        });
    }

    public string GerarToken(int id, string role)
    {
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken("SobraBoa.API", "SobraBoa.Clients",
            [new Claim("sub", id.ToString()), new Claim("name", "teste"), new Claim("role", role)],
            expires: DateTime.UtcNow.AddMinutes(30), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    public HttpClient ClienteAutenticado(int id, string role)
    {
        var c = CreateClient();
        c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GerarToken(id, role));
        return c;
    }
}

[CollectionDefinition("IntegrationTests")]
public class IntegrationTestsCollection : ICollectionFixture<CustomWebApplicationFactory>;