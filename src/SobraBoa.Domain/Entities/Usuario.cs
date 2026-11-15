using SobraBoa.Domain.Enums;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Domain.Entities;

public class Usuario
{
    public int Id { get; private set; }
    public string Nome { get; private set; } = default!;
    public string Email { get; private set; } = default!;
    public string SenhaHash { get; private set; } = default!;
    public Papel Papel { get; private set; }
    public StatusAprovacao Aprovacao { get; private set; } = StatusAprovacao.Pendente;

    public string NomeOrganizacao { get; private set; } = default!;
    public string? Cnpj { get; private set; }
    public string? Cep { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public bool PossuiRefrigeracao { get; private set; } 

    private Usuario()
    {
    } 

    public Usuario(string nome, string email, string senhaHash, Papel papel, string nomeOrganizacao,
        double latitude, double longitude, bool possuiRefrigeracao, string? cnpj = null, string? cep = null)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new DomainException("Nome é obrigatório.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@')) throw new DomainException("E-mail inválido.");
        if (papel == Papel.Admin) throw new DomainException("Cadastro público não pode criar Admin.");

        Nome = nome.Trim();
        Email = email.Trim().ToLowerInvariant();
        SenhaHash = senhaHash;
        Papel = papel;
        NomeOrganizacao = nomeOrganizacao.Trim();
        Latitude = latitude;
        Longitude = longitude;
        PossuiRefrigeracao = possuiRefrigeracao;
        Cnpj = cnpj;
        Cep = cep;
    }

    public static Usuario CriarAdmin(string nome, string email, string senhaHash) => new()
    {
        Nome = nome, Email = email.ToLowerInvariant(), SenhaHash = senhaHash,
        Papel = Papel.Admin, Aprovacao = StatusAprovacao.Aprovado, NomeOrganizacao = "SobraBoa"
    };

    public bool EstaAprovado => Aprovacao == StatusAprovacao.Aprovado;
    public void Aprovar() => Aprovacao = StatusAprovacao.Aprovado;
    public void Rejeitar() => Aprovacao = StatusAprovacao.Rejeitado;
}