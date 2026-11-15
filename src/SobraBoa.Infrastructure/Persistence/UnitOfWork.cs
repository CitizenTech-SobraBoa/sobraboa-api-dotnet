using Microsoft.EntityFrameworkCore;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Infrastructure.Persistence;

public class UnitOfWork(SobraBoaContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken ct = default)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (EhViolacaoDeUnicidade(ex))
        {
            // ORA-00001: unique constraint violated -> alguém chegou antes
            throw new ConflictException(
                "Operação conflitante: o recurso já foi reservado ou registrado por outra requisição.");
        }
    }

    private static bool EhViolacaoDeUnicidade(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("ORA-00001") == true
           || ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) == true;
}