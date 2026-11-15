using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Domain.Tests;

public class DoacaoTests
{
    private static readonly DateTime Agora = new(2026, 11, 20, 18, 0, 0, DateTimeKind.Utc);

    private static Doacao Nova(bool publicar = true, DateTime? fim = null)
    {
        var d = new Doacao(1, "Pães do dia", null, Agora.AddHours(-1), fim ?? Agora.AddHours(3), false, null, -23.5,
            -46.6,
            [new ItemDoacao("Pão francês", 30, UnidadeMedida.UN, 1.5m, null)], Agora);
        if (publicar) d.Publicar();
        return d;
    }

    [Fact]
    public void Criar_JanelaTerminandoNoPassado_LancaDomainException()
    {
        // Arrange / Act
        var act = () => new Doacao(1, "x", null, Agora.AddHours(-3), Agora.AddHours(-1), false, null, 0, 0,
            [new ItemDoacao("a", 1, UnidadeMedida.UN, 1, null)], Agora);
        // Assert
        Assert.Throws<DomainException>(act);
    }

    [Fact]
    public void Criar_SemItens_LancaDomainException()
        => Assert.Throws<DomainException>(() =>
            new Doacao(1, "x", null, Agora, Agora.AddHours(2), false, null, 0, 0, [], Agora));

    [Fact]
    public void Publicar_Rascunho_VaiParaDisponivel()
    {
        var d = Nova(publicar: false);
        d.Publicar();
        Assert.Equal(StatusDoacao.Disponivel, d.Status);
    }

    [Fact]
    public void Reservar_DoacaoJaReservada_LancaConflictException()
    {
        var d = Nova();
        d.Reservar(Agora);
        Assert.Throws<ConflictException>(() => d.Reservar(Agora));
    }

    [Fact]
    public void Reservar_ForaDaJanela_LancaDomainException()
    {
        var d = Nova();
        Assert.Throws<DomainException>(() => d.Reservar(Agora.AddHours(10)));
    }

    [Fact]
    public void LiberarReserva_JanelaAberta_VoltaParaDisponivel()
    {
        var d = Nova();
        d.Reservar(Agora);
        d.LiberarReserva(Agora);
        Assert.Equal(StatusDoacao.Disponivel, d.Status);
    }

    [Fact]
    public void Concluir_SemRetirada_LancaDomainException()
    {
        var d = Nova();
        d.Reservar(Agora);
        Assert.Throws<DomainException>(() => d.Concluir());
    }

    [Fact]
    public void PesoTotalKg_SomaOsItens()
        => Assert.Equal(1.5m, Nova().PesoTotalKg);
}
