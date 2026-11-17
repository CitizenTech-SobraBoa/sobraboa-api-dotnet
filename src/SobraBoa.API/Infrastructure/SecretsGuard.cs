namespace SobraBoa.API.Infrastructure;

/// <summary>Impede a API de subir com segredos de exemplo fora do ambiente de desenvolvimento.</summary>
public static class SecretsGuard
{
    private const string ChavePlaceholder = "TROQUE-ESTA-CHAVE";
    private const string SenhaPlaceholder = "TROCAR-VIA-SECRETS";

    public static void Validar(IConfiguration cfg, IHostEnvironment env)
    {
        var key = cfg["Jwt:Key"];

        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException(
                "Jwt:Key ausente ou com menos de 32 caracteres. Configure via user-secrets ou variável de ambiente.");

        if (!env.IsDevelopment() && key.StartsWith(ChavePlaceholder, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Jwt:Key ainda é o valor de exemplo do repositório. Troque antes de rodar fora de Development.");
    }

    public static bool EhSenhaPlaceholder(string? senha)
        => string.Equals(senha, SenhaPlaceholder, StringComparison.Ordinal);
}