using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Domain.Tests;

public class ReservaTests
{
    private static Reserva Criar()
    {
        var agora = DateTime.UtcNow;
        var d = new Doacao(1, "x", null, agora.AddHours(-1), agora.AddHours(5), false, null, 0, 0,
            [new ItemDoacao("a", 1, UnidadeMedida.UN, 1, null)], agora);
        return Reserva.Criar(d, 2, agora);
    }

    [Fact]
    public void Criar_GeraCodigoDe6Caracteres() => Assert.Equal(6, Criar().CodigoRetirada.Length);

    [Fact]
    public void ConfirmarRetirada_CodigoErrado_LancaDomainException()
        => Assert.Throws<DomainException>(() => Criar().ConfirmarRetirada("ZZZZZZ"));

    [Fact]
    public void ConfirmarRetirada_CodigoCorreto_MudaParaRetirada()
    {
        var r = Criar();
        r.ConfirmarRetirada(r.CodigoRetirada.ToLower()); // case-insensitive
        Assert.Equal(StatusReserva.Retirada, r.Status);
    }
}