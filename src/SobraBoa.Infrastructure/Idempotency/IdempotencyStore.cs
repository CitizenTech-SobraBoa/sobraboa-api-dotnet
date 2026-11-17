using Microsoft.EntityFrameworkCore;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.Infrastructure.Idempotency;

public class IdempotencyStore(SobraBoaContext db) : IIdempotencyStore
{
    public Task<IdempotencyRecord?> ObterAsync(string chave, string escopo, CancellationToken ct = default)
        => db.IdempotencyRecords.AsNoTracking().FirstOrDefaultAsync(r => r.Chave == chave && r.Escopo == escopo, ct);

    public async Task<bool> SalvarAsync(IdempotencyRecord r, CancellationToken ct = default)
    {
        db.IdempotencyRecords.Add(r);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(r).State = EntityState.Detached;
            return false;
        } // outra requisição gravou antes
    }
    
    public async Task<bool> TentarReservarAsync(string chave, string escopo, DateTime agoraUtc,
        CancellationToken ct = default)
    {
        var r = new IdempotencyRecord { Chave = chave, Escopo = escopo, StatusCode = 0, CriadoEm = agoraUtc };
        db.IdempotencyRecords.Add(r);
        try
        {
            await db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            db.Entry(r).State = EntityState.Detached;
            return false; // o índice único (Chave, Escopo) mostrou que outra requisição chegou antes
        }
    }

    public async Task CompletarAsync(string chave, string escopo, int statusCode, string? corpo,
        CancellationToken ct = default)
    {
        var r = await db.IdempotencyRecords.FirstAsync(x => x.Chave == chave && x.Escopo == escopo, ct);
        r.StatusCode = statusCode;
        r.CorpoResposta = corpo;
        await db.SaveChangesAsync(ct);
    }

    public async Task LiberarAsync(string chave, string escopo, CancellationToken ct = default)
    {
        // O DbContext é o mesmo do endpoint. Se a operação falhou no meio (ex.: violação do índice de reserva),
        // ainda pode haver entidades "sujas" rastreadas. Sem limpar, este SaveChanges tentaria gravá-las de novo.
        db.ChangeTracker.Clear();

        var r = await db.IdempotencyRecords.FirstOrDefaultAsync(x => x.Chave == chave && x.Escopo == escopo, ct);
        if (r is null) return;
        db.IdempotencyRecords.Remove(r);
        await db.SaveChangesAsync(ct);
    }
    
    public async Task<int> RemoverAnterioresAsync(DateTime limiteUtc, CancellationToken ct = default)
    {
        var velhos = await db.IdempotencyRecords.Where(r => r.CriadoEm < limiteUtc).ToListAsync(ct);
        db.IdempotencyRecords.RemoveRange(velhos);
        await db.SaveChangesAsync(ct);
        return velhos.Count;
    }
}