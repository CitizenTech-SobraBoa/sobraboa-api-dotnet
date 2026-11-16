using Microsoft.EntityFrameworkCore;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Infrastructure.Persistence;

namespace SobraBoa.API.Infrastructure;

/// <summary>
/// Dados de demonstração para testar o fluxo sem precisar cadastrar tudo na mão.
/// Só roda quando a configuração Seed:DadosDemo = true (NUNCA em produção).
/// É idempotente: se o doador "padaria@sobraboa.com" já existe, não faz nada.
/// </summary>
public static class DemoDataSeeder
{
    public const string SenhaDemo = "Senha@123";

    public static async Task SeedDadosDemoAsync(this WebApplication app)
    {
        if (!app.Configuration.GetValue<bool>("Seed:DadosDemo")) return;

        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<SobraBoaContext>();
        var hasher = sp.GetRequiredService<IPasswordHasher>();
        var agora = sp.GetRequiredService<TimeProvider>().GetUtcNow().UtcDateTime;
        var log = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DemoData");

        // CountAsync > 0 em vez de AnyAsync: evita o bug do provider Oracle
        if (await db.Usuarios.CountAsync(u => u.Email == "padaria@sobraboa.com") > 0)
        {
            log.LogInformation("Dados de demonstração já existem. Nada a fazer.");
            return;
        }

        // ------------------------------------------------------------------
        // 1) Usuários (senha de todos: Senha@123)
        // ------------------------------------------------------------------
        var hash = hasher.Hash(SenhaDemo);

        Usuario Novo(string nome, string email, Papel papel, string org, double lat, double lng,
                     bool refrigeracao, bool aprovado, string cnpj)
        {
            var u = new Usuario(nome, email, hash, papel, org, lat, lng, refrigeracao, cnpj);
            if (aprovado) u.Aprovar();
            return u;
        }

        var padaria     = Novo("Rita Alves",     "padaria@sobraboa.com",     Papel.Doador,   "Padaria Boa Massa",      -23.5614, -46.6559, false, true,  "11.111.111/0001-11");
        var restaurante = Novo("Carlos Mota",    "restaurante@sobraboa.com", Papel.Doador,   "Restaurante Sabor Caseiro", -23.5505, -46.6333, true,  true,  "22.222.222/0001-22");
        var hortifruti  = Novo("Paulo Dias",     "hortifruti@sobraboa.com",  Papel.Doador,   "Hortifruti Verde Vida",   -23.5890, -46.6350, false, false, "33.333.333/0001-33"); // PENDENTE
        var ong         = Novo("João Souza",     "ong@sobraboa.com",         Papel.Receptor, "ONG Mesa Cheia",          -23.5700, -46.6400, true,  true,  "44.444.444/0001-44");
        var casa        = Novo("Maria Lima",     "casa@sobraboa.com",        Papel.Receptor, "Casa de Apoio Esperança", -23.5650, -46.6500, false, true,  "55.555.555/0001-55"); // SEM refrigeração
        var abrigo      = Novo("Beatriz Lima",   "abrigo@sobraboa.com",      Papel.Receptor, "Abrigo Novo Amanhecer",   -23.5700, -46.6360, true,  false, "77.777.777/0001-77"); // PENDENTE

        db.Usuarios.AddRange(padaria, restaurante, hortifruti, ong, casa, abrigo);
        await db.SaveChangesAsync(); // gera os Ids

        // ------------------------------------------------------------------
        // 2) Helpers para criar doações já com histórico
        // ------------------------------------------------------------------
        ItemDoacao Item(string desc, decimal qtd, UnidadeMedida un, decimal kg) => new(desc, qtd, un, kg, null);

        async Task<Doacao> NovaDoacao(Usuario doador, string titulo, string descricao, bool refrigerar,
                                      string instrucoes, bool publicar, params ItemDoacao[] itens)
        {
            // janela aberta desde 1h atrás e válida por 7 dias: a demo não "vence" no meio do teste
            var d = new Doacao(doador.Id, titulo, descricao, agora.AddHours(-1), agora.AddDays(7),
                               refrigerar, instrucoes, doador.Latitude, doador.Longitude, itens, agora);
            db.Doacoes.Add(d);
            await db.SaveChangesAsync();
            db.Historico.Add(new HistoricoStatus(d.Id, null, d.Status, doador.Id, "Rascunho criado (demo)", agora));

            if (publicar)
            {
                var anterior = d.Status;
                d.Publicar();
                db.Historico.Add(new HistoricoStatus(d.Id, anterior, d.Status, doador.Id, "Doação publicada (demo)", agora));
            }
            await db.SaveChangesAsync();
            return d;
        }

        // ------------------------------------------------------------------
        // 3) Doações em estados diferentes
        // ------------------------------------------------------------------

        // 3.1 DISPONÍVEL (sem refrigeração): qualquer receptor aprovado pode reservar
        await NovaDoacao(padaria, "Pães do dia", "Assados hoje, sobra do expediente.", false,
            "Retirar no balcão e falar com o gerente.", true,
            Item("Pão francês", 30, UnidadeMedida.UN, 1.5m),
            Item("Pão de queijo", 20, UnidadeMedida.UN, 1.0m));

        // 3.2 RASCUNHO: serve para testar o "publicar"
        await NovaDoacao(padaria, "Bolos e tortas", "Bolos inteiros sem decoração.", false,
            "Retirar na porta lateral.", false,
            Item("Bolo de fubá", 3, UnidadeMedida.UN, 2.4m));

        // 3.3 DISPONÍVEL e EXIGE REFRIGERAÇÃO: 'casa' (sem refrigeração) leva 422; 'ong' consegue reservar
        await NovaDoacao(restaurante, "Marmitas refrigeradas", "Pratos prontos do almoço, mantidos em câmara fria.", true,
            "Trazer caixa térmica. Retirar na cozinha.", true,
            Item("Marmita de frango com arroz", 25, UnidadeMedida.UN, 10.0m));

        // 3.4 RESERVADA pela ONG (reserva ATIVA): serve para testar retirada, cancelamento e recebimento
        var reservada = await NovaDoacao(padaria, "Sanduíches naturais", "Sanduíches embalados individualmente.", false,
            "Retirar no balcão.", true,
            Item("Sanduíche natural", 12, UnidadeMedida.UN, 2.4m));

        var anteriorR = reservada.Status;
        reservada.Reservar(agora);
        var reservaAtiva = Reserva.Criar(reservada, ong.Id, agora);
        db.Reservas.Add(reservaAtiva);
        db.Historico.Add(new HistoricoStatus(reservada.Id, anteriorR, reservada.Status, ong.Id, "Reservada pelo receptor (demo)", agora));
        await db.SaveChangesAsync();

        // 3.5 CONCLUÍDA, com comprovante: já alimenta as métricas do admin
        var concluida = await NovaDoacao(restaurante, "Sopa do dia", "Sopa de legumes, porções individuais.", false,
            "Retirar na cozinha.", true,
            Item("Sopa de legumes (porção)", 40, UnidadeMedida.UN, 8.0m));

        var anteriorC = concluida.Status;
        concluida.Reservar(agora);
        var reservaConcluida = Reserva.Criar(concluida, ong.Id, agora);
        db.Reservas.Add(reservaConcluida);
        db.Historico.Add(new HistoricoStatus(concluida.Id, anteriorC, concluida.Status, ong.Id, "Reservada pelo receptor (demo)", agora));
        await db.SaveChangesAsync();

        reservaConcluida.ConfirmarRetirada(reservaConcluida.CodigoRetirada);
        anteriorC = concluida.Status;
        concluida.MarcarRetirada();
        db.Historico.Add(new HistoricoStatus(concluida.Id, anteriorC, concluida.Status, restaurante.Id, "Retirada confirmada (demo)", agora));

        reservaConcluida.Concluir();
        anteriorC = concluida.Status;
        concluida.Concluir();
        db.Historico.Add(new HistoricoStatus(concluida.Id, anteriorC, concluida.Status, ong.Id, "Recebimento confirmado (demo)", agora));
        db.Comprovantes.Add(new Comprovante(reservaConcluida.Id, concluida.Id, concluida.PesoTotalKg, agora));
        await db.SaveChangesAsync();

        // ------------------------------------------------------------------
        // 4) Resumo no log
        // ------------------------------------------------------------------
        log.LogInformation(
            """
            ==================== DADOS DE DEMONSTRAÇÃO CRIADOS ====================
            Senha de todos: {Senha}
              Doadores  : padaria@ (aprovado) | restaurante@ (aprovado) | hortifruti@ (PENDENTE)
              Receptores: ong@ (aprovado, COM refrigeração) | casa@ (aprovado, SEM refrigeração) | abrigo@ (PENDENTE)
              (todos @sobraboa.com)
            Reserva ATIVA para testar a retirada: id {ReservaId} | código de retirada {Codigo} (doador: padaria@, receptor: ong@)
            Reserva CONCLUÍDA com comprovante   : id {ReservaConcluidaId}
            ========================================================================
            """,
            SenhaDemo, reservaAtiva.Id, reservaAtiva.CodigoRetirada, reservaConcluida.Id);
    }
}