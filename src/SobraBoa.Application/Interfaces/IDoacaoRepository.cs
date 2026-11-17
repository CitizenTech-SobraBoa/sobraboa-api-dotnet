using SobraBoa.Domain.Entities;

namespace SobraBoa.Application.Interfaces;

public interface IDoacaoRepository
{
    Task<Doacao?> ObterPorIdAsync(int id, CancellationToken ct = default);   // com Itens
    Task<(IReadOnlyList<Doacao> Itens, int Total)> ListarDoDoadorAsync(int doadorId, int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<Doacao>> ListarDisponiveisAsync(DateTime agoraUtc, double minLat, double maxLat, double minLng, double maxLng, CancellationToken ct = default);
    Task AdicionarAsync(Doacao d, CancellationToken ct = default);
    Task AdicionarHistoricoAsync(HistoricoStatus h, CancellationToken ct = default);
    Task<IReadOnlyDictionary<string, int>> ContarPorStatusAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Doacao>> ListarDisponiveisVencidasAsync(DateTime agoraUtc, int limite, CancellationToken ct = default);
}