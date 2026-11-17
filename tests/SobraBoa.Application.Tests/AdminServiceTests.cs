using Moq;
using SobraBoa.Application.Interfaces;
using SobraBoa.Application.Services;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Tests;

public class AdminServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IDoacaoRepository> _doacoes = new();
    private readonly Mock<IReservaRepository> _reservas = new();
    private readonly Mock<IUnitOfWork> _uow = new();

    private AdminService Svc() => new(_usuarios.Object, _doacoes.Object, _reservas.Object, _uow.Object);

    [Fact]
    public async Task Aprovar_UsuarioInexistente_LancaNotFound()
        => await Assert.ThrowsAsync<NotFoundException>(() => Svc().AprovarAsync(99, true));

    [Fact]
    public async Task Aprovar_True_AprovaESalva()
    {
        var u = new Usuario("Ana", "ana@x.com", "hash", Papel.Receptor, "ONG", 0, 0, false);
        _usuarios.Setup(x => x.ObterPorIdAsync(1, default)).ReturnsAsync(u);

        await Svc().AprovarAsync(1, true);

        Assert.True(u.EstaAprovado);
        _uow.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Aprovar_False_Rejeita()
    {
        var u = new Usuario("Ana", "ana@x.com", "hash", Papel.Receptor, "ONG", 0, 0, false);
        _usuarios.Setup(x => x.ObterPorIdAsync(1, default)).ReturnsAsync(u);

        await Svc().AprovarAsync(1, false);

        Assert.Equal(StatusAprovacao.Rejeitado, u.Aprovacao);
    }

    [Fact]
    public async Task Pendentes_LimitaOTamanhoDaPaginaA50()
    {
        _usuarios.Setup(x => x.ListarPendentesAsync(1, 50, default))
            .ReturnsAsync(((IReadOnlyList<Usuario>)new List<Usuario>(), 0));

        var r = await Svc().PendentesAsync(page: 0, pageSize: 5000);

        Assert.Equal(1, r.Page);
        Assert.Equal(50, r.PageSize);
    }
}