using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;

namespace SobraBoa.API.Infrastructure;

/// <summary>
/// A cada N segundos: expira reservas vencidas (devolvendo a doação à vitrine se a janela ainda está aberta),
/// expira doações disponíveis com janela encerrada e apaga registros de idempotência com mais de 24 h.
/// </summary>
public class ExpiracaoJob(
    IServiceScopeFactory scopes,
    TimeProvider clock,
    IConfiguration cfg,
    ILogger<ExpiracaoJob> log) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        if (!cfg.GetValue("Jobs:Expiracao:Habilitado", true)) return;

        var intervalo = TimeSpan.FromSeconds(cfg.GetValue("Jobs:Expiracao:IntervaloSegundos", 60));
        using var timer = new PeriodicTimer(intervalo);

        try
        {
            while (await timer.WaitForNextTickAsync(ct))
            {
                try
                {
                    await ProcessarAsync(ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    log.LogError(ex, "Falha no job de expiração (nova tentativa no próximo ciclo)");
                }
            }
        }
        catch (OperationCanceledException)
        {
            // aplicação encerrando
        }
    }

    private async Task ProcessarAsync(CancellationToken ct)
    {
        // cada ciclo usa seu próprio escopo (DbContext é Scoped)
        using var scope = scopes.CreateScope();
        var sp = scope.ServiceProvider;
        var reservas = sp.GetRequiredService<IReservaRepository>();
        var doacoes = sp.GetRequiredService<IDoacaoRepository>();
        var idempotencia = sp.GetRequiredService<IIdempotencyStore>();
        var uow = sp.GetRequiredService<IUnitOfWork>();
        var agora = clock.GetUtcNow().UtcDateTime;

        // 1) reservas vencidas
        foreach (var r in await reservas.ListarAtivasVencidasAsync(agora, 100, ct))
        {
            var d = await doacoes.ObterPorIdAsync(r.DoacaoId, ct);
            if (d is null) continue;

            var anterior = d.Status;
            r.Expirar();
            if (d.Status == StatusDoacao.Reservada) d.LiberarReserva(agora); // Disponivel ou Expirada
            await doacoes.AdicionarHistoricoAsync(
                new HistoricoStatus(d.Id, anterior, d.Status, null, "Reserva expirada automaticamente", agora), ct);
        }

        // 2) doações disponíveis com a janela encerrada
        foreach (var d in await doacoes.ListarDisponiveisVencidasAsync(agora, 100, ct))
        {
            var anterior = d.Status;
            d.Expirar(agora);
            await doacoes.AdicionarHistoricoAsync(
                new HistoricoStatus(d.Id, anterior, d.Status, null, "Janela de retirada encerrada", agora), ct);
        }

        await uow.SaveChangesAsync(ct);

        // 3) limpeza das chaves de idempotência antigas
        var removidos = await idempotencia.RemoverAnterioresAsync(agora.AddHours(-24), ct);
        if (removidos > 0) log.LogInformation("{N} registros de idempotência antigos removidos", removidos);
    }
}