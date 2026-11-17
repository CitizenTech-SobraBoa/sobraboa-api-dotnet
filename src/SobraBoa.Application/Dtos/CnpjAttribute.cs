using System.ComponentModel.DataAnnotations;

namespace SobraBoa.Application.Dtos;

/// <summary>Valida o CNPJ pelos dígitos verificadores. Campo opcional: nulo ou vazio passa.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Field)]
public sealed class CnpjAttribute : ValidationAttribute
{
    public CnpjAttribute() : base("CNPJ inválido.") { }

    public override bool IsValid(object? value)
        => value is null or "" || (value is string s && CnpjValido(s));

    public static bool CnpjValido(string cnpj)
    {
        var d = new string(cnpj.Where(char.IsDigit).ToArray());
        if (d.Length != 14 || d.Distinct().Count() == 1) return false;

        int[] p1 = [5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        int[] p2 = [6, 5, 4, 3, 2, 9, 8, 7, 6, 5, 4, 3, 2];
        return Digito(d, p1) == d[12] - '0' && Digito(d, p2) == d[13] - '0';
    }

    private static int Digito(string d, int[] pesos)
    {
        var soma = 0;
        for (var i = 0; i < pesos.Length; i++) soma += (d[i] - '0') * pesos[i];
        var resto = soma % 11;
        return resto < 2 ? 0 : 11 - resto;
    }
}