using System.Text.Json;
using SobraBoa.Application.Interfaces;

namespace SobraBoa.API.Infrastructure;

public class ViaCepService(HttpClient http, ILogger<ViaCepService> log) : ICepService
{
    public async Task<bool?> CepExisteAsync(string cep, CancellationToken ct = default)
    {
        var digitos = new string(cep.Where(char.IsDigit).ToArray());
        if (digitos.Length != 8) return false;

        try
        {
            using var resp = await http.GetAsync($"ws/{digitos}/json/", ct);
            if (!resp.IsSuccessStatusCode) return null;

            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
            return !doc.RootElement.TryGetProperty("erro", out _); // CEP inexistente => { "erro": true }
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            log.LogWarning(ex, "ViaCEP indisponível; cadastro segue sem validar o CEP");
            return null;
        }
    }
}