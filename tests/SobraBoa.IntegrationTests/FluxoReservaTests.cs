using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace SobraBoa.IntegrationTests;

[Collection("IntegrationTests")]
public class FluxoReservaTests(CustomWebApplicationFactory f)
{
    // ---------------- testes ----------------

    [Fact]
    public async Task Reservar_MesmaIdempotencyKey_RetornaMesmaReservaSemDuplicar()
    {
        // Arrange
        var (_, doador) = await RegistrarEAprovarAsync("Doador");
        var (_, receptor) = await RegistrarEAprovarAsync("Receptor");
        var doacaoId = await CriarDoacaoPublicadaAsync(doador);
        var key = Guid.NewGuid().ToString();

        // Act
        var r1 = await Reservar(receptor, doacaoId, key);
        var r2 = await Reservar(receptor, doacaoId, key);

        // Assert
        Assert.Equal(HttpStatusCode.Created, r1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, r2.StatusCode); // repetição NÃO vira 409
        Assert.True(r2.Headers.Contains("Idempotent-Replayed"));
        Assert.Equal(await r1.Content.ReadAsStringAsync(),
            await r2.Content.ReadAsStringAsync()); // mesmo corpo e mesmo código de retirada
    }

    [Fact]
    public async Task Reservar_SemIdempotencyKey_Retorna400()
    {
        var (_, doador) = await RegistrarEAprovarAsync("Doador");
        var (_, receptor) = await RegistrarEAprovarAsync("Receptor");
        var doacaoId = await CriarDoacaoPublicadaAsync(doador);

        var resp = await receptor.PostAsync($"/api/doacoes/{doacaoId}/reservas", null); // sem o header

        Assert.Equal(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [Fact]
    public async Task Reservar_OutroReceptorNaMesmaDoacao_Retorna409()
    {
        var (_, doador) = await RegistrarEAprovarAsync("Doador");
        var (_, receptorA) = await RegistrarEAprovarAsync("Receptor");
        var (_, receptorB) = await RegistrarEAprovarAsync("Receptor");
        var doacaoId = await CriarDoacaoPublicadaAsync(doador);

        var a = await Reservar(receptorA, doacaoId, Guid.NewGuid().ToString());
        var b = await Reservar(receptorB, doacaoId, Guid.NewGuid().ToString()); // chave diferente

        Assert.Equal(HttpStatusCode.Created, a.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, b.StatusCode);
    }

    [Fact]
    public async Task FluxoCompleto_ReservarRetirarReceberEmitirComprovante()
    {
        var (_, doador) = await RegistrarEAprovarAsync("Doador");
        var (_, receptor) = await RegistrarEAprovarAsync("Receptor");
        var doacaoId = await CriarDoacaoPublicadaAsync(doador);

        // reserva
        var reserva = await Reservar(receptor, doacaoId, Guid.NewGuid().ToString());
        Assert.Equal(HttpStatusCode.Created, reserva.StatusCode);
        var corpo = await reserva.Content.ReadFromJsonAsync<JsonElement>();
        var reservaId = corpo.GetProperty("id").GetInt32();
        var codigo = corpo.GetProperty("codigoRetirada").GetString();

        // doador confirma a retirada com o código
        var retirada = await doador.PostAsJsonAsync($"/api/reservas/{reservaId}/retirada", new { codigo });
        Assert.Equal(HttpStatusCode.OK, retirada.StatusCode);

        // receptor confirma o recebimento (idempotente)
        var receber = new HttpRequestMessage(HttpMethod.Post, $"/api/reservas/{reservaId}/receber");
        receber.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        var recebido = await receptor.SendAsync(receber);
        Assert.Equal(HttpStatusCode.OK, recebido.StatusCode);

        // comprovante disponível
        var comp = await receptor.GetAsync($"/api/reservas/{reservaId}/comprovante");
        Assert.Equal(HttpStatusCode.OK, comp.StatusCode);
    }

    // ---------------- helpers ----------------

    private static Task<HttpResponseMessage> Reservar(HttpClient c, int doacaoId, string key)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"/api/doacoes/{doacaoId}/reservas");
        req.Headers.Add("Idempotency-Key", key);
        return c.SendAsync(req);
    }

    /// <summary>
    /// Registra um usuário pela API (nasce PENDENTE), aprova como Admin e devolve (id, cliente autenticado).
    /// </summary>
    private async Task<(int Id, HttpClient Client)> RegistrarEAprovarAsync(string papel)
    {
        var anonimo = f.CreateClient();

        var registro = await anonimo.PostAsJsonAsync("/api/auth/registro", new
        {
            nome = $"Teste {papel}",
            email = $"{Guid.NewGuid():N}@teste.com", // único: o banco InMemory é compartilhado
            senha = "Senha@123",
            papel, // "Doador" | "Receptor" (exige JsonStringEnumConverter)
            nomeOrganizacao = $"Org {papel}",
            latitude = -23.55,
            longitude = -46.63,
            possuiRefrigeracao = true,
            aceitouTermos = true
        });
        Assert.Equal(HttpStatusCode.Created, registro.StatusCode);
        var id = (await registro.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("usuarioId").GetInt32();

        // Admin aprova (o token de teste basta: os endpoints de admin só checam o papel)
        var admin = f.ClienteAutenticado(0, "Admin");
        var aprovacao = await admin.PatchAsJsonAsync($"/api/admin/usuarios/{id}/aprovacao", new { aprovado = true });
        Assert.Equal(HttpStatusCode.NoContent, aprovacao.StatusCode);

        // token com o id real: os services consultam o usuário no banco
        return (id, f.ClienteAutenticado(id, papel));
    }

    /// <summary>
    /// Doador cria uma doação (janela aberta agora) e publica. Devolve o id.
    /// </summary>
    private static async Task<int> CriarDoacaoPublicadaAsync(HttpClient doador)
    {
        var agora = DateTime.UtcNow;
        var criar = await doador.PostAsJsonAsync("/api/doacoes", new
        {
            titulo = "Pães do fim de tarde",
            descricao = "Sobra do expediente",
            janelaInicio = agora.AddHours(-1),
            janelaFim = agora.AddHours(4),
            requerRefrigeracao = false,
            instrucoesRetirada = "Retirar no balcão",
            itens = new[]
            {
                new
                {
                    descricao = "Pão francês", quantidade = 30m, unidade = "UN", pesoEstimadoKg = 1.5m,
                    dataValidade = (string?)null
                }
            }
        });
        Assert.Equal(HttpStatusCode.Created, criar.StatusCode);
        var id = (await criar.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var publicar = await doador.PatchAsync($"/api/doacoes/{id}/publicar", null);
        Assert.Equal(HttpStatusCode.OK, publicar.StatusCode);
        return id;
    }
}

// HttpClient não tem PatchAsJsonAsync nativo: extensão mínima
internal static class HttpClientPatchExtensions
{
    public static Task<HttpResponseMessage> PatchAsJsonAsync<T>(this HttpClient client, string url, T value)
        => client.PatchAsync(url, JsonContent.Create(value));
}