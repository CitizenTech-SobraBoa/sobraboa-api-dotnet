using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

namespace SobraBoa.IntegrationTests;

public class RateLimitTests
{
    [Fact]
    public async Task Login_AcimaDoLimite_Retorna429ComRetryAfter()
    {
        await using var factory = new CustomWebApplicationFactory();
        var baixa = factory.WithWebHostBuilder(b => b.ConfigureAppConfiguration((_, c) =>
            c.AddInMemoryCollection(new Dictionary<string, string?> { ["RateLimiting:Auth:PermitLimit"] = "3" })));
        var client = baixa.CreateClient();

        HttpResponseMessage? ultima = null;
        for (var i = 0; i < 5; i++)
            ultima = await client.PostAsJsonAsync("/api/auth/login", new { email = "a@a.com", senha = "senha-errada-1" });

        Assert.Equal(HttpStatusCode.TooManyRequests, ultima!.StatusCode);
        Assert.True(ultima.Headers.Contains("Retry-After"));
    }
}