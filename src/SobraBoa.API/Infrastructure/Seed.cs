using Microsoft.EntityFrameworkCore;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.API.Infrastructure;

public static class Seed
{
    public static async Task SeedAdminAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SobraBoaContext>();
        if (db.Database.IsRelational()) await db.Database.MigrateAsync(); // InMemory (testes) não migra

        var cfg = app.Configuration;
        var email = cfg["Admin:Email"];
        var senha = cfg["Admin:Senha"];
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha)) return;
        if (SecretsGuard.EhSenhaPlaceholder(senha))
        {
            app.Logger.LogWarning("Admin:Senha é o placeholder do repositório. Admin NÃO foi criado.");
            return;
        }

        if (await db.Usuarios.CountAsync(u => u.Email == email) > 0) return;

        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        db.Usuarios.Add(Usuario.CriarAdmin("Administrador", email, hasher.Hash(senha)));
        await db.SaveChangesAsync();
    }
}