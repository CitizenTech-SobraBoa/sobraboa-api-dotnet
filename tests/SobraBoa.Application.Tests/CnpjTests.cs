using SobraBoa.Application.Dtos;

namespace SobraBoa.Application.Tests;

public class CnpjTests
{
    [Theory]
    [InlineData("11.222.333/0001-81")]
    [InlineData("11222333000181")]
    public void CnpjValido_Aceita(string cnpj) => Assert.True(CnpjAttribute.CnpjValido(cnpj));

    [Theory]
    [InlineData("11.111.111/0001-11")]
    [InlineData("00000000000000")]
    [InlineData("123")]
    [InlineData("11.222.333/0001-80")] // dígito verificador errado
    public void CnpjValido_Rejeita(string cnpj) => Assert.False(CnpjAttribute.CnpjValido(cnpj));

    [Fact]
    public void Atributo_AceitaNuloEVazio()
    {
        var a = new CnpjAttribute();
        Assert.True(a.IsValid(null));
        Assert.True(a.IsValid(""));
    }
}