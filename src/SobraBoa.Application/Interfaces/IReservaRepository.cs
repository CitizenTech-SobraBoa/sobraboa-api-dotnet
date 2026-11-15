using SobraBoa.Domain.Entities;

namespace SobraBoa.Application.Interfaces;

public interface IReservaRepository
{
    Task<Reserva?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<IReadOnlyList<Reserva>> ListarDoReceptorAsync(int receptorId, CancellationToken ct = default);
    Task AdicionarAsync(Reserva r, CancellationToken ct = default);
    Task AdicionarComprovanteAsync(Comprovante c, CancellationToken ct = default);
    Task<Comprovante?> ObterComprovantePorReservaAsync(int reservaId, CancellationToken ct = default);
    Task<decimal> SomarKgDoadosAsync(CancellationToken ct = default);
}