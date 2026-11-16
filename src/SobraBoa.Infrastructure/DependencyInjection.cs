using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SobraBoa.Application.Interfaces;
using SobraBoa.Infrastructure.Idempotency;
using SobraBoa.Infrastructure.Persistence;
using SobraBoa.Infrastructure.Repositories;
using SobraBoa.Infrastructure.Security;

namespace SobraBoa.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection s, IConfiguration cfg)
    {
        s.AddDbContext<SobraBoaContext>(o => o.UseOracle(
            cfg.GetConnectionString("OracleConnection"),
            x => x.MigrationsHistoryTable("__SobraBoaMigrationsHistory")));        s.AddScoped<IUnitOfWork, UnitOfWork>();
        s.AddScoped<IUsuarioRepository, UsuarioRepository>();
        s.AddScoped<IDoacaoRepository, DoacaoRepository>();
        s.AddScoped<IReservaRepository, ReservaRepository>();
        s.AddScoped<IIdempotencyStore, IdempotencyStore>();
        s.AddSingleton<IPasswordHasher, PasswordHasher>();
        s.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();
        return s;
    }
}

