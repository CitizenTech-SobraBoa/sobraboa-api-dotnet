using Microsoft.EntityFrameworkCore;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.Infrastructure.Repositories;

public class DoacaoRepository(SobraBoaContext db) : IDoacaoRepository
{
    public Task<Doacao?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => db.Doacoes.Include(d => d.Itens).FirstOrDefaultAsync(d => d.Id == id, ct);

    public async Task<(IReadOnlyList<Doacao>, int)> ListarDoDoadorAsync(int doadorId, int page, int pageSize,
        CancellationToken ct = default)
    {
        var q = db.Doacoes.AsNoTracking().Include(d => d.Itens).Where(d => d.DoadorId == doadorId);
        var total = await q.CountAsync(ct);
        var itens = await q.OrderByDescending(d => d.CriadoEm).Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync(ct);
        return (itens, total);
    }

    public async Task<IReadOnlyList<Doacao>> ListarDisponiveisAsync(DateTime agora, double minLat, double maxLat,
        double minLng, double maxLng, CancellationToken ct = default)
        => await db.Doacoes.AsNoTracking().Include(d => d.Itens)
            .Where(d => d.Status == StatusDoacao.Disponivel && d.JanelaInicio <= agora && d.JanelaFim >= agora
                        && d.Latitude >= minLat && d.Latitude <= maxLat && d.Longitude >= minLng &&
                        d.Longitude <= maxLng)
            .ToListAsync(ct);

    public async Task AdicionarAsync(Doacao d, CancellationToken ct = default) => await db.Doacoes.AddAsync(d, ct);

    public async Task AdicionarHistoricoAsync(HistoricoStatus h, CancellationToken ct = default) =>
        await db.Historico.AddAsync(h, ct);

    public async Task<IReadOnlyDictionary<string, int>> ContarPorStatusAsync(CancellationToken ct = default)
        => (await db.Doacoes.GroupBy(d => d.Status).Select(g => new { g.Key, N = g.Count() }).ToListAsync(ct))
            .ToDictionary(x => x.Key.ToString(), x => x.N);
    
    public async Task<IReadOnlyList<Doacao>> ListarDisponiveisVencidasAsync(DateTime agoraUtc, int limite,
        CancellationToken ct = default)
        => await db.Doacoes
            .Where(d => d.Status == StatusDoacao.Disponivel && d.JanelaFim <= agoraUtc)
            .OrderBy(d => d.JanelaFim).Take(limite)
            .ToListAsync(ct);
}