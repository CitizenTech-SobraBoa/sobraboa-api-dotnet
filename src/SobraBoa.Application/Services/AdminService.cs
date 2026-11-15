using SobraBoa.Application.Dtos;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Services;

public class AdminService(
    IUsuarioRepository usuarios,
    IDoacaoRepository doacoes,
    IReservaRepository reservas,
    IUnitOfWork uow)
{
    public async Task<PagedResult<object>> PendentesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var (itens, total) = await usuarios.ListarPendentesAsync(page, pageSize, ct);
        return new(
            itens.Select(u => (object)new
                { u.Id, u.Nome, u.Email, u.NomeOrganizacao, Papel = u.Papel.ToString(), u.Cnpj }).ToList(), page,
            pageSize, total);
    }

    public async Task AprovarAsync(int usuarioId, bool aprovado, CancellationToken ct = default)
    {
        var u = await usuarios.ObterPorIdAsync(usuarioId, ct) ?? throw new NotFoundException("Usuário não encontrado.");
        if (aprovado) u.Aprovar();
        else u.Rejeitar();
        await uow.SaveChangesAsync(ct);
    }

    public async Task<MetricasResponse> MetricasAsync(CancellationToken ct = default)
    {
        var (dd, rr) = await usuarios.ContarAprovadosAsync(ct);
        return new(await doacoes.ContarPorStatusAsync(ct), await reservas.SomarKgDoadosAsync(ct), dd, rr);
    }
}