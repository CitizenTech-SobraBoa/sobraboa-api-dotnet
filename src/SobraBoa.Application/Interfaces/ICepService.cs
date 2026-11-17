namespace SobraBoa.Application.Interfaces;

public interface ICepService
{
    /// <returns>true = existe; false = não existe; null = serviço indisponível (não bloqueia o cadastro)</returns>
    Task<bool?> CepExisteAsync(string cep, CancellationToken ct = default);
}