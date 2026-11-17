using System.Net;
using System.Net.Http.Json;

namespace SobraBoa.IntegrationTests;

[Collection("IntegrationTests")]
public class ValidacaoTests(CustomWebApplicationFactory f)
{
    private static object Registro(string? cnpj = null, string? cep = null) => new
    {
        nome = "Teste", email = $"{Guid.NewGuid():N}@teste.com", senha = "Senha@123", papel = "Doador",
        nomeOrganizacao = "Org", cnpj, cep, latitude = -23.5, longitude = -46.6,
        possuiRefrigeracao = false, aceitouTermos = true
    };

    [Fact]
    public async Task Registro_CnpjInvalido_Retorna400()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/registro", Registro(cnpj: "11.111.111/0001-11"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Registro_CnpjValido_Retorna201()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/registro", Registro(cnpj: "11.222.333/0001-81"));
        Assert.Equal(HttpStatusCode.Created, r.StatusCode);
    }

    [Fact]
    public async Task Registro_CepComFormatoInvalido_Retorna400()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/registro", Registro(cep: "123"));
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Registro_AceitouTermosFalso_Retorna400()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/registro", new
        {
            nome = "T", email = $"{Guid.NewGuid():N}@t.com", senha = "Senha@123", papel = "Doador",
            nomeOrganizacao = "O", latitude = 0, longitude = 0, aceitouTermos = false
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Registro_PapelAdmin_NaoEPermitido()
    {
        var r = await f.CreateClient().PostAsJsonAsync("/api/auth/registro", new
        {
            nome = "T", email = $"{Guid.NewGuid():N}@t.com", senha = "Senha@123", papel = "Admin",
            nomeOrganizacao = "O", latitude = 0, longitude = 0, aceitouTermos = true
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r.StatusCode);
    }

    [Fact]
    public async Task Doacao_JanelaFimAntesDoInicio_Retorna400()
    {
        var doador = f.ClienteAutenticado(1, "Doador");
        var agora = DateTime.UtcNow;
        var r = await doador.PostAsJsonAsync("/api/doacoes", new
        {
            titulo = "x", janelaInicio = agora.AddHours(3), janelaFim = agora.AddHours(1),
            requerRefrigeracao = false,
            itens = new[] { new { descricao = "a", quantidade = 1m, unidade = "UN", pesoEstimadoKg = 1m } }
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Doacao_SemItens_Retorna400()
    {
        var doador = f.ClienteAutenticado(1, "Doador");
        var agora = DateTime.UtcNow;
        var r = await doador.PostAsJsonAsync("/api/doacoes", new
        {
            titulo = "x", janelaInicio = agora.AddHours(-1), janelaFim = agora.AddHours(2),
            requerRefrigeracao = false, itens = Array.Empty<object>()
        });
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }
}