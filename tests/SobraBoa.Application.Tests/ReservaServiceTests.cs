using Moq;
using SobraBoa.Application.Interfaces;
using SobraBoa.Application.Services;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Tests;

public class ReservaServiceTests
{
    private readonly Mock<IReservaRepository> _reservas = new();
    private readonly Mock<IDoacaoRepository> _doacoes = new();
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private ReservaService Svc() =>
        new(_reservas.Object, _doacoes.Object, _usuarios.Object, _uow.Object, TimeProvider.System);

    private static Usuario Receptor(bool aprovado, bool refrigeracao)
    {
        var u = new Usuario("Ana", "ana@x.com", "hash", Papel.Receptor, "ONG", 0, 0, refrigeracao);
        if (aprovado) u.Aprovar();
        return u;
    }

    private static Doacao DoacaoDisponivel(bool refrigerar)
    {
        var agora = DateTime.UtcNow;
        var d = new Doacao(9, "Marmitas", null, agora.AddHours(-1), agora.AddHours(4), refrigerar, null, 0, 0,
            [new ItemDoacao("Marmita", 10, UnidadeMedida.UN, 5, null)], agora);
        d.Publicar();
        return d;
    }

    [Fact]
    public async Task Reservar_ReceptorPendente_LancaForbidden()
    {
        _usuarios.Setup(x => x.ObterPorIdAsync(1, default)).ReturnsAsync(Receptor(aprovado: false, refrigeracao: true));
        await Assert.ThrowsAsync<ForbiddenException>(() => Svc().ReservarAsync(1, 10));
    }

    [Fact]
    public async Task Reservar_DoacaoRefrigeradaSemRefrigeracao_LancaDomainException()
    {
        _usuarios.Setup(x => x.ObterPorIdAsync(1, default)).ReturnsAsync(Receptor(true, refrigeracao: false));
        _doacoes.Setup(x => x.ObterPorIdAsync(10, default)).ReturnsAsync(DoacaoDisponivel(refrigerar: true));
        await Assert.ThrowsAsync<DomainException>(() => Svc().ReservarAsync(1, 10));
    }

    [Fact]
    public async Task Reservar_Valido_CriaReservaAtivaESalva()
    {
        _usuarios.Setup(x => x.ObterPorIdAsync(1, default)).ReturnsAsync(Receptor(true, true));
        _doacoes.Setup(x => x.ObterPorIdAsync(10, default)).ReturnsAsync(DoacaoDisponivel(true));

        var r = await Svc().ReservarAsync(1, 10);

        Assert.Equal("Ativa", r.Status);
        _reservas.Verify(x => x.AdicionarAsync(It.IsAny<Reserva>(), default), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task ConfirmarRetirada_DoadorDeOutraDoacao_LancaForbidden()
    {
        var doacao = DoacaoDisponivel(false); // doador 9
        doacao.Reservar(DateTime.UtcNow);
        var reserva = Reserva.Criar(doacao, 1, DateTime.UtcNow);
        _reservas.Setup(x => x.ObterPorIdAsync(5, default)).ReturnsAsync(reserva);
        _doacoes.Setup(x => x.ObterPorIdAsync(reserva.DoacaoId, default)).ReturnsAsync(doacao);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            Svc().ConfirmarRetiradaAsync(doadorId: 77, 5, reserva.CodigoRetirada));
    }
}