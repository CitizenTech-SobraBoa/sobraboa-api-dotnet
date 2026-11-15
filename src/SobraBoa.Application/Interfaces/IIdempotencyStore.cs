using SobraBoa.Domain.Entities;

namespace SobraBoa.Application.Interfaces;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> ObterAsync(string chave, string escopo, CancellationToken ct = default);

    /// <returns>false se outra requisição com a mesma chave já gravou (corrida)</returns>
    Task<bool> SalvarAsync(IdempotencyRecord r, CancellationToken ct = default);
}