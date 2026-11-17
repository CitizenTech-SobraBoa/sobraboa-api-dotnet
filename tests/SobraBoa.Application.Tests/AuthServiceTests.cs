using Moq;
using SobraBoa.Application.Dtos;
using SobraBoa.Application.Interfaces;
using SobraBoa.Application.Services;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Tests;

public class AuthServiceTests
{
    private readonly Mock<IUsuarioRepository> _usuarios = new();
    private readonly Mock<IPasswordHasher> _hasher = new();
    private readonly Mock<IJwtTokenGenerator> _jwt = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly Mock<ICepService> _cep = new();

    private AuthService Svc() => new(_usuarios.Object, _hasher.Object, _jwt.Object, _uow.Object, _cep.Object);

    private static RegistroRequest Registro(string? cep = null) => new(
        "Rita", "rita@x.com", "Senha@123", Papel.Doador, "Padaria", null, cep, -23.5, -46.6, false, true);

    [Fact]
    public async Task Registrar_EmailJaCadastrado_LancaConflict()
    {
        _usuarios.Setup(x => x.ExisteEmailAsync("rita@x.com", default)).ReturnsAsync(true);

        await Assert.ThrowsAsync<ConflictException>(() => Svc().RegistrarAsync(Registro()));
        _uow.Verify(x => x.SaveChangesAsync(default), Times.Never);
    }

    [Fact]
    public async Task Registrar_CepInexistente_LancaDomainException()
    {
        _cep.Setup(x => x.CepExisteAsync("00000-000", default)).ReturnsAsync(false);

        await Assert.ThrowsAsync<DomainException>(() => Svc().RegistrarAsync(Registro("00000-000")));
    }

    [Fact]
    public async Task Registrar_ViaCepIndisponivel_NaoBloqueiaOCadastro()
    {
        _cep.Setup(x => x.CepExisteAsync("01310-100", default)).ReturnsAsync((bool?)null);
        _hasher.Setup(x => x.Hash("Senha@123")).Returns("hash");

        await Svc().RegistrarAsync(Registro("01310-100"));

        _usuarios.Verify(x => x.AdicionarAsync(It.IsAny<Usuario>(), default), Times.Once);
        _uow.Verify(x => x.SaveChangesAsync(default), Times.Once);
    }

    [Fact]
    public async Task Login_SenhaErrada_LancaAuthenticationFailed()
    {
        var u = new Usuario("Rita", "rita@x.com", "hash", Papel.Doador, "Padaria", 0, 0, false);
        _usuarios.Setup(x => x.ObterPorEmailAsync("rita@x.com", default)).ReturnsAsync(u);
        _hasher.Setup(x => x.Verify("errada", "hash")).Returns(false);

        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            Svc().LoginAsync(new LoginRequest("rita@x.com", "errada")));
    }

    [Fact]
    public async Task Login_EmailInexistente_UsaAMesmaExcecaoDaSenhaErrada()
    {
        await Assert.ThrowsAsync<AuthenticationFailedException>(() =>
            Svc().LoginAsync(new LoginRequest("nao@existe.com", "qualquer")));
    }

    [Fact]
    public async Task Login_Valido_DevolveTokenEPapel()
    {
        var u = new Usuario("Rita", "rita@x.com", "hash", Papel.Doador, "Padaria", 0, 0, false);
        _usuarios.Setup(x => x.ObterPorEmailAsync("rita@x.com", default)).ReturnsAsync(u);
        _hasher.Setup(x => x.Verify("Senha@123", "hash")).Returns(true);
        _jwt.Setup(x => x.Gerar(u)).Returns(("token-xyz", DateTime.UtcNow.AddHours(2)));

        var r = await Svc().LoginAsync(new LoginRequest("rita@x.com", "Senha@123"));

        Assert.Equal("token-xyz", r.Token);
        Assert.Equal("Doador", r.Papel);
        Assert.Equal("Pendente", r.Aprovacao);
    }
}