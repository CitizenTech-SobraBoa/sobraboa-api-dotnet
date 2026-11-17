using SobraBoa.Application.Dtos;
using SobraBoa.Application.Interfaces;
using SobraBoa.Domain.Entities;
using SobraBoa.Domain.Exceptions;

namespace SobraBoa.Application.Services;

public class AuthService(
    IUsuarioRepository usuarios,
    IPasswordHasher hasher,
    IJwtTokenGenerator jwt,
    IUnitOfWork uow,
    ICepService cep)
{
    public async Task<int> RegistrarAsync(RegistroRequest r, CancellationToken ct = default)
    {
        if (await usuarios.ExisteEmailAsync(r.Email, ct)) throw new ConflictException("E-mail já cadastrado.");
        if (!string.IsNullOrWhiteSpace(r.Cep) && await cep.CepExisteAsync(r.Cep, ct) == false)
            throw new DomainException("CEP não encontrado.");
        var u = new Usuario(r.Nome, r.Email, hasher.Hash(r.Senha), r.Papel, r.NomeOrganizacao,
            r.Latitude, r.Longitude, r.PossuiRefrigeracao, r.Cnpj, r.Cep);
        await usuarios.AdicionarAsync(u, ct);
        await uow.SaveChangesAsync(ct);
        return u.Id;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest r, CancellationToken ct = default)
    {
        var u = await usuarios.ObterPorEmailAsync(r.Email.Trim().ToLowerInvariant(), ct);
        // mesma mensagem para e-mail inexistente e senha errada (não revela quais e-mails existem)
        if (u is null || !hasher.Verify(r.Senha, u.SenhaHash))
            throw new AuthenticationFailedException("E-mail ou senha inválidos.");
        var (token, exp) = jwt.Gerar(u);
        return new LoginResponse(token, "Bearer", exp, u.Id, u.Nome, u.Papel.ToString(), u.Aprovacao.ToString());
    }
}