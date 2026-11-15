using Microsoft.Extensions.DependencyInjection;
using SobraBoa.Application.Services;

namespace SobraBoa.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection s)
    {
        s.AddScoped<AuthService>();
        s.AddScoped<DoacaoService>();
        s.AddScoped<ReservaService>();
        s.AddScoped<AdminService>();
        s.AddSingleton(TimeProvider.System);
        return s;
    }
}