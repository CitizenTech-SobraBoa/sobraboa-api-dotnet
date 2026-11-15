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
}