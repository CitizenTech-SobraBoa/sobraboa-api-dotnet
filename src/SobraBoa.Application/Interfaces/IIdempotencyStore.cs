using SobraBoa.Domain.Entities;

namespace SobraBoa.Application.Interfaces;

public interface IIdempotencyStore
{
    Task<IdempotencyRecord?> ObterAsync(string chave, string escopo, CancellationToken ct = default);

    /// <returns>false se outra requisição com a mesma chave já gravou (corrida)</returns>
    Task<bool> SalvarAsync(IdempotencyRecord r, CancellationToken ct = default);
    
    /// <returns>true se esta requisição reservou a chave; false se outra chegou antes</returns>
    Task<bool> TentarReservarAsync(string chave, string escopo, DateTime agoraUtc, CancellationToken ct = default);

    /// <summary>Grava a resposta definitiva de uma chave já reservada.</summary>
    Task CompletarAsync(string chave, string escopo, int statusCode, string? corpo, CancellationToken ct = default);

    /// <summary>Remove a reserva (a operação falhou e o cliente pode tentar de novo).</summary>
    Task LiberarAsync(string chave, string escopo, CancellationToken ct = default);
    
    Task<int> RemoverAnterioresAsync(DateTime limiteUtc, CancellationToken ct = default);
    
}