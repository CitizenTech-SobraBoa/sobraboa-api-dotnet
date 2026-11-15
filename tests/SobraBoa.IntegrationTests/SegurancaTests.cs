using System.Net;
using System.Net.Http.Json;

namespace SobraBoa.IntegrationTests;

[Collection("IntegrationTests")]
public class SegurancaTests(CustomWebApplicationFactory f)
{
    [Fact]
    public async Task Doacoes_SemToken_Retorna401()
    {
        var r = await f.CreateClient().GetAsync("/api/doacoes/minhas");
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Doacoes_ComPapelErrado_Retorna403()
    {
        var receptor = f.ClienteAutenticado(1, "Receptor");
        var r = await receptor.GetAsync("/api/doacoes/minhas"); // rota só de Doador
        Assert.Equal(HttpStatusCode.Forbidden, r.StatusCode);
    }

    [Fact]
    public async Task Login_CredenciaisInvalidas_Retorna401()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/login", new { email = "nao@existe.com", senha = "qualquer123" });
        Assert.Equal(HttpStatusCode.Unauthorized, r.StatusCode);
    }

    [Fact]
    public async Task Registro_SenhaCurta_Retorna400()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/registro", new
            { nome = "X", email = $"{Guid.NewGuid()}@x.com", senha = "123", papel = "Doador", nomeOrganizacao = "Y", latitude = 0, longitude = 0, aceitouTermos = true });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}

[Collection("IntegrationTests")]
public class HealthTests(CustomWebApplicationFactory f)
{
    [Fact]
    public async Task Live_Retorna200()
        => Assert.Equal(HttpStatusCode.OK, (await f.CreateClient().GetAsync("/health/live")).StatusCode);

    [Fact]
    public async Task Health_ServicoExternoFora_ContinuaRetornando200ComDegraded()
    {
        var r = await f.CreateClient().GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, r.StatusCode);                    // Degraded != Unhealthy
        Assert.Contains("external_service", await r.Content.ReadAsStringAsync());
    }
}