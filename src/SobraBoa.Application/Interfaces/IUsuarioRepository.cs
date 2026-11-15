using SobraBoa.Domain.Entities;

namespace SobraBoa.Application.Interfaces;

public interface IUsuarioRepository
{
    Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default);
    Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default);
    Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default);

    Task<(IReadOnlyList<Usuario> Itens, int Total)> ListarPendentesAsync(int page, int pageSize,
        CancellationToken ct = default);

    Task AdicionarAsync(Usuario u, CancellationToken ct = default);
    Task<(int Doadores, int Receptores)> ContarAprovadosAsync(CancellationToken ct = default);
}