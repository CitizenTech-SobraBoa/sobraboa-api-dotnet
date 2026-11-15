using Microsoft.EntityFrameworkCore;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.Infrastructure.Repositories;

public class UsuarioRepository(SobraBoaContext db) : IUsuarioRepository
{
    // Com tracking: o serviço altera a entidade (ex.: Aprovar()) e o UnitOfWork salva.
    public Task<Usuario?> ObterPorIdAsync(int id, CancellationToken ct = default)
        => db.Usuarios.FirstOrDefaultAsync(u => u.Id == id, ct);

    // O e-mail é sempre gravado em minúsculas (ver construtor de Usuario).
    public Task<Usuario?> ObterPorEmailAsync(string email, CancellationToken ct = default)
    {
        var e = email.Trim().ToLowerInvariant();
        return db.Usuarios.FirstOrDefaultAsync(u => u.Email == e, ct);
    }

    // CountAsync > 0 em vez de AnyAsync: evita o bug ORA-00904 do provider Oracle.
    public async Task<bool> ExisteEmailAsync(string email, CancellationToken ct = default)
    {
        var e = email.Trim().ToLowerInvariant();
        return await db.Usuarios.CountAsync(u => u.Email == e, ct) > 0;
    }

    public async Task<(IReadOnlyList<Usuario> Itens, int Total)> ListarPendentesAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var q = db.Usuarios.AsNoTracking().Where(u => u.Aprovacao == StatusAprovacao.Pendente);
        var total = await q.CountAsync(ct);
        var itens = await q.OrderBy(u => u.Id)               // ordem estável para paginar
                           .Skip((page - 1) * pageSize).Take(pageSize)
                           .ToListAsync(ct);
        return (itens, total);
    }

    public async Task AdicionarAsync(Usuario u, CancellationToken ct = default)
        => await db.Usuarios.AddAsync(u, ct);

    public async Task<(int Doadores, int Receptores)> ContarAprovadosAsync(CancellationToken ct = default)
    {
        var doadores = await db.Usuarios.CountAsync(u => u.Papel == Papel.Doador && u.Aprovacao == StatusAprovacao.Aprovado, ct);
        var receptores = await db.Usuarios.CountAsync(u => u.Papel == Papel.Receptor && u.Aprovacao == StatusAprovacao.Aprovado, ct);
        return (doadores, receptores);
    }
}