using Microsoft.EntityFrameworkCore;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.Infrastructure.Repositories;

public class ReservaRepository(SobraBoaContext db) : IReservaRepository
{
    // Com tracking: ReservaService chama ConfirmarRetirada()/Concluir()/Cancelar() e salva.
    public Task<Reserva?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => db.Reservas.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<Reserva>> ListarDoReceptorAsync(int receptorId, CancellationToken ct = default)
        => await db.Reservas.AsNoTracking()
            .Where(r => r.ReceptorId == receptorId)
            .OrderByDescending(r => r.CriadaEm)
            .ToListAsync(ct);

    public async Task AdicionarAsync(Reserva r, CancellationToken ct = default)
        => await db.Reservas.AddAsync(r, ct);

    public async Task AdicionarComprovanteAsync(Comprovante c, CancellationToken ct = default)
        => await db.Comprovantes.AddAsync(c, ct);

    public Task<Comprovante?> ObterComprovantePorReservaAsync(int reservaId, CancellationToken ct = default)
        => db.Comprovantes.AsNoTracking().FirstOrDefaultAsync(c => c.ReservaId == reservaId, ct);

    // Cast para decimal? faz o SUM devolver null (e não exceção) quando não há comprovantes.
    public async Task<decimal> SomarKgDoadosAsync(CancellationToken ct = default)
        => await db.Comprovantes.SumAsync(c => (decimal?)c.PesoTotalKg, ct) ?? 0m;
}