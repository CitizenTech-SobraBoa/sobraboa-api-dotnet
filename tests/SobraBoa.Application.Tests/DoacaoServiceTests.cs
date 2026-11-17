using Moq;
using SobraBoa.Application.Interfaces;
using SobraBoa.Application.Services;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Tests;

public class DoacaoServiceTests
{
    private readonly Mock<IDoacaoRepository> _doacoes = new();
    private readonly Mock<IReservaRepository> _reservas = new();
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private DoacaoService Svc() =>
        new(_doacoes.Object, _reservas.Object, _usuarios.Object, _uow.Object, TimeProvider.System);

    private static Doacao Nova(bool publicar, int doadorId = 9)
    {
        var agora = DateTime.UtcNow;
        var d = new Doacao(doadorId, "Marmitas", null, agora.AddHours(-1), agora.AddHours(4), false, null, 0, 0,
            [new ItemDoacao("Marmita", 10, UnidadeMedida.UN, 5, null)], agora);
        if (publicar) d.Publicar();
        return d;
    }

    // ---------- Haversine ----------

    [Fact]
    public void Haversine_MesmoPonto_DevolveZero()
        => Assert.Equal(0, DoacaoService.Haversine(-23.55, -46.63, -23.55, -46.63), 6);

    [Fact]
    public void Haversine_SaoPauloRio_FicaPertoDe360Km()
    {
        var km = DoacaoService.Haversine(-23.5505, -46.6333, -22.9068, -43.1729);
        Assert.InRange(km, 350, 365);
    }

    // ---------- Proximas ----------

    [Fact]
    public async Task Proximas_RaioAcimaDoLimite_LancaArgumentException()
    {
        var receptor = new Usuario("Ana", "ana@x.com", "hash", Papel.Receptor, "ONG", 0, 0, true);
        receptor.Aprovar();
        _usuarios.Setup(x => x.ObterPorIdAsync(1, default)).ReturnsAsync(receptor);

        await Assert.ThrowsAsync<ArgumentException>(() => Svc().ProximasAsync(1, -23.5, -46.6, 99, 1, 10));
    }

    [Fact]
    public async Task Proximas_FiltraPeloRaioEOrdenaPorQuemEncerraPrimeiro()
    {
        var receptor = new Usuario("Ana", "ana@x.com", "hash", Papel.Receptor, "ONG", 0, 0, true);
        receptor.Aprovar();
        _usuarios.Setup(x => x.ObterPorIdAsync(1, default)).ReturnsAsync(receptor);

        var agora = DateTime.UtcNow;
        Doacao Em(double lat, double lng, int horasAteFim)
        {
            var d = new Doacao(9, "x", null, agora.AddHours(-1), agora.AddHours(horasAteFim), false, null, lat, lng,
                [new ItemDoacao("a", 1, UnidadeMedida.UN, 1, null)], agora);
            d.Publicar();
            return d;
        }

        var perto = Em(-23.5610, -46.6560, 5);
        var maisPerto = Em(-23.5615, -46.6558, 2);   // encerra antes
        var longe = Em(-22.9068, -43.1729, 3);       // Rio: fora de 10 km
        _doacoes.Setup(x => x.ListarDisponiveisAsync(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>(),
                It.IsAny<double>(), It.IsAny<double>(), default))
            .ReturnsAsync([perto, longe, maisPerto]);

        var r = await Svc().ProximasAsync(1, -23.5614, -46.6559, 10, 1, 10);

        Assert.Equal(2, r.TotalCount);
        Assert.True(r.Items[0].JanelaFim < r.Items[1].JanelaFim);
    }

    // ---------- Visibilidade ----------

    [Fact]
    public async Task Obter_RascunhoDeOutroDoador_LancaNotFound()
    {
        _doacoes.Setup(x => x.ObterPorIdAsync(10, default)).ReturnsAsync(Nova(publicar: false, doadorId: 9));

        await Assert.ThrowsAsync<NotFoundException>(() => Svc().ObterAsync(10, usuarioId: 55, ehAdmin: false));
    }

    [Fact]
    public async Task Obter_RascunhoPeloDono_Retorna()
    {
        _doacoes.Setup(x => x.ObterPorIdAsync(10, default)).ReturnsAsync(Nova(publicar: false, doadorId: 9));

        var r = await Svc().ObterAsync(10, usuarioId: 9, ehAdmin: false);

        Assert.Equal("Rascunho", r.Status);
    }

    [Fact]
    public async Task Obter_RascunhoPeloAdmin_Retorna()
    {
        _doacoes.Setup(x => x.ObterPorIdAsync(10, default)).ReturnsAsync(Nova(publicar: false, doadorId: 9));

        var r = await Svc().ObterAsync(10, usuarioId: 1, ehAdmin: true);

        Assert.Equal("Rascunho", r.Status);
    }

    // ---------- Cancelar ----------

    [Fact]
    public async Task Cancelar_DoacaoReservada_CancelaTambemAReservaAtiva()
    {
        var doacao = Nova(publicar: true);
        doacao.Reservar(DateTime.UtcNow);
        var reserva = Reserva.Criar(doacao, receptorId: 1, DateTime.UtcNow);
        _doacoes.Setup(x => x.ObterPorIdAsync(10, default)).ReturnsAsync(doacao);
        _reservas.Setup(x => x.ObterAtivaPorDoacaoAsync(10, default)).ReturnsAsync(reserva);

        await Svc().CancelarAsync(doadorId: 9, doacaoId: 10);

        Assert.Equal(StatusDoacao.Cancelada, doacao.Status);
        Assert.Equal(StatusReserva.Cancelada, reserva.Status);
        _uow.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Cancelar_DoacaoDeOutroDoador_LancaForbidden()
    {
        _doacoes.Setup(x => x.ObterPorIdAsync(10, default)).ReturnsAsync(Nova(publicar: true, doadorId: 9));

        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().CancelarAsync(doadorId: 77, doacaoId: 10));
    }
}